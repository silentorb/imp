using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace imperative.schema
{/*
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
        public Realm parent;
        public Dictionary<string, Realm> children = new Dictionary<string, Realm>();
        public bool is_virtual = false;
        
        public Realm(string name, Overlord overlord)
        {
            this.name = name;
            this.overlord = overlord;
        }

        public Realm add_child(Realm realm)
        {
            children[realm.name] = realm;
            realm.parent = this;
            return realm;
        }

        public Dungeon create_dungeon(string name)
        {
            var dungeon = new Dungeon(name, overlord, this);
            return dungeon;
        }

        public Treasury create_treasury(string treasury_name, List<string> jewels)
        {
            if (get_dungeon(treasury_name) != null)
                throw new Exception("Realm " + name + " already contains a type named " + treasury_name + ".");

            var treasury = new Treasury(treasury_name, jewels, this);
            treasuries[treasury_name] = treasury;

            return treasury;
        }

        public IDungeon get_dungeon_from_path(string path)
        {
            return get_dungeon(path.Split('.'));
        }

        public IDungeon get_dungeon(string child_name)
        {
            if (dungeons.ContainsKey(child_name))
                return dungeons[child_name];

            if (treasuries.ContainsKey(child_name))
                return treasuries[child_name];

            return null;
        }

        public IDungeon get_dungeon(IEnumerable<string> original_path, bool throw_error = true)
        {
            var realm = this;
            var path = original_path.ToArray();
            var tokens = path.Take(path.Length - 1).ToArray();
            foreach (var token in tokens)
            {
                realm = realm.get_child_realm(token, throw_error);
            }

            if (realm == null && !throw_error)
                return null;

            return realm.get_dungeon(path.Last());
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

        public Realm get_child_realm(string token, bool throw_error = true)
        {
            if (!children.ContainsKey(token))
            {
                if (name == "")
                {
                    if (token == "imp")
                        return overlord.load_standard_library();

                    if (!throw_error)
                        return null;

                    throw new Exception("Invalid namespace: " + token + ".");
                }
                else
                {
                    if (!throw_error)
                        return null;

                    throw new Exception("Namespace " + name + " does not have a child named " + token + ".");
                }
            }

            return children[token];
        }

        public Realm get_or_create_realm(IEnumerable<string> original_path)
        {
            var realm = this;
            var path = original_path.ToArray();
            foreach (var token in path)
            {
                if (!realm.children.ContainsKey(token))
                {
                    realm.add_child(new Realm(token, overlord));
                }

                realm = realm.children[token];
            }

            return realm;
        }

        public Realm get_realm(IEnumerable<string> original_path)
        {
            var realm = this;
            var path = original_path.ToArray();
            foreach (var token in path)
            {
                realm = realm.get_child_realm(token);
            }

            return realm;
        }
    }*/
}
