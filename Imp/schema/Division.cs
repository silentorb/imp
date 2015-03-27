using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;

namespace imperative.schema
{
    public class Division
    {
        public List<Expression> expressions;


        public void add(Expression expression)
        {
            expressions.Add(expression);
        }

        public void add_many(IEnumerable<Expression> expressions)
        {
            this.expressions.AddRange(expressions);
        }

    }
}
