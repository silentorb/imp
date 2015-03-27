using System.Collections.Generic;
using System.Linq;
using imperative.schema;


namespace imperative.expressions
{
    public abstract class Abstract_Function_Call : Expression
    {
        public List<Expression> args;
        public Expression reference;

        public abstract string get_name();

        protected Abstract_Function_Call(Expression_Type type, Expression reference = null, IEnumerable<Expression> args = null)
            : base(type)
        {
            this.args = args != null ? args.ToList() : new List<Expression>();
            this.reference = reference;
        }

        public override IEnumerable<Expression> children
        {
            get
            {
                return reference != null
                    ? new[] { reference }.Concat(args)
                    : args;
            }
        }

    }

}