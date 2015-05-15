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
        public Portal parent;
        public Profession profession;
        public Minion setter;
        private int id; 
        private static int next_id = 1;
        public Expression default_expression;
        public List<Portal_Expression> expressions = new List<Portal_Expression>();
        public List<Enchantment> enchantments = new List<Enchantment>();

        private Portal _other_portal;

        public Portal other_portal
        {
            get
            {
                if (_other_portal != null)
                    return _other_portal;

                if (other_dungeon == null || other_dungeon.is_value)
                    return null;

                _other_portal = ((Dungeon) other_dungeon).all_portals.Values.FirstOrDefault(p => p.other_dungeon == dungeon);
                return _other_portal;
            }

            set { _other_portal = value; }
        }

//        public Kind type
//        {
//            get { return profession.type; }
//            set { profession.type = value; }
//        }

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
            profession = dungeon.overlord.library.get(other_dungeon);
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
            profession = original.dungeon.overlord.library.get(original.other_dungeon);
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

//            if (other_portal != null)
//                return other_portal.profession;

            return dungeon.overlord.library.get(other_dungeon);
//            var result = profession.clone();
//            result.is_list = false;
//            return result;
//            return other_portal.profession;
        }

        public object get_default_value()
        {
            if (other_dungeon != null && other_dungeon.default_value != null)
                return other_dungeon.default_value;

            if (profession == Professions.Int)
                return 0;

            if (profession == Professions.Float)
                return 0;

            if (profession == Professions.String)
                return "";

            if (profession == Professions.Bool)
                return false;

            return null;
        }

        public bool has_enchantment(string name)
        {
            return enchantments.Any(e => e.name == name);
        }

        public void enchant(Enchantment enchantment)
        {
            enchantments.Add(enchantment);
        }
    }
}