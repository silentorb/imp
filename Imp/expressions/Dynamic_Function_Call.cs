using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace imperative.expressions
{
    public class Dynamic_Function_Call :Abstract_Function_Call
    {
        public string name;
        public override string get_name() { return name; }

        public Dynamic_Function_Call(string name, Expression reference = null, IEnumerable<Expression> args = null)
            : base(Expression_Type.function_call, reference, args)
        {
            this.name = name;
        }
    }
}
