using System.Collections.Generic;
using System.Linq;
using imperative.schema;
using metahub.schema;

namespace imperative.expressions
{
    public class Create_Dictionary : Expression
    {
        public Dictionary<string, Expression> items;
        private Profession _profession;

        public Create_Dictionary()
            : base(Expression_Type.create_dictionary)
        {
            items = new Dictionary<string, Expression>();
        }

        public Create_Dictionary(Dictionary<string, Expression> children)
            : base(Expression_Type.create_dictionary)
        {
            items = children;
        }

        public override IEnumerable<Expression> children
        {
            get { return items.Values; }
        }

        public override Profession get_profession()
        {
            if (_profession == null)
                _profession = new Profession(Kind.reference, null) { children = new List<Profession>()
                {
                    new Profession(Kind.String),
                    new Profession(Kind.unknown)
                }};

            return _profession;
        }
    }
}