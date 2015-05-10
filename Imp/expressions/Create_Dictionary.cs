using System.Collections.Generic;
using System.Linq;

namespace imperative.expressions
{
    public class Create_Dictionary : Expression
    {
        public Dictionary<string, Expression> items;

        public Create_Dictionary()
            : base(Expression_Type.create_dictionary)
        {
            items = new Dictionary<string, Expression>();
        }

        public Create_Dictionary(Dictionary<string, Expression> children)
            : base(Expression_Type.create_array)
        {
            items = children;
        }

        public override IEnumerable<Expression> children
        {
            get { return items.Values; }
        }
    }
}