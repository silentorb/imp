using System.Collections.Generic;
using System.Linq;

namespace imperative.expressions
{
    public class Create_Array : Expression
    {
        public List<Expression> items;

        public Create_Array()
            : base(Expression_Type.create_array)
        {
            items = new List<Expression>();
        }

        public Create_Array(IEnumerable<Expression> children)
            : base(Expression_Type.create_array)
        {
            items = children.ToList();
        }

        public override IEnumerable<Expression> children
        {
            get { return items; }
        }
    }
}