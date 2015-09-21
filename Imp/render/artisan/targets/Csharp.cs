using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.legion;
using imperative.schema;
using metahub.render;
using metahub.schema;

namespace imperative.render.artisan.targets
{
    public class Csharp : Common_Target2
    {
        public Csharp(Overlord overlord = null)
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
                    foreach_symbol = "foreach"
                };

            types["reference"] = "object";
        }

        override public void run(Build_Orders config1, string[] sources)
        {
            foreach (var dungeon in overlord.dungeons)
            {
                if (dungeon.is_external || dungeon.realm.is_external
                    || (dungeon.is_abstract && dungeon.is_external))
                    continue;

                var strokes = generate_dungeon_file_contents(dungeon);
                var path = config1 + "/" + render_realm_path(dungeon.realm, "/");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                var output = render_strokes(strokes);
                Generator.create_file(path + "/" + dungeon.name + ".cs", output);
            }
        }

        public override void build_wrapper_project(Project config1)
        {

        }

        public List<Stroke> generate_dungeon_file_contents(Dungeon dungeon)
        {
            return render_dependencies(dungeon).Concat(new Stroke[] {
                render_realm(dungeon.realm, ()=> new List<Stroke>{ render_dungeon(dungeon) })
                   }).ToList();
        }

        virtual protected Stroke[] render_dependencies(Dungeon dungeon)
        {
            var dependencies = new[]
                {
                    "System",
                    "System.Collections.Generic",
                    "System.Linq",
                    "System.Text"
                };

            return dependencies.Select(d =>
                new Stroke_Token(config.dependency_keyword + " " + d) + terminate_statement()
            ).Concat(

            dungeon.needed_realms.Select(d =>
                new Stroke_Token(config.dependency_keyword + " ") + render_realm_path(d, config.namespace_separator) + terminate_statement()
            )).ToArray();
        }

        override protected Stroke render_platform_function_call(Platform_Function expression, Expression parent)
        {
            Stroke ref_string, ref_full = null;

            if (expression.reference != null)
            {
                ref_string = render_expression(expression.reference);
                ref_full = ref_string + get_connector(expression.reference.get_end());
            }

            switch (expression.name)
            {
                case "count":
                    return Stroke.chain(ref_full, new Stroke_Token("length"));

                case "add":
                    {
                        var first = render_expression(expression.args[0]);
                        //var dereference = is_pointer(expression.args.Last().get_signature()) ? "*" : "";
                        return Stroke.chain(ref_full, new Stroke_Token("push(") + first + new Stroke_Token(")"));
                    }

                case "contains":
                    {
                        var first = render_expression(expression.args[0]);
                        return Stroke.chain(ref_full, new Stroke_Token("Contains(") + first + new Stroke_Token(")"));
                    }

                case "distance":
                    {
                        var first = render_expression(expression.args[0]);
                        return Stroke.chain(
                            ref_full,
                            new Stroke_Token("distance(") + first + new Stroke_Token(")"));
                    }

                case "first":
                    return new Stroke_Token("[0]");

                case "last":
                    return Stroke.chain(ref_full, new Stroke_Token("back()"));

                case "pop":
                    return Stroke.chain(ref_full, new Stroke_Token("pop_back()"));

                case "remove":
                    {
                        var first = render_expression(expression.args[0]);
                        return Stroke.chain(ref_full, new Stroke_Token("Remove(")
                            + first + new Stroke_Token(")"));
                    }

                case "rand":
                    float min = ((Literal)expression.args[0]).get_float();
                    float max = ((Literal)expression.args[1]).get_float();
                    return new Stroke_Token("(float)metahub.Hub.random.NextDouble() * " + (max - min) + (min < 0 ? " - " + -min : " + " + min));

                default:
                    throw new Exception("Unsupported platform-specific function: " + expression.name + ".");
            }
        }

        public override Stroke listify(Stroke type, Profession signature)
        {
            return signature.is_array(overlord)
                ? render_profession(signature.children[0]) + new Stroke_Token("[]")
                : new Stroke_Token("List<") + type + new Stroke_Token(">");
        }

        protected override Stroke render_list(Profession profession, List<Expression> args)
        {
            var arg_string = args != null
                ? args.Select(a => render_expression(a)).join(", ")
                : "";

            if (profession.is_array(overlord))
            {
                var text = render_profession(profession);
                //                return "new " + text.Substring(0, text.Length - 2) + "[" + arg_string + "]";
                return new Stroke_Token("new ") + text + new Stroke_Token("[" + arg_string + "]");
            }

            return new Stroke_Token("new ") + render_profession(profession) + new Stroke_Token("(" + arg_string + ")");
        }
    }
}
