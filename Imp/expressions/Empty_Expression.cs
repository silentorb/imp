using System.Collections.Generic;

namespace imperative.expressions
{
    public class Empty_Expression : Expression
    {
        public Empty_Expression()
            : base(Expression_Type.empty)
        {
        }

        public override Expression clone()
        {
            return new Empty_Expression();
        }

        public override IEnumerable<Expression> children
        {
            get { return new List<Expression>(); }
        }
    }
}
