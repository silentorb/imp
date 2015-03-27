using System.Collections.Generic;
using System.Linq;
using imperative.schema;
using metahub.schema;

namespace imperative.expressions
{
    public class Anonymous_Function : Block
    {
        public Ethereal_Minion minion;

        public Anonymous_Function(Ethereal_Minion minion)
            : base(Expression_Type.anonymous_function)
        {
            this.minion = minion;
        }

        public List<Parameter> parameters { get { return minion.parameters; } }
        public List<Expression> expressions { get { return minion.expressions; } }

        public override Profession get_profession()
        {
            return new Profession(Kind.function);
        }

        public override IEnumerable<Expression> children
        {
            get { return expressions; }
        }
    }
}