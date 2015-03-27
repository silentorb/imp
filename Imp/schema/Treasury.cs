using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace imperative.schema
{
   public class Treasury :IDungeon
    {
       public List<string> jewels;
       public int start = 0;
       public Realm realm { get; set; }
       public string name { get; set; }
       public string source_file { get; set; }
       public object default_value { get; set; }

       public Treasury(string name, List<string> jewels, Realm realm)
       {
           this.name = name;
           this.jewels = jewels;
           this.realm = realm;
           default_value = 0;
       }

       public bool is_value
       {
           get { return true; }
       }

    }
}
