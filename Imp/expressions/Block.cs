using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace imperative.expressions
{
    public class Block : Expression
    {
        public List<Expression> body;

        public Block(List<Expression> children = null)
            : base(Expression_Type.statements)
        {
            initialize(children);
        }

        public Block(Expression_Type type, List<Expression> children = null)
            : base(type)
        {
            initialize(children);
        }

        private void initialize(List<Expression> children)
        {
            this.body = children ?? new List<Expression>();
            foreach (var expression in body)
            {
                expression.parent = this;
            }
        }

        public override Expression clone()
        {
            return new Block(body.Select(c => c.clone()).ToList());
        }

        public override IEnumerable<Expression> children
        {
            get { return body; }
        }
    }
}
