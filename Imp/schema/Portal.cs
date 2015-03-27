using System;
using System.Collections.Generic;
using System.Linq;
using imperative.expressions;

using metahub.schema;

namespace imperative.schema
{
    public class Portal
    {
        public Dungeon dungeon;
        public string name;
        public bool is_value = false;
        public Portal other_portal;
        public Portal parent;
        public Profession profession;
        public Minion setter;
        private int id; 
        private static int next_id = 1;
        public object default_value;
        public List<Portal_Expression> expressions = new List<Portal_Expression>(); 

        public Kind type
        {
            get { return profession.type; }
            set { profession.type = value; }
        }

        public IDungeon other_dungeon
        {
            get { return profession.dungeon; }
            set { profession.dungeon = value; }
        }

        public bool is_list
        {
            get { return profession.is_list; }
        }

        public string fullname
        {
            get { return dungeon.name + "." + name; }
        }

        public Portal(string name, Kind type, Dungeon dungeon, Dungeon other_dungeon = null)
        {
            id = next_id++;
            if (type == Kind.reference && other_dungeon == null)
                throw new Exception("Invalid portal.");

            this.name = name;
            this.dungeon = dungeon;
            profession = new Profession(type, other_dungeon);
        }

        public Portal(string name, Profession profession, Dungeon dungeon = null)
        {
            this.name = name;
            this.profession = profession;
            this.dungeon = dungeon;
        }

        public Portal(Portal original, Dungeon new_dungeon)
        {
            dungeon = new_dungeon;
            profession = new Profession(original.type, original.other_dungeon);
            name = original.name;
            is_value = original.is_value;
            other_portal = original.other_portal;
            parent = original;
        }

        public Portal_Expression create_reference(Expression child = null)
        {
            return new Portal_Expression(this, child);
        }

        public Profession get_profession()
        {
            return profession;
        }

        public Profession get_target_profession()
        {
            if (profession.dungeon == null)
                return profession;

            return new Profession(Kind.reference, other_dungeon);
        }

        public object get_default_value()
        {
            if (other_dungeon != null && other_dungeon.default_value != null)
                return other_dungeon.default_value;
            
            if (default_value != null)
                return default_value;

            switch (type)
            {
                case Kind.Int:
                    return 0;

                case Kind.Float:
                    return 0;

                case Kind.String:
                    return "";

                case Kind.Bool:
                    return false;

                default:
                    return null;
            }
        }
    }
}