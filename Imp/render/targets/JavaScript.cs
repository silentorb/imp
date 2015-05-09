using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using imperative;
using imperative.render;
using imperative.schema;

using imperative.expressions;
using metahub.schema;

namespace metahub.render.targets
{
    public class JavaScript : Common_Target
    {
        public JavaScript(Overlord overlord = null)
            : base(overlord)
        {
            config = new Target_Configuration
                {
                    implicit_this = false,
                    supports_abstract = false,
                    supports_enums = false,
                    supports_namespaces = false
                };
        }

        override public void run(Overlord_Configuration settings)
        {
            var output = generate();
            var output_path = File.Exists(settings.input)
                ? settings.output + Path.GetFileNameWithoutExtension(settings.input) + ".js"
                : settings.output + "/" + "lib.js";

            Generator.create_file(output_path, output);
        }

        public string generate()
        {
            var output = "";
            foreach (var dungeon in overlord.dungeons)
            {
                if (dungeon.is_external || (dungeon.is_abstract && dungeon.is_external))
                    continue;

                //Console.WriteLine(dungeon.realm.name + "." + dungeon.name);

                var space = Generator.get_namespace_path(dungeon.realm);

                line_count = 0;
                output += create_class_file(dungeon);
            }

            return output;
        }

        string create_class_file(Dungeon dungeon)
        {
            render = new Renderer();
            var result = render_statements(dungeon.code);

            return result;
        }

        override protected string render_dungeon(Dungeon dungeon, IEnumerable<Expression> statements)
        {
            if (dungeon.is_abstract)
                return "";

            current_dungeon = dungeon;

            var i = 0;
            var total = dungeon.core_portals.Count + statements.Count();
            String_Delegate2 render_line = text => ++i < total
                    ? text + "," + newline()
                    : text;

            var result = line(render_dungeon_path(dungeon) + " = function() {}");
            var intro = render_dungeon_path(dungeon) + ".prototype =";
            result += add(intro) + render_scope(() =>
                dungeon.core_portals.Values.Select(portal =>
                    render_line(add(portal.name + ": " + get_default_value(portal))))
                .join("")
                +
                statements.Select(s => render_line(render_statement(s)))
                .join("")
                + newline()
            );

            current_dungeon = null;

            return result;
        }

        override protected string render_properties(Dungeon dungeon)
        {
            var result = "";
            foreach (var portal in dungeon.core_portals.Values)
            {
                result += line(portal.name + ": " + get_default_value(portal));
            }

            return result;
        }

        override protected string render_variable_declaration(Declare_Variable declaration)
        {
            var profession = declaration.symbol.get_profession(overlord);
            var first = add("var " + declaration.symbol.name);
            if (declaration.expression != null)
                first += " = " + render_expression(declaration.expression);

            current_scope[declaration.symbol.name] = profession;
            return first + newline();
        }

        override protected string render_function_definition(Function_Definition definition)
        {
            if (definition.is_abstract)
                return "";

            var minion = definition.minion;

            // Search for any case of "this" inside an anonymous function.
            var minions = definition.find(Expression_Type.anonymous_function);
            if (minions.Any(m => m.find(e => e.type == Expression_Type.self || e.type == Expression_Type.property_function_call).Any()))
            {
                var self = minion.scope.create_symbol("self", new Profession(Kind.reference, current_dungeon));
                minion.expressions.Insert(0, new Declare_Variable(self, new Self(minion.dungeon)));
            }

            return render.get_indentation() + definition.name + ": function(" + definition.parameters.Select(p => p.symbol.name).join(", ") + ")"
                + render_minion_scope(minion);
        }

        protected override string render_this()
        {
            return current_minion.GetType() == typeof(Ethereal_Minion)
                && current_minion.scope.find_or_null("self") != null
                ? "self"
                : "this";
        }

        override protected string render_iterator_block(Iterator statement)
        {
            var parameter = statement.parameter;
            var it = parameter.scope.create_symbol("it", parameter.profession);
            var expression = render_iterator(it, statement.expression);

            var result = add("for (" + expression + ")") + render_scope(new List<Expression> { 
                    new Declare_Variable(parameter, new Insert("*" + it.name))
                }.Concat(statement.body).ToList()
            );
            return result;
        }

        //        string render_operation(Operation operation)
        //        {
        //            return operation.children.Select(c =>
        //                c.type == Expression_Type.operation && ((Operation)c).is_condition() == operation.is_condition()
        //                ? "(" + render_expression(c) + ")"
        //                : render_expression(c)
        //            ).join(" " + operation.op + " ");
        //        }

        override protected string render_iterator(Symbol parameter, Expression expression)
        {
            var path_string = render_expression(expression);
            return
                "::const_iterator " + parameter.name + " = "
                + path_string + ".begin(); " + parameter.name + " != "
                + path_string + ".end(); " + parameter.name + "++";
        }

        override protected string render_scope(String_Delegate action)
        {
            push_scope();
            var result = config.block_brace_same_line
                ? " {" + newline()
                : newline() + line("{");

            indent();
            result += action();
            unindent();
            result += add("}");
            pop_scope();
            return result;
        }

        override protected string render_platform_function_call(Platform_Function expression, Expression parent)
        {
            var ref_string = expression.reference != null
          ? render_expression(expression.reference)
          : "";

            var ref_full = ref_string.Length > 0
                ? ref_string + "."
                : "";

            switch (expression.name)
            {
                case "count":
                    return ref_full + "size()";

                case "add":
                    {
                        var first = render_expression(expression.args[0]);
                        //var dereference = is_pointer(expression.args.Last().get_signature()) ? "*" : "";
                        return ref_full + "push(" + first + ")";
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

        //        string render_function_call(Class_Function_Call expression, Expression parent)
        //        {
        //            var ref_string = expression.reference != null
        //               ? render_expression(expression.reference)
        //               : "";
        //
        //            var ref_full = ref_string.Length > 0
        //                ? ref_string + "."
        //                : "";
        //
        //            return ref_full + expression.name + "(" +
        //                expression.args.Select(a => render_expression(a))
        //                .join(", ") + ")";
        //        }
        //
        //        string render_assignment(Assignment statement)
        //        {
        //            return line(render_expression(statement.target) + " " + statement.op + " " + render_expression(statement.expression));
        //        }
        //
        //        string render_comment(Comment comment)
        //        {
        //            return comment.is_multiline
        //                ? "/* " + comment.text + "*/"
        //                : "// " + comment.text;
        //        }
        //
        //        string render_instantiation(Instantiate expression)
        //        {
        //            var args = expression.args.Select(a => render_expression(a)).join(", ");
        //            return "new " + full_dungeon_name(expression.dungeon) + "(" + args + ")";
        //        }

        override protected string render_realm(Realm realm, String_Delegate action)
        {
            if (realm.name == "")
                return action();

            var result = line("var " + realm.name + " = {}") + newline();

            current_realm = realm;
            var body = action();
            current_realm = null;

            if (body == "")
                return "";

            result += body;
            return result;
        }
    }
}
