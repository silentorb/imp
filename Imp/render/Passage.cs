using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;

namespace imperative.render
{
   public class Passage
   {
       public Expression expression;
       public string text;
       public int x;
       public int y;

       public Passage(Expression expression, int x, int y)
       {
           this.expression = expression;
           this.x = x;
           this.y = y;
       }
   }
}
