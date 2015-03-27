using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.schema;
using metahub.render;

namespace imperative.render.targets
{
    public class Csharp : Common_Target
    {
        public Csharp(Overlord overlord = null)
            : base(overlord)
        {
            config = new Target_Configuration()
                {
                    statement_terminator = ";",
                    dependency_keyword = "using",
                    space_tabs = true,
                    indent = 4,
                    block_brace_same_line = false,
                    explicit_public_members = true,
                    type_mode = Type_Mode.required_prefix
                };
        }

        public string generate_dungeon_file_contents(Dungeon dungeon)
        {
            return render_dependencies(dungeon) + newline()
                   + render_statements(dungeon.code);
        }

        public string generate_enum_file_contents(Treasury treasury)
        {
            return render_realm(treasury.realm, ()=> render_treasury(treasury));
        }

        virtual protected string render_dependencies(Dungeon dungeon)
        {
            var dependencies = new[]
                {
                    "System",
                    "System.Collections.Generic",
                    "System.Linq",
                    "System.Text"
                };

            return dependencies.Select(d =>
                line(config.dependency_keyword + " " + d + terminate_statement())
            ).join("");
        }

        override protected string render_platform_function_call(Platform_Function expression, Expression parent)
        {
            var ref_string = expression.reference != null
                                 ? render_expression(expression.reference)
                                 : "";

            var ref_full = ref_string.Length > 0
                               ? ref_string + get_connector(expression.reference.get_end())
                               : "";

            switch (expression.name)
            {
                case "count":
                    return ref_full + "size()";

                case "add":
                    {
                        var first = render_expression(expression.args[0]);
                        //var dereference = is_pointer(expression.args.Last().get_signature()) ? "*" : "";
                        return ref_full + "Add(" + first + ")";
                    }

                case "contains":
                    {
                        var first = render_expression(expression.args[0]);
                        return "std::find(" + ref_full + "begin(), "
                               + ref_full + "end(), " + first + ") != " + ref_full + "end()";
                    }

                case "distance":
                    {
                        //var signature = expression.args[0].get_signature();
                        var first = render_expression(expression.args[0]);
                        //var dereference = is_pointer(signature) ? "*" : "";
                        return ref_full + "distance(" + first + ")";
                    }

                case "first":
                    return "[0]";

                case "last":
                    return ref_full + "back()";

                case "pop":
                    return ref_full + "pop_back()";

                case "remove":
                    {
                        var first = render_expression(expression.args[0]);
                        return ref_full + "erase(std::remove(" + ref_full + "begin(), "
                               + ref_full + "end(), " + first + "), " + ref_full + "end())";
                    }

                case "rand":
                    float min = ((Literal)expression.args[0]).get_float();
                    float max = ((Literal)expression.args[1]).get_float();
                    return "rand() % " + (max - min) + (min < 0 ? " - " + -min : " + " + min);

                default:
                    throw new Exception("Unsupported platform-specific function: " + expression.name + ".");
            }
        }

        protected override string listify(string type)
        {
            return "List<" + type + ">";
        }

        protected override string instantiate_list(Portal portal)
        {
            return "new " + render_profession(portal.profession) + "()";
        }
    }
}
