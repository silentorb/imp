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
       public Stroke stroke;
       public string text;
       public int x;
       public int y;

       public Passage(Stroke stroke, int x, int y)
       {
           this.stroke = stroke;
           this.x = x;
           this.y = y;
       }

       public Passage(Stroke stroke)
       {
           this.stroke = stroke;
           text = stroke.text;
       }

       public Passage(string text)
       {
           this.text = text;
       }
   }
}
