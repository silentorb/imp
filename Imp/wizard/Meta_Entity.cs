using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.schema;

namespace imperative.wizard
{
   public class Meta_Entity
    {
       public object[] values;
       public Dungeon dungeon;
       public Guid guid { get; set; }

       public Meta_Entity(Dungeon dungeon, Guid guid, Conjurer conjure)
       {
           this.guid = guid;
           this.dungeon = dungeon;
           var portals = dungeon.portals;
           values = new object[portals.Length];
           for (var i = 0; i < portals.Length; ++i)
           {
               values[++i] = conjure(portals[i].profession);
           }
       }
    }
}
