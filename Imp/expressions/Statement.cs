

using System.Collections.Generic;
using runic.parser;

namespace imperative.expressions
{
    public sealed class Statement : Expression
    {
        public string name;

        public Statement(string name, Expression child = null, Legend legend = null)
            : base(Expression_Type.statement)
        {
            this.name = name;
            next = child;
            this.legend = legend;
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