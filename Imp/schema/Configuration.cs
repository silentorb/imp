using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace imperative.schema
{

    public class Region_Additional
    {
        public bool? is_external;

        [JsonProperty("namespace")]
        public string space;
        public string class_export;
        public Dictionary<string, Dungeon_Additional> trellises;
    }

    public class Dungeon_Additional
    {
        public string name;
        public bool? is_external;
        public string source_file;
        public string class_export;
        public Dictionary<string, string[]> inserts;
        public object default_value;
        public Dictionary<string, object> hooks;
        public List<string> stubs;
        public Dictionary<string, Tie_Addition> properties;
    }

    public class Tie_Addition
    {
        public Tie_Hooks hooks;
    }

    public class Tie_Hooks
    {
        public object set_post;
    }

}
