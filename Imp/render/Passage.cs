using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.render.artisan;

namespace imperative.render
{
   public class Passage
   {
       public Expression expression;
       public string text;

       public Passage(Stroke stroke)
       {
           this.expression = stroke.expression;
           text = stroke.full_text();
       }

       public Passage(string text, Expression expression = null)
       {
           this.text = text;
           this.expression = expression;
       }
   }
}
