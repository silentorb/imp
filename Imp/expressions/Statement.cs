

using System.Collections.Generic;

namespace imperative.expressions
{
    public sealed class Statement : Expression
    {
        public string name;

        public Statement(string name, Expression child = null)
            : base(Expression_Type.statement)
        {
            this.name = name;
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