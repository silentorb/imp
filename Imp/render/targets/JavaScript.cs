using System;
using System.Collections.Generic;
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
                    supports_enums = false,
                    supports_namespaces = false
                };
        }

        override public void run(string output_folder)
        {
            var output = generate();
            Generator.create_file(output_folder + "/" + "lib.js", output);
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

//        string render_statement(Expression statement)
//        {
//            Expression_Type type = statement.type;
//            switch (type)
//            {
//                case Expression_Type.space:
//                    var space = (Namespace)statement;
//                    return render_realm(space.realm, () => render_statements(space.children));
//
//                case Expression_Type.class_definition:
//                    var definition = (Class_Definition)statement;
//                    return class_definition(definition.dungeon, definition.children);
//
//                case Expression_Type.function_definition:
//                    return render_function_definition((Function_Definition)statement);
//
//                case Expression_Type.flow_control:
//                    return render_flow_control((Flow_Control)statement);
//
//                case Expression_Type.iterator:
//                    return render_iterator_block((Iterator)statement);
//
//                case Expression_Type.function_call:
//                    return line(render_function_call((Class_Function_Call)statement, null));
//
//                case Expression_Type.assignment:
//                    return render_assignment((Assignment)statement);
//
//                case Expression_Type.declare_variable:
//                    return render_variable_declaration((Declare_Variable)statement);
//
//                case Expression_Type.statement:
//                    var state = (Statement)statement;
//                    return line(state.name + (state.next != null
//                        ? " " + render_expression(state.next)
//                        : ""));
//
//                case Expression_Type.insert:
//                    return line(((Insert)statement).code);
//
//                default:
//                    return line(render_expression(statement));
//                //                    throw new Exception("Unsupported statement type: " + statement.type + ".");
//            }
//        }

        override protected string render_dungeon(Dungeon dungeon, IEnumerable<Expression> statements)
        {
            if (dungeon.is_abstract)
                return "";

            current_dungeon = dungeon;

            var result = line(render_dungeon_path(dungeon) + " = function() {}");
            var intro = render_dungeon_path(dungeon) + ".prototype =";
            result += add(intro) + render_scope(() =>
                render_properties(dungeon)
                + render_statements(statements, newline())
            );

            current_dungeon = null;

            return result;
        }

        override protected string render_properties(Dungeon dungeon)
        {
            var result = "";
            foreach (var portal in dungeon.core_portals.Values)
            {
                result += line(portal.name + ": " + get_default_value(portal) + ",");
            }

            return result;
        }

//        override protected string get_default_value(Portal portal)
//        {
//            if (portal.is_list)
//                return "[]";
//
//            return render_literal(portal.get_default_value(), portal.get_target_profession());
//        }

        override protected string render_variable_declaration(Declare_Variable declaration)
        {
            var profession = declaration.symbol.get_profession(overlord);
            var first = "var " + declaration.symbol.name;
            if (declaration.expression != null)
                first += " = " + render_expression(declaration.expression);

            current_scope[declaration.symbol.name] = profession;
            return line(first);
        }

        override protected string render_function_definition(Function_Definition definition)
        {
            if (definition.is_abstract)
                return "";

            var minion = definition.minion;

            // Search for any case of "this" inside an anonymous function.
            var minions = definition.find(Expression_Type.anonymous_function);
            if (minions.Any(m => m.find(e=>e.type == Expression_Type.self || e.type == Expression_Type.property_function_call).Any()))
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

//        private string render_expression(Expression expression, Expression parent = null)
//        {
//            string result;
//            switch (expression.type)
//            {
//                case Expression_Type.literal:
//                    var literal = (Literal) expression;
//                    return render_literal(literal.value, literal.profession);
//
//                case Expression_Type.operation:
//                    return render_operation((Operation)expression);
//
//                case Expression_Type.portal:
//                    var portal_expression = (Portal_Expression)expression;
//                    result = portal_expression.portal.name;
//                    if (portal_expression.parent.next == null)
//                        result = "this." + result;
//
//                    if (portal_expression.index != null)
//                        result += "[" + render_expression(portal_expression.index) + "]";
//
//                    break;
//
//                case Expression_Type.function_call:
//                    result = render_function_call((Class_Function_Call)expression, parent);
//                    break;
//
//                case Expression_Type.property_function_call:
//                    result = render_property_function_call((Property_Function_Call)expression, parent);
//                    break;
//
//                case Expression_Type.platform_function:
//                    return render_platform_function_call((Platform_Function)expression, null);
//
//                case Expression_Type.instantiate:
//                    result = render_instantiation((Instantiate)expression);
//                    break;
//
//                case Expression_Type.self:
//                    result = "this";
//                    break;
//
//                case Expression_Type.null_value:
//                    return "null";
//
//                case Expression_Type.profession:
//                    result = expression.get_profession().dungeon.name;
//                    break;
//
//                case Expression_Type.create_array:
//                    result = "FOOO";
//                    break;
//
//                case Expression_Type.anonymous_function:
//                    return render_anonymous_function((Anonymous_Function)expression);
//
//                case Expression_Type.comment:
//                    return render_comment((Comment)expression);
//
//                case Expression_Type.variable:
//                    var variable_expression = (Variable)expression;
//                    //if (find_variable(variable_expression.symbol.name) == null)
//                    //    throw new Exception("Could not find variable: " + variable_expression.symbol.name + ".");
//
//                    result = variable_expression.symbol.name;
//                    if (variable_expression.index != null)
//                        result += "[" + render_expression(variable_expression.index) + "]";
//
//                    break;
//
//                case Expression_Type.parent_class:
//                    result = current_dungeon.parent.name;
//                    break;
//
//                case Expression_Type.insert:
//                    result = ((Insert)expression).code;
//                    break;
//
//                default:
//                    throw new Exception("Unsupported Expression type: " + expression.type + ".");
//            }
//
//            if (expression.next != null)
//            {
//                result += "." + render_expression(expression.next, expression);
//            }
//
//            return result;
//        }

//        string render_literal(Object value, Profession profession)
//        {
//            if (profession == null)
//                return value.ToString();
//
//            switch (profession.type)
//            {
//                case Kind.unknown:
//                    return value.ToString();
//
//                case Kind.Float:
//                    return value.ToString();
//
//                case Kind.Int:
//                    return value.ToString();
//
//                case Kind.String:
//                    return "\"" + value + "\"";
//
//                case Kind.Bool:
//                    return (bool)value ? "true" : "false";
//
//                case Kind.reference:
//                    if (!profession.dungeon.is_value)
//                        throw new Exception("Literal expressions must be scalar values.");
//
//                    if (value != null)
//                        return value.ToString();
//
//                    return render_trellis_name(profession.dungeon) + "()";
//
//                default:
//                    throw new Exception("Invalid literal " + value + " type " + profession.type + ".");
//            }
//        }
//
//        string render_trellis_name(Dungeon dungeon)
//        {
//            if (dungeon.realm != current_realm)
//                return render_realm_name(dungeon.realm) + "." + dungeon.name;
//
//            return dungeon.name;
//        }

//        string render_realm_name(Realm realm)
//        {
//            var path = Generator.get_namespace_path(realm);
//            return path.join(".");
//        }

//        public string render_scope2(string intro, List<Expression> statements, bool minimal = false)
//        {
//            indent();
//            push_scope();
//            var lines = line_count;
//            var block = render_statements(statements);
//            pop_scope();
//            unindent();
//
//            if (minimal)
//            {
//                minimal = line_count == lines + 1;
//            }
//            var result = line(intro + (minimal ? "" : " {"));
//            result += block;
//            result += line((minimal ? "" : "}"));
//            return result;
//        }

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

      override protected  string render_iterator(Symbol parameter, Expression expression)
        {
            var path_string = render_expression(expression);
            return
                "::const_iterator " + parameter.name + " = "
                + path_string + ".begin(); " + parameter.name + " != "
                + path_string + ".end(); " + parameter.name + "++";
        }


//        private string render_property_function_call(Property_Function_Call expression, Expression parent)
//        {
//            var ref_full = expression.reference != null
//                ? render_expression(expression.reference) + "."
//                : "";
//
//            ref_full = "this." + ref_full;
//
//            var args = expression.args.Select(e => render_expression(e)).join(", ");
//            var portal = expression.portal;
//            var setter = portal.setter;
//            if (setter != null)
//                return ref_full + setter.name + "(" + args + ")";
//
//            return expression.portal.is_list
//                ? ref_full + portal.name + "." + "push(" + args + ")"
//                : ref_full + portal.name + " = " + args;
//        }

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
