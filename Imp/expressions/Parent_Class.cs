
using System.Collections.Generic;

namespace imperative.expressions
{
    public sealed class Parent_Class : Expression
    {
        public Parent_Class(Expression child = null)
            : base(Expression_Type.parent_class)
        {
            next = child;
        }

        public override IEnumerable<Expression> children
        {
            get
            {
                return next != null
                           ? new List<Expression> { next }
                           : new List<Expression>();
            }
        }
    }
}