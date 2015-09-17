using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.schema;
using metahub.render;
using metahub.render.targets;

namespace imperative.render.artisan.targets.cpp
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
                CMake.create_files(config1, overlord);
           
            foreach (var dungeon in config1.dungeons.Values)
            {
                if (dungeon.is_external || (dungeon.is_abstract && dungeon.is_external))
                    continue;

                render_full_dungeon(dungeon, config1);
                //Console.WriteLine(dungeon.realm.name + "." + dungeon.name);
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
                    Overlord.stroke_to_string(Header_File.generate_header_file(this, dungeon)));

                Generator.create_file(name + ".cpp",
                    Overlord.stroke_to_string(generate_class_file(dungeon)));
            }

//            foreach (var child in dungeon.dungeons.Values)
//            {
//                render_full_dungeon(child, config1);
//            }
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

       public static List<Dungeon> get_dungeon_parents(Dungeon dungeon)
        {
            var parents = new List<Dungeon>();
            if (dungeon.parent != null)
                parents.Add(dungeon.parent);

            return parents.Concat(dungeon.interfaces).ToList();
        }

        override public Stroke render_profession(Profession signature, bool is_parameter = false)
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
                render_includes(this,headers) + new Stroke_Newline() + new Stroke_Newline()
//                + render_outer_dependencies(dungeon)
                + render_dungeon(dungeon);
        }

        public static Stroke render_includes(Cpp target, IEnumerable<External_Header> headers)
        {
            return new Stroke_List(Stroke_Type.statements, headers
                .Select(h => (Stroke)new Stroke_Token(h.is_standard
                    ? "#include <" + h.name + ">"
                    : "#include \"" + h.name + ".h\""
                    )).ToList()) { margin_bottom = 1 };
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

        override public Stroke listify(Stroke type, Profession signature)
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
