using System.Collections;
using System.Collections.Generic;
using System.Linq;
using imperative.schema;

namespace imperative.expressions
{
    public class Profession_Expression : Expression
    {
        public Profession profession;

        public Profession_Expression(Profession profession)
            : base(Expression_Type.profession)
        {
            this.profession = profession;
        }

        public override Profession get_profession()
        {
            return profession;
        }

        public override IEnumerable<Expression> children
        {
            get { return new List<Expression>(); }
        }
    }

}