using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.schema;

namespace imperative.expressions
{
    public class Iterator : Block
    {
        public Symbol parameter;
        public Expression expression;
        public List<Expression> body;

        public Iterator(Symbol parameter, Expression expression, List<Expression> body)
            : base(Expression_Type.iterator)
        {
            this.parameter = parameter;
            this.expression = expression;
            this.body = body;
        }

        public override IEnumerable<Expression> children
        {
            get
            {
                return new[] { expression }.Concat(body);
            }
        }
    }
}
