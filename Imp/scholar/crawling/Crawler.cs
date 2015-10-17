using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.schema;
using imperative.scholar.crawling;

namespace imperative.scholar
{
    public delegate void Expression_Delegate(Expression expression);

    public static class Crawler
    {
        public static void analyze_expression(Expression expression, Expression_Delegate action)
        {
            action(expression);

            switch (expression.type)
            {
                case Expression_Type.function_definition:
                    analyze_expressions(((Function_Definition)expression).expressions, action);
                    break;

                case Expression_Type.operation:
                    analyze_expressions(((Operation)expression).children, action);
                    break;

                case Expression_Type.flow_control:
                    analyze_expression(((Flow_Control)expression).condition, action);
                    analyze_expressions(((Flow_Control)expression).body, action);
                    break;

                case Expression_Type.function_call:
                    {
                        var definition = (Abstract_Function_Call)expression;
                        analyze_expressions(definition.args, action);
                    }
                    break;

                case Expression_Type.property_function_call:
                    var property_function = (Property_Function_Call)expression;
                    if (property_function.reference != null)
                        analyze_expression(property_function.reference, action);

                    analyze_expressions(property_function.args, action);
                    break;

                case Expression_Type.assignment:
                    {
                        var assignment = (Assignment)expression;
                        analyze_expression(assignment.target, action);
                        analyze_expression(assignment.expression, action);
                    }
                    break;

                case Expression_Type.declare_variable:
                    var declare_variable = (Declare_Variable)expression;
                    if (declare_variable.expression != null)
                        analyze_expression(declare_variable.expression, action);

                    break;

                case Expression_Type.iterator:
                    var iterator = (Iterator)expression;
                    analyze_expression(iterator.expression, action);
                    analyze_expressions(iterator.body, action);
                    break;

                case Expression_Type.instantiate:
                    var instantiation = (Instantiate)expression;
                    analyze_expressions(instantiation.args, action);
                    break;
            }

            if (expression.next != null)
                analyze_expression(expression.next, action);
        }

        public static void analyze_expressions(IEnumerable<Expression> expressions, Expression_Delegate action)
        {
            foreach (var expression in expressions)
            {
                analyze_expression(expression, action);
            }
        }
    }
}
