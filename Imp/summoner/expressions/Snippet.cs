using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using runic.parser;

namespace metahub.jackolantern.expressions
{
   public class Snippet : Expression
   {
       public string name;
       public Legend source;
       public string[] parameters;

       public Snippet(string name, Legend source, string[] parameters)
           : base(Expression_Type.snippet)
       {
           this.name = name;
           this.source = source;
           this.parameters = parameters;
       }

       public override IEnumerable<Expression> children
       {
           get { return new List<Expression>(); }
       }
    }
}
