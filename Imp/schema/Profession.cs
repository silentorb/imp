using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using metahub.schema;

namespace imperative.schema
{
    public class Profession
    {
//        public bool is_list = false;
        public IDungeon dungeon;
        public List<Profession> children;

        internal Profession(IDungeon dungeon, List<Profession> children = null)
        {
            this.dungeon = dungeon;
            this.children = children;
        }

        public bool is_array(Overlord overlord)
        {
            return dungeon != null && dungeon == overlord.array;
        }

        public static Profession create(IDungeon dungeon, bool is_list = false, List<Profession> children = null)
        {
            return new Profession(dungeon, children);
        }
    }
}
