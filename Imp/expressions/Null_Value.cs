
using System.Collections.Generic;

namespace imperative.expressions
{
    public class Null_Value : Expression
    {
        public Null_Value()

            : base(Expression_Type.null_value)
        {
        }

        public override Expression clone()
        {
            return new Null_Value();
        }

        public override IEnumerable<Expression> children
        {
            get { return new List<Expression>(); }
        }
    }
}