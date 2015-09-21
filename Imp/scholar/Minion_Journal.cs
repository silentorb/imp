using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.schema;

namespace imperative.scholar
{
    public class Minion_Journal
    {
        public Minion minion;

       public static IEnumerable<Expression> get_all_expressions(Expression expression)
        {
            return expression.children.SelectMany(get_all_expressions);
        } 
    }
}
