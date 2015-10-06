using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.schema;
using metahub.render.targets;
using imperative.expressions;
using metahub.render;

namespace imperative.render.artisan.targets.cpp
{
    public static class Header_File
    {
        public static Stroke generate_header_file(Cpp target, Dungeon dungeon)
        {
            List<External_Header> headers = new List<External_Header>
            {
                new External_Header("string", true),
                new External_Header("vector", true),
                new External_Header("map", true),
                new External_Header("memory", true) 
            };

            if (!dungeon.is_enum)
            {
                headers = headers.Concat(
                    dungeon.dependencies.Values.Where(d => !d.allow_partial && !d.dungeon.is_standard)
                        .OrderBy(d => Cpp.render_source_path(d.dungeon))
                        .Select(d => new External_Header(Cpp.render_source_path(d.dungeon)))
                    )
                    .ToList();
            }

            var result = new Stroke_Token("#pragma once") + new Stroke_Newline()
                + Cpp.render_includes(headers) + new Stroke_Newline() + new Stroke_Newline()
                + render_outer_dependencies(target, dungeon)
                + target.render_realm2(dungeon.realm, () =>
                    render_inner_dependencies(target, dungeon)
                    .Concat(render_body(target, dungeon))
                    .ToList());

            return result;
        }

        private static IEnumerable<Stroke> render_body(Cpp target, Dungeon dungeon)
        {
            if (dungeon.is_enum)
                return new[] { enum_declaration(target, dungeon) };

            if (dungeon.is_value)
                return new[] { class_declaration(target, dungeon) };

            return new[]
            {
                class_declaration(target, dungeon),
                render_pointer_alias(dungeon)
            };
        }

        private static Stroke render_pointer_alias(Dungeon dungeon)
        {
            if (dungeon.generic_parameters.Count > 0)
                return new Stroke_Token();

            return new Stroke_Token("typedef std::shared_ptr<" + dungeon.name + "> " + dungeon.name + "_Pointer;");
        }

        private class Temp
        {
            public Dungeon realm;
            public List<IDungeon> dependencies;
        }

        static Stroke render_outer_dependencies(Cpp target, Dungeon dungeon)
        {
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
                }
            }

            if (realms.Count == 0)
                return new Stroke_Token();

            foreach (var r in realms.Values)
            {
                result += target.render_realm(r.realm, () =>
                   r.dependencies.Select(d =>
                       (Stroke)new Stroke_Token("class " + d.name + ";")).ToList()
                    );
            }

            return result + new Stroke_Newline();
        }

        static List<Stroke> render_inner_dependencies(Cpp target, Dungeon dungeon)
        {
            var result = new List<Stroke>();
            foreach (var d in dungeon.dependencies.Values)
            {
                var dependency = d.dungeon;
                if (d.allow_partial && dependency.realm == dungeon.realm)
                {
                    Stroke addition = new Stroke_Token("class ")
                                      + target.render_dungeon_path(dependency) + new Stroke_Token(";");

                    var current = (Dungeon)d.dungeon;
                    if (current.generic_parameters.Count > 0)
                    {
                        addition = render_template_prefix(dungeon) + new Stroke_Newline() + addition;
                    }
                    result.Add(addition);
                }
            }

            return result;
        }

        static Stroke render_template_prefix(Dungeon dungeon)
        {
            return new Stroke_Token("template <" + dungeon.generic_parameters.Keys
                     .Select(p => "typename " + p).join(", ") + ">");
        }

        static Stroke class_declaration(Cpp target, Dungeon dungeon)
        {
            target.current_dungeon = dungeon;
            Stroke first = new Stroke_Token(dungeon.is_value ? "struct " : "class ");
            var context = new Render_Context(dungeon.realm, Cpp.static_config,
              Cpp.statement_router, target);

            if (dungeon.generic_parameters.Count > 0)
            {
                first = render_template_prefix(dungeon) + new Stroke_Newline() + first;
            }

            if (dungeon.class_export.Length > 0)
                first += new Stroke_Token(dungeon.class_export + " ");

            first += new Stroke_Token(dungeon.name);
            var parents = Cpp.get_dungeon_parents(dungeon);

            if (parents.Count > 0)
            {
                first += new Stroke_Token(" : ") + Stroke.join(
                    parents.Select(p => new Stroke_Token("public ") +
                        Cpp.render_profession2(p, context, false, true)).ToList(), ", ");
            }

            var lines = new List<Stroke>();

            if (!dungeon.is_value)
                lines.Add(new Stroke_Token("public:"));

            foreach (var portal in dungeon.core_portals.Values)
            {
                lines.Add(property_declaration(target, portal));
            }

            foreach (var portal in dungeon.all_portals.Values.Except(dungeon.core_portals.Values))
            {
                if (portal.dungeon.is_abstract)
                    lines.Add(property_declaration(target, portal));
            }

            lines.AddRange(render_function_declarations(target, dungeon));

            return first + target.render_block(lines, false) + new Stroke_Token(";");
        }

        static Stroke enum_declaration(Cpp target, Dungeon dungeon)
        {
            target.current_dungeon = dungeon;
            Stroke first = new Stroke_Token("enum ");
            var context = new Render_Context(dungeon.realm, Cpp.static_config,
              Cpp.statement_router, target);

            if (dungeon.class_export.Length > 0)
                first += new Stroke_Token(dungeon.class_export + " ");

            first += new Stroke_Token(dungeon.name);

            var lines = new List<Stroke>();

            var i = 1;
            foreach (var portal in dungeon.core_portals.Values)
            {
                var line = new Stroke_Token(portal.name + (i == dungeon.core_portals.Count ? "" : ","));
                lines.Add(line);
                ++i;
            }

            return first + target.render_block(lines, false) + new Stroke_Token(";");
        }

        static Stroke property_declaration(Cpp target, Portal portal)
        {
            var prefix = "";
            if (portal.has_enchantment(Enchantments.Static))
            {
                prefix += "static ";
            }
            return new Stroke_Token(prefix) + target.render_profession(portal.get_profession()) + new Stroke_Token(" " + portal.name + ";");
        }

        static List<Stroke> render_function_declarations(Cpp target, Dungeon dungeon)
        {
            //            var declarations = dungeon.stubs.Select(line).ToList();
            var declarations = new List<Stroke>();
            //
            //            if (dungeon.hooks.ContainsKey("initialize_post"))
            //            {
            //                declarations.Add(line("void initialize_post(); // Externally defined."));
            //            }

            declarations.AddRange(dungeon.minions.Values.Select(d => render_function_declaration(target, d)));

            return declarations;
        }

        static Stroke render_function_declaration(Cpp target, Minion definition)
        {
            if (definition.generic_parameters.Count > 0)
            {
                var context = new Render_Context(definition.dungeon.realm, Cpp.static_config, Cpp.statement_router, target);
                return Source_File.render_function_intro(definition, context, definition.name)
                + Source_File.render_function_body(definition, context);
            }

            return render_function_declaration_intro(target, definition)
                + new Stroke_Token("(")
                + Stroke.join(definition.parameters.Select(p => render_declaration_parameter(target, p)), ", ")
                + new Stroke_Token(")")
                + new Stroke_Token(definition.is_abstract ? " = 0;" : ";");
        }

        static Stroke render_function_declaration_intro(Cpp target, Minion definition)
        {
            if (definition.name == "constructor")
            {
                return new Stroke_Token(definition.dungeon.name);
            }

            return new Stroke_Token("virtual ")
                   + (definition.return_type != null
                       ? target.render_profession(definition.return_type)
                         + new Stroke_Token(" ")
                       : new Stroke_Token(""))
                   + new Stroke_Token(definition.name);
        }

        static Stroke render_declaration_parameter(Cpp target, Parameter parameter)
        {
            return target.render_profession(parameter.symbol.profession, true) + new Stroke_Token(" " + parameter.symbol.name)
                   + (parameter.default_value != null
                          ? new Stroke_Token(" = ") + target.render_expression(parameter.default_value)
                          : new Stroke_Token()
                     );
        }

    }
}
