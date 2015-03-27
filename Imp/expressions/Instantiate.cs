using System.Collections;
using System.Collections.Generic;
using System.Linq;
using imperative.schema;


using metahub.schema;

namespace imperative.expressions
{
    public class Instantiate : Expression
    {
        public Profession profession;
        public List<Expression> args;

        public Instantiate(Profession profession, IEnumerable<Expression> args = null)
            : base(Expression_Type.instantiate)
        {
            this.profession = profession;
            this.args = args != null ? args.ToList() : new List<Expression>();
        }

        public Instantiate(Dungeon dungeon, IEnumerable<Expression> args = null)
            : base(Expression_Type.instantiate)
        {
            this.profession = new Profession(Kind.reference, dungeon);
            this.args = args != null ? args.ToList() : new List<Expression>();
        }

        public override Profession get_profession()
        {
            return profession;
        }

        public override IEnumerable<Expression> children
        {
            get { return args; }
        }
    }

}