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
                    if (!Cpp.has_header(headers, "stdlib"))
                        headers.Add(new External_Header("stdlib", true));
                }
            }

            return
                Cpp.render_includes(headers) + new Stroke_Newline() + new Stroke_Newline()
                + render_dungeon(dungeon);
        }

        static Stroke render_dungeon(Dungeon dungeon)
        {
            if (dungeon.realm != null && dungeon.realm.name != "")
            {
                return render_realm(dungeon.realm, () =>
                    render_minions(dungeon));
            }

            return new Stroke_List(Stroke_Type.statements,
                render_minions(dungeon));
        }


        static List<Stroke> render_minions(Dungeon dungeon)
        {
            return new List<Stroke> { render_constructor(dungeon) }
                .Concat(dungeon.minions.Values
                .Where(m => m.name != "constructor")
                .Select(f => render_function_definition(f, f.name))).ToList();
        }

        static Stroke render_constructor(Dungeon dungeon)
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

        static Stroke render_function_definition(Minion definition, string name)
        {
            if (definition.is_abstract)
                return new Stroke_Token();

            var a = (definition.return_type != null
                ? Cpp.render_profession2(definition.return_type) + new Stroke_Token(" ")
                : new Stroke_Token());

            var intro = a
                + new Stroke_Token(definition.dungeon.name + "::" + name)
                + new Stroke_Token("(")
                + new Stroke_Token(definition.parameters.Select(p => Cpp.render_definition_parameter2(p).full_text()).@join(", "))
                + new Stroke_Token(")");

            return intro + Common_Functions.render_block(Common_Functions.render_statements(definition.expressions), false) + new Stroke_Newline();
        }

    }
}
