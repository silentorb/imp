using System.Collections.Generic;
using System.Linq;
using imperative.schema;


namespace imperative.expressions
{
    public class Method_Call : Abstract_Function_Call
    {
        public string name;
        public Minion minion;
        public Profession profession;
        public override string get_name() { return name; }

        //public Class_Function_Call(string name, Expression reference = null, IEnumerable<Expression> args = null)
        //    : base(Expression_Type.function_call, reference, args)
        //{
        //    this.name = name;
        //}

        public Method_Call(Expression reference = null, IEnumerable<Expression> args = null)
            : base(Expression_Type.function_call, reference, args)
        {
        }

        public Method_Call(Minion minion, Expression reference = null, IEnumerable<Expression> args = null)
            : base(Expression_Type.function_call, reference, args)
        {
            this.minion = minion;
            name = minion.name;
        }

        public Method_Call(Minion minion, Expression reference, Expression arg)
            : base(Expression_Type.function_call, reference, new[] { arg })
        {
            this.minion = minion;
            name = minion.name;
        }

        public Method_Call set_reference(Expression reference)
        {
            this.reference = reference;
            return this;
        }

        public override Profession get_profession()
        {
            return minion != null
                ? minion.return_type
                : profession;
        }

        public override Expression clone()
        {
            return new Method_Call(minion, reference, args);
        }
    }

}