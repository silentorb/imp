using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.schema;
using metahub.render;
using metahub.render.targets;

namespace imperative.render.artisan.targets
{
    public class Cpp : Common_Target2
    {

        public Cpp(Overlord overlord = null)
            : base(overlord)
        {
            config = new Target_Configuration()
            {
                float_suffix = true,
                statement_terminator = ";",
                dependency_keyword = "using",
                space_tabs = true,
                indent = 4,
                block_brace_same_line = false,
                explicit_public_members = false,
                type_mode = Type_Mode.required_prefix,
                namespace_separator = "::",
                list_start = "{",
                list_end = "}"
            };

            types["string"] = "std::string";
            minion_names[Professions.List.minions["push"]] = "push_back";
        }

        public override void run(Build_Orders config1, string[] sources)
        {
            if (config1.name != "")
            {
                create_cmake_file(config1);
            }
            foreach (var dungeon in overlord.root.dungeons.Values)
            {
                if (dungeon.is_external || (dungeon.is_abstract && dungeon.is_external))
                    continue;

                render_full_dungeon(dungeon, config1);
                //Console.WriteLine(dungeon.realm.name + "." + dungeon.name);
            }
        }

        void create_cmake_file(Build_Orders config1)
        {
            var dir = config1.output + "/";
            var sources = new List<String>();

            gather_source_paths(overlord.root, sources);
            string output =
                "set(" + config1.name + "_includes\r\n"
                + "  ${CMAKE_CURRENT_LIST_DIR}\r\n"
                + ")\r\n"
                + "\r\n"
                + "set(" + config1.name + "_sources\r\n"
                + sources.join("\r\n")
                + "\r\n"
                + ")\r\n";

            Generator.create_file(dir + config1.name + "-config.cmake", output);

            Generator.create_file(dir + "CMakeLists.txt", "project(" + config1.name + ")");
        }

        void gather_source_paths(Dungeon dungeon, List<String> sources)
        {
            if (dungeon.portals.Length > 0 || dungeon.minions.Count > 0)
            {
                var space = Generator.get_namespace_path(dungeon.realm).join("/") + "/";
                if (space == "/")
                    space = "";

                sources.Add("\t" + space+ dungeon.name + ".cpp");
            }

            foreach (var child in dungeon.dungeons.Values)
            {
                if (child.is_external || (child.is_abstract && child.is_external))
                    continue;

                gather_source_paths(child, sources);
            }
        }

        public void render_full_dungeon(Dungeon dungeon, Build_Orders config1)
        {
            if (dungeon.portals.Length > 0 || dungeon.minions.Count > 0)
            {
                var space = Generator.get_namespace_path(dungeon.realm);
                var dir = config1.output + "/" + space.join("/");
                Generator.create_folder(dir);

                var name = dir + "/" + dungeon.name;

                Generator.create_file(name + ".h",
                    Overlord.stroke_to_string(generate_header_file(dungeon)));

                Generator.create_file(name + ".cpp",
                    Overlord.stroke_to_string(generate_class_file(dungeon)));
            }

            foreach (var child in dungeon.dungeons.Values)
            {
                render_full_dungeon(child, config1);
            }
        }

        public List<Stroke> generate_source_strokes()
        {
            var output = new List<Stroke>();
            foreach (var dungeon in overlord.dungeons)
            {
                if (dungeon.is_external || dungeon.is_external
                    || dungeon.name == "")
                    continue;

                output.Add(generate_class_file(dungeon));
            }

            return output;
        }

        public Stroke generate_header_file(Dungeon dungeon)
        {
            List<External_Header> headers = new List<External_Header>
            {
                new External_Header("string", true),
                new External_Header("vector", true),
                new External_Header("map", true),
                new External_Header("memory", true) 
            }.Concat(
            dungeon.dependencies.Values.Where(d => !d.allow_partial)
                   .OrderBy(d => d.dungeon.source_file)
                   .Select(d => new External_Header(d.dungeon.source_file))
            )
            .ToList();

            var result = new Stroke_Token("#pragma once") + new Stroke_Newline()
                + render_includes(headers) + new Stroke_Newline() + new Stroke_Newline()
                + render_outer_dependencies(dungeon)
                + render_realm(dungeon.realm, () =>
                    render_inner_dependencies(dungeon).Concat(new[] { class_declaration(dungeon) }).ToList());

            return result;
        }

        static List<Dungeon> get_dungeon_parents(Dungeon dungeon)
        {
            var parents = new List<Dungeon>();
            if (dungeon.parent != null)
                parents.Add(dungeon.parent);

            return parents.Concat(dungeon.interfaces).ToList();
        }

        Stroke class_declaration(Dungeon dungeon)
        {
            current_dungeon = dungeon;
            Stroke first = new Stroke_Token("class ");
            if (dungeon.class_export.Length > 0)
                first += new Stroke_Token(dungeon.class_export + " ");

            first += new Stroke_Token(dungeon.name);
            var parents = get_dungeon_parents(dungeon);

            if (parents.Count > 0)
            {
                first += new Stroke_Token(" : ") + Stroke.join(
                    parents.Select(p => new Stroke_Token("public ") + render_dungeon_path(p)).ToList(), ", ");
            }

            var lines = new List<Stroke>
            {
                new Stroke_Token("public:")
            };

            foreach (var portal in dungeon.core_portals.Values)
            {
                lines.Add(property_declaration(portal));
            }

            foreach (var portal in dungeon.all_portals.Values.Except(dungeon.core_portals.Values))
            {
                if (portal.dungeon.is_abstract)
                    lines.Add(property_declaration(portal));
            }

            lines.AddRange(render_function_declarations(dungeon));

            return first + render_block(lines, false) + new Stroke_Token(";");
        }

        Stroke property_declaration(Portal portal)
        {
            return render_profession(portal.get_profession()) + new Stroke_Token(" " + portal.name + ";");
        }

        List<Stroke> render_function_declarations(Dungeon dungeon)
        {
            //            var declarations = dungeon.stubs.Select(line).ToList();
            var declarations = new List<Stroke>();
            //
            //            if (dungeon.hooks.ContainsKey("initialize_post"))
            //            {
            //                declarations.Add(line("void initialize_post(); // Externally defined."));
            //            }

            declarations.AddRange(dungeon.minions.Values.Select(render_function_declaration));

            return declarations;
        }

        Stroke render_function_declaration(Minion definition)
        {
            return new Stroke_Token(definition.return_type != null ? "virtual " : "")
                        + (definition.return_type != null ? render_profession(definition.return_type)
                        + new Stroke_Token(" ") : new Stroke_Token(""))
                        + new Stroke_Token(definition.name)
                        + new Stroke_Token("(")
                        + Stroke.join(definition.parameters.Select(render_declaration_parameter), ", ")
                        + new Stroke_Token(")")
                        + new Stroke_Token(definition.is_abstract ? " = 0;" : ";");
        }

        Stroke render_declaration_parameter(Parameter parameter)
        {
            return render_profession(parameter.symbol, true) + new Stroke_Token(" " + parameter.symbol.name)
                   + (parameter.default_value != null
                          ? new Stroke_Token(" = ") + render_expression(parameter.default_value)
                          : new Stroke_Token()
                     );
        }

        override protected Stroke render_profession(Profession signature, bool is_parameter = false)
        {
            if (signature.dungeon == Professions.List)
                return listify(render_profession(signature.children[0]), signature);
            //            throw new Exception("Not implemented.");
            var lower_name = signature.dungeon.name.ToLower();
            var name = types.ContainsKey(lower_name)
                ? new Stroke_Token(types[lower_name])
                : render_dungeon_path(signature.dungeon);

            if (!signature.dungeon.is_value)
                name = new Stroke_Token("std::shared_ptr<") + name + new Stroke_Token(">");

            return name;
        }
        public Stroke generate_class_file(Dungeon dungeon)
        {
            //            var headers = new List<External_Header> { new External_Header("stdafx") }.Concat(
            var headers = new List<External_Header> { }.Concat(
             new List<External_Header> { new External_Header(dungeon.source_file) }.Concat(
                 dungeon.dependencies.Values.Where(d => d.dungeon != dungeon.parent && d.dungeon.source_file != null)
                        .Select(d => new External_Header(d.dungeon.source_file)))
                                                                                   .OrderBy(h => h.name)
             ).ToList();

            foreach (var func in dungeon.used_functions.Values)
            {
                if (func.name == "rand" && func.is_platform_specific)
                {
                    if (!has_header(headers, "stdlib"))
                        headers.Add(new External_Header("stdlib", true));
                }
            }

            return
                render_includes(headers) + new Stroke_Newline() + new Stroke_Newline()
//                + render_outer_dependencies(dungeon)
                + render_dungeon(dungeon);
        }

        static bool has_header(IEnumerable<External_Header> list, string name)
        {
            return list.Any(header => header.name == name);
        }

        override protected Stroke render_dungeon(Dungeon dungeon)
        {
            if (dungeon.realm != null && dungeon.realm.name != "")
            {
                return render_realm(dungeon.realm, () =>
                    render_minions(dungeon));
            }

            return new Stroke_List(Stroke_Type.statements,
                render_minions(dungeon));
        }

        List<Stroke> render_minions(Dungeon dungeon)
        {
            return new List<Stroke> { render_constructor(dungeon) }
                .Concat(dungeon.minions.Values
                .Where(m => m.name != "constructor")
                .Select(f => render_function_definition(f, f.name))).ToList();
        }

        override protected Stroke listify(Stroke type, Profession signature)
        {
            return new Stroke_Token("std::vector<") + type + new Stroke_Token(">");
        }

        protected Stroke render_function_definition(Minion definition, string name)
        {
            if (definition.is_abstract)
                return new Stroke_Token();

            var a = (definition.return_type != null
                ? render_profession(definition.return_type) + new Stroke_Token(" ")
                : new Stroke_Token());

            var intro = a
                + new Stroke_Token(definition.dungeon.name + "::" + name)
                + new Stroke_Token("(")
                + new Stroke_Token(definition.parameters.Select(p => render_definition_parameter(p).full_text()).join(", "))
                + new Stroke_Token(")");

            return intro + render_block(render_statements(definition.expressions), false) + new Stroke_Newline();
        }

        protected override Stroke render_platform_function_call(Platform_Function expression, Expression parent)
        {
            throw new Exception("Not implemented.");
        }

        private class Temp
        {
            public Dungeon realm;
            public List<IDungeon> dependencies;
        }

        Stroke render_outer_dependencies(Dungeon dungeon)
        {
            bool lines = false;
            Stroke result = new Stroke_Token();
            Dictionary<string, Temp> realms = new Dictionary<string, Temp>();

            foreach (var d in dungeon.dependencies.Values)
            {
                var dependency = d.dungeon;
                if (d.allow_partial && dependency.realm != dungeon.realm && dependency.realm != null)
                {
                    if (!realms.ContainsKey(dependency.realm.name))
                    {
                        realms[dependency.realm.name] = new Temp
                        {
                            realm = dependency.realm,
                            dependencies = new List<IDungeon>()
                        };
                    }
                    realms[dependency.realm.name].dependencies.Add(dependency);
                    lines = true;
                }
            }

            foreach (var r in realms.Values)
            {
                result += render_realm(r.realm, () =>
                   r.dependencies.Select(d =>
                       (Stroke)new Stroke_Token("class " + d.name + ";")).ToList()
                    );
            }

            return result;
        }

        List<Stroke> render_inner_dependencies(Dungeon dungeon)
        {
            bool lines = false;
            var result = new List<Stroke>();
            foreach (var d in dungeon.dependencies.Values)
            {
                var dependency = d.dungeon;
                if (d.allow_partial && dependency.realm == dungeon.realm)
                {
                    result.Add(new Stroke_Token("class ")
                        + render_dungeon_path(dependency) + new Stroke_Token(";"));

                    lines = true;
                }
            }

            //            if (result.Length > 0)
            //                result += new Stroke_Newline();

            return result;
        }

        Stroke render_includes(IEnumerable<External_Header> headers)
        {
            return new Stroke_List(Stroke_Type.statements, headers
                .Select(h => (Stroke)new Stroke_Token(h.is_standard
                    ? "#include <" + h.name + ">"
                    : "#include \"" + h.name + ".h\""
                    )).ToList()) { margin_bottom = 1 };
        }

        override protected Stroke render_variable_declaration(Declare_Variable declaration)
        {
            var result = new Stroke_Token("auto " + declaration.symbol.name)
                + (declaration.expression != null
                    ? new Stroke_Token(" = ") + render_expression(declaration.expression)
                    : null)
                + terminate_statement();

            result.expression = declaration;
            return result;
        }

        Stroke render_constructor(Dungeon dungeon)
        {
            Minion constructor;
            if (dungeon.has_minion("constructor"))
            {
                constructor = dungeon.minions["constructor"];
            }
            else
            {
                constructor = new Minion("constructor", dungeon);
                constructor.parameters = new List<Parameter>();
            }

            foreach (var portal in dungeon.portals)
            {
                if (portal.default_expression != null)
                {
                    var assignment = new Assignment(new Portal_Expression(portal), "=", portal.default_expression);
                    constructor.expressions.Insert(0, assignment);
                }
            }

            if (constructor.expressions.Count == 0)
                return new Stroke_Token();

            return render_function_definition(constructor, dungeon.name);
        }
    }
}
