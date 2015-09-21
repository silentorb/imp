using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.schema;

namespace imperative.render.artisan
{
   public class Render_Context
   {
       public Dungeon realm;
       public Target_Configuration config;
       public Statement_Router router;
       public Common_Target2 target;

       public Render_Context(Dungeon realm, Target_Configuration config, Statement_Router router, Common_Target2 target)
       {
           this.realm = realm;
           this.config = config;
           this.router = router;
           this.target = target;
       }
   }
}
