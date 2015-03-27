using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using metahub.schema;

namespace imperative.schema
{
    public class Profession
    {
        public Kind type;
        public bool is_list = false;
        public IDungeon dungeon;

        public Profession(Kind type, IDungeon dungeon = null)
        {
            this.type = type;
            this.dungeon = dungeon;
            is_list = type == Kind.list;
        }

        public Profession clone()
        {
            return new Profession(type, dungeon) { is_list = is_list };
        }

        public Profession get_reference()
        {
            return new Profession(Kind.reference, dungeon);
        }
    }
}
