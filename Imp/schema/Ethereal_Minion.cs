using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;

namespace imperative.schema
{
    public class Ethereal_Minion : Minion_Base
    {
        public Ethereal_Minion(IEnumerable<Parameter> parameters, Scope parent_scope)
        {
            this.parameters = parameters.ToList();
            scope = new Scope(parent_scope);
        }
    }
}
