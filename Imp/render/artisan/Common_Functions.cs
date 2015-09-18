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

        public static Stroke render_realm(Dungeon realm, Target_Configuration config, Stroke_List_Delegate action)
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
                ? render_realm_path(realm.parent, separator) + separator + realm.name
                : realm.name);
        }

        public static Stroke render_block(List<Stroke> strokes, bool try_minimal = true, bool is_succeeded = false)
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

        public static List<Stroke> render_statements(IEnumerable<Expression> statements, Statement_Router router)
        {
            return statements.Select(s => render_statement(s, router)).ToList();
        }

        public static Stroke render_statement(Expression statement, Statement_Router router)
        {
            Expression_Type type = statement.type;
            switch (type)
            {
                case Expression_Type.function_definition:
                    return router.render_function_definition(((Function_Definition)statement).minion);

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
                        statement_result += new Stroke_Token(" ") + render_expression(state.next);

                    statement_result.expression = statement;
                    return statement_result + terminate_statement();

                case Expression_Type.insert:
                    return new Stroke_Token(((Insert)statement).code);

                default:
                    return render_expression(statement) + terminate_statement();
            }
        }

    }
}
