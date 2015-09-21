using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.schema;

namespace imperative.render.artisan
{
    static class Common_Functions
    {
        public static Stroke render_realm2(Dungeon realm, Target_Configuration config, Stroke_List_Delegate action)
        {
            if (realm == null || realm.name == "")
                return new Stroke_Token() + action();

            var result = new Stroke_Token(config.namespace_keyword + " ")
                + render_realm_path(realm, config.namespace_separator)
                + render_block(action(), false);

            return result;
        }

        public static Stroke render_realm_path(Dungeon realm, string separator)
        {
            return new Stroke_Token(realm.parent != null && realm.parent.name != ""
                ? render_realm_path(realm.parent.dungeon, separator) + separator + realm.name
                : realm.name);
        }

        public static Stroke render_block(List<Stroke> strokes, bool try_minimal = true, 
            bool is_succeeded = false)
        {
            var block = new Stroke_List(Stroke_Type.block, strokes);
            if (strokes.Count == 1 && try_minimal)
            {
                return is_succeeded
                ? block
                : block + new Stroke_Newline { ignore_on_block_end = true };
            }

            return new Stroke_Token(" {") + new Stroke_List(Stroke_Type.block, strokes)
                + new Stroke_Newline()
                + new Stroke_Token("}")
                ;
        }

        public static List<Stroke> render_statements(IEnumerable<Expression> statements, 
            Render_Context context)
        {
            return statements.Select(s => render_statement(s, context)).ToList();
        }

        public static Stroke render_statement(Expression statement, Render_Context context)
        {
            var router = context.router;
            var target = context.target;
            Expression_Type type = statement.type;
            switch (type)
            {
                case Expression_Type.function_definition:
                    var minion = ((Function_Definition) statement).minion;
                    return router.render_function_definition(minion, context,minion.name);

                case Expression_Type.flow_control:
                    var flow_control = (Flow_Control)statement;
                    return router.render_flow_control(flow_control, flow_control.flow_type == Flow_Control_Type.If);

                case Expression_Type.iterator:
                    return router.render_iterator((Iterator)statement);

                case Expression_Type.assignment:
                    return router.render_assignment((Assignment)statement);

                case Expression_Type.comment:
                    return router.render_comment((Comment)statement);

                case Expression_Type.declare_variable:
                    return router.render_variable_declaration((Declare_Variable)statement);

                case Expression_Type.statement:
                    var state = (Statement)statement;
                    var statement_result = (Stroke)new Stroke_Token(state.name);
                    if (state.next != null)
                        statement_result += new Stroke_Token(" ") + target.render_expression(state.next);

                    statement_result.expression = statement;
                    return statement_result + target.terminate_statement();

                case Expression_Type.insert:
                    return new Stroke_Token(((Insert)statement).code);

                default:
                    return target.render_expression(statement) + target.terminate_statement();
            }
        }
        /*
        public static Stroke render_expression(Expression expression,Render_Context context, Expression parent = null)
        {
            Stroke result;
            switch (expression.type)
            {
                case Expression_Type.literal:
                    var literal = (Literal)expression;
                    return render_literal(literal.value, literal.profession);

                case Expression_Type.operation:
                    return render_operation((Operation)expression);

                case Expression_Type.portal:
                    result = render_portal((Portal_Expression)expression);
                    break;

                case Expression_Type.function_call:
                    result = render_function_call((Abstract_Function_Call)expression, parent);
                    break;

                case Expression_Type.property_function_call:
                    result = render_property_function_call((Property_Function_Call)expression, parent);
                    break;

                case Expression_Type.platform_function:
                    return render_platform_function_call((Platform_Function)expression, null);

                case Expression_Type.instantiate:
                    result = render_instantiation((Instantiate)expression);
                    break;

                case Expression_Type.self:
                    result = render_this();
                    break;

                case Expression_Type.null_value:
                    return render_null();

                case Expression_Type.profession:
                    result = render_profession(expression.get_profession());
                    break;

                case Expression_Type.anonymous_function:
                    return render_anonymous_function((Anonymous_Function)expression);

                case Expression_Type.comment:
                    return render_comment((Comment)expression);

                case Expression_Type.variable:
                    var variable_expression = (Variable)expression;

                    result = new Stroke_Token(variable_expression.symbol.name, variable_expression);
                    if (variable_expression.index != null)
                    {
                        result = result + new Stroke_Token("[")
                                 + render_expression(variable_expression.index) + new Stroke_Token("]");
                    }
                    break;

                case Expression_Type.parent_class:
                    result = render_dungeon_path(current_dungeon.parent);
                    break;

                case Expression_Type.insert:
                    //                    throw new Exception("Not implemented");
                    result = new Stroke_Token(((Insert)expression).code);
                    break;

                case Expression_Type.create_dictionary:
                    return render_dictionary((Create_Dictionary)expression);

                default:
                    throw new Exception("Unsupported Expression type: " + expression.type + ".");
            }

            if (expression.next != null)
            {
                var child = render_expression(expression.next, expression);
                var text = child.full_text();
                result += !string.IsNullOrEmpty(text) && text[0] == '['
                    ? child
                    : new Stroke_Token(".") + child;
            }

            return result;
        }
        */

    }
}
