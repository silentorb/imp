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
    public static class Source_File
    {

        public static Stroke generate_source_file(Cpp target, Dungeon dungeon)
        {
            var headers = new List<External_Header> { new External_Header(Cpp.render_source_path(dungeon)) }
                .Concat(get_dependency_headers(dungeon))
                .OrderBy(h => h.name)
                .ToList();

            foreach (var func in dungeon.used_functions.Values)
            {
                if (func.name == "rand" && func.is_platform_specific)
                {
                    if (!Cpp.has_header(headers, "stdlib"))
                        headers.Add(new External_Header("stdlib", true));
                }
            }

            var context = new Render_Context(dungeon.realm, Cpp.static_config, Cpp.statement_router, target);
            return
                Cpp.render_includes(headers) + new Stroke_Newline() + new Stroke_Newline()
                + render_dungeon(dungeon, context);
        }

        static IEnumerable<External_Header> get_dependency_headers(Dungeon dungeon)
        {
            return dungeon.dependencies.Values
                .Where(d => !d.dungeon.is_standard && (dungeon.parent == null || d.dungeon != dungeon.parent.dungeon))
                .Select(d => new External_Header(Cpp.render_source_path(d.dungeon)));
        }

        static Stroke render_dungeon(Dungeon dungeon, Render_Context context)
        {
            if (dungeon.realm != null && dungeon.realm.name != "")
            {
                return ((Cpp)context.target).render_realm2(dungeon.realm, () =>
                    render_minions(dungeon, context));
            }

            return new Stroke_List(Stroke_Type.statements,
                render_minions(dungeon, context));
        }


        static List<Stroke> render_minions(Dungeon dungeon, Render_Context context)
        {
            return new List<Stroke> { render_constructor(dungeon, context) }
                .Concat(dungeon.minions.Values
                .Where(m => m.name != "constructor")
                .Select(f => render_function_definition(f, context, f.name))).ToList();
        }

        static Stroke render_constructor(Dungeon dungeon, Render_Context context)
        {
            if (!dungeon.has_minion("constructor"))
                return new Stroke_Token();

            var constructor = dungeon.minions["constructor"];
            return render_function_definition(constructor, context, dungeon.name);
        }

        public static Stroke render_function_definition(Minion definition, Render_Context context, string name)
        {
            if (definition.is_abstract || definition.generic_parameters.Count > 0)
                return new Stroke_Token();

            return render_function_intro(definition, context, definition.dungeon.name + "::" + name)
                + render_function_body(definition, context);
        }

        public static Stroke render_function_intro(Minion definition, Render_Context context, string name)
        {
            var a = (definition.return_type != null
             ? Cpp.render_profession2(definition.return_type, context) + new Stroke_Token(" ")
             : new Stroke_Token());

            return a
                + new Stroke_Token(name)
                + new Stroke_Token("(")
                + new Stroke_Token(definition.parameters.Select(p => context.target.render_definition_parameter(p).full_text()).@join(", "))
                + new Stroke_Token(")");
        }

        public static Stroke render_function_body(Minion definition, Render_Context context)
        {
            return Common_Functions.render_block(context.target
                .render_statements(definition.expressions), false) +
                   new Stroke_Newline();
        }

    }
}
