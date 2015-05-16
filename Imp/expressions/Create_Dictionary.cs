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

        public Create_Dictionary(Professions library)
            : base(Expression_Type.create_dictionary)
        {
            items = new Dictionary<string, Expression>();
            _profession = library.get(Professions.Dictionary, Professions.unknown);
        }

        public Create_Dictionary(Professions library, Dictionary<string, Expression> children)
            : base(Expression_Type.create_dictionary)
        {
            items = children;
            _profession = library.get(Professions.Dictionary, Professions.unknown);
        }

        public override IEnumerable<Expression> children
        {
            get { return items.Values; }
        }

        public override Profession get_profession()
        {
//            if (_profession == null)
//                _profession = Professions.get(Professions.any.dungeon, false, new List<Profession>()
//                {
//                    new Profession(Kind.String),
//                    new Profession(Kind.unknown)
//                });

            return _profession;
        }
    }
}