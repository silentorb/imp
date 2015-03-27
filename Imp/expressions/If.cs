using System.Collections;
using System.Collections.Generic;

namespace imperative.expressions
{
    public class If : Expression
    {
        public List<Flow_Control> if_statements;
        public List<Expression> else_block;

        public If(List<Flow_Control> if_statements, List<Expression> else_block = null)
            : base(Expression_Type.if_statement)
        {
            this.if_statements = if_statements;
            this.else_block = else_block;
        }

        public override IEnumerable<Expression> children
        {
            get
            {
                var result = new List<Expression>();
                foreach (var if_statement in if_statements)
                {
                    result.AddRange(if_statement.children);
                }

                if (else_block != null)
                    result.AddRange(else_block);

                return result;
            }
        }

    }
}