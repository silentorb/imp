using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using metahub.schema;

namespace imperative.schema
{
    public delegate void Minion_Expression_Event(Minion_Base minion, Expression expression);
    
    public abstract class Minion_Base
    {
        public List<Parameter> parameters;
        public List<Expression> expressions = new List<Expression>();
        public Scope scope;
//        public event Minion_Expression_Event on_add_expression;
        public Profession return_type = new Profession(Kind.none);

    }
}
