using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace imperative.schema
{
    public class Realm
    {
        public string name;
        public string external_name;
        public Dictionary<string, Dungeon> dungeons = new Dictionary<string, Dungeon>();
        public Dictionary<string, Treasury> treasuries = new Dictionary<string, Treasury>();
        public Overlord overlord;
        public Dictionary<string, Dungeon_Additional> trellis_additional = new Dictionary<string, Dungeon_Additional>();
        public bool is_external;
        public string class_export = "";

        public Realm(string name, Overlord overlord)
        {
            this.name = name;
            this.overlord = overlord;
        }

        public Dungeon create_dungeon(string name)
        {
            var dungeon = new Dungeon(name, overlord, this);
            return dungeon;
        }

        public Treasury create_treasury(string treasury_name, List<string> jewels)
        {
            if (get_child(treasury_name) != null)
                throw new Exception("Realm " + name + " already contains a type named " + treasury_name + ".");

            var treasury = new Treasury(treasury_name, jewels, this);
            treasuries[treasury_name] = treasury;
            
            return treasury;
        }

        public IDungeon get_child(string child_name)
        {
            if (dungeons.ContainsKey(child_name))
                return dungeons[child_name];

            if (treasuries.ContainsKey(child_name))
                return treasuries[child_name];

            return null;
//            throw new Exception("Realm " + name + " does not have symbol: " + child_name + ".");
        }

        public void load_additional(Region_Additional additional)
        {
            if (additional.is_external.HasValue)
                is_external = additional.is_external.Value;

            if (additional.space != null)
                external_name = additional.space;

            if (additional.class_export != null)
                class_export = additional.class_export;

            if (additional.trellises != null)
            {
                foreach (var item in additional.trellises)
                {
                    trellis_additional[item.Key] = item.Value;
                }
            }
        }
    }
}
