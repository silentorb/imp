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
                explicit_public_members = true,
                type_mode = Type_Mode.required_prefix,
                namespace_separator = "::",
                list_start = "{",
                list_end = "}"
            };

            types["string"] = "std::string";
            minion_names[Professions.List.minions["push"]] = "push_back";
        }

        public override void run(Overlord_Configuration config1, string[] sources)
        {

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
                + render_outer_dependencies(dungeon)
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
                .Select(render_function_definition)).ToList();
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

        string render_inner_dependencies(Dungeon dungeon)
        {
            bool lines = false;
            var result = "";
            foreach (var d in dungeon.dependencies.Values)
            {
                var dependency = d.dungeon;
                if (d.allow_partial && dependency.realm == dungeon.realm)
                {
                    result += new Stroke_Token("class ")
                        + render_dungeon_path(dependency) + new Stroke_Token(";");

                    lines = true;
                }
            }

            if (result.Length > 0)
                result += new Stroke_Newline();

            return result;
        }

        Stroke render_includes(IEnumerable<External_Header> headers)
        {
            return new Stroke_List(Stroke_Type.statements, headers
                .Select(h => (Stroke)new Stroke_Token(h.is_standard
                    ? "#include <" + h.name + ".h>"
                    : "#include \"" + h.name + ".h\""
                    )).ToList());
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

            return render_function_definition(constructor, dungeon.name);
        }
    }
}
