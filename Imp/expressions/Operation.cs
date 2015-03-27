using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace imperative.expressions
{
    public class Operation : Expression
    {
        public string op;
        public List<Expression> expressions = new List<Expression>();

        public Operation(string op, IEnumerable<Expression> expressions)
            : base(Expression_Type.operation)
        {
            this.op = op;

#if DEBUG
            if (expressions.Any(e => e == null))
                throw new Exception("Argument cannot be null");
#endif

            this.expressions.AddRange(expressions);
            foreach (var expression in this.expressions)
            {
                expression.parent = this;
            }
        }

        public Operation(string op, Expression first, Expression second)
            : base(Expression_Type.operation)
        {
            this.op = op;
            expressions.Add(first);
            expressions.Add(second);
            first.parent = this;
            second.parent = this;
        }

        public bool is_condition()
        {
            return op == "=="
                 || op == "!="
                 || op == ">="
                 || op == "<="
                 || op == ">"
                 || op == "<"
             ;
        }

        public override Expression clone()
        {
            return new Operation(op, children.Select(e => e.clone()));
        }

        public override bool is_empty()
        {
            return expressions.Count > 0;
        }

        public override IEnumerable<Expression> children
        {
            get { return expressions; }
        }
    }
}
