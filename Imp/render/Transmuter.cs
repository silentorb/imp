using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;

namespace metahub.render
{
    public abstract class Transmuter
    {
        public abstract void transform(Expression expression);

        private static Expression_Type[] expression_ancestor_types = new []
            {
                Expression_Type.property,
                Expression_Type.variable,
                Expression_Type.property_function_call,
                Expression_Type.function_call
            };

        public static Expression get_root_expression(Expression expression)
        {
            if (expression.parent == null)
                return expression;

            if (expression_ancestor_types.Contains(expression.parent.type))
                return get_root_expression(expression.parent);

            return expression;
        }

        public static Expression clone_expression_ancestors_without_self(Expression expression)
        {
            var original = expression;
            Expression result = null;
            while (expression.parent != null && expression_ancestor_types.Contains(expression.parent.type))
            {
                expression = expression.parent;
            }

            return result;
        }
    }
}
