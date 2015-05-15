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
        public bool is_list = false;
        public IDungeon dungeon;
        public List<Profession> children;

        Profession(IDungeon dungeon, bool is_list = false, List<Profession> children = null)
        {
            this.dungeon = dungeon;
            this.is_list = true;
            this.children = children;
        }

//        public Profession clone()
//        {
//            return new Profession(dungeon, is_list, children);
//        }

        public bool is_array(Overlord overlord)
        {
            return dungeon != null && dungeon == overlord.array;
        }

        public static Profession create(IDungeon dungeon, bool is_list = false, List<Profession> children = null)
        {
            return new Profession(dungeon,is_list, children);
        }

        public static Profession get(Professions library, IDungeon dungeon, bool is_list = false, List<Profession> children = null)
        {
            var fullname = dungeon.fullname;
            if (!library.professions.ContainsKey(fullname))
            {
                var result = new Profession(dungeon, is_list, children);
                library.professions[fullname] = new List<Profession> { result };
                return result;
            }

            var group = library.professions[fullname];
            foreach (var item in group)
            {
                if (item.is_list == is_list && (item.children != null) == (children != null))
                {
                    if (children == null || compare_children(children, item.children))
                        return item;
                }
            }

            {
                var result = new Profession(dungeon, is_list, children);
                group.Add(result);
                return result;
            }
        }

        private static bool compare_children(List<Profession> a, List<Profession> b)
        {
            if (a.Count != b.Count)
                return false;

            for (var i = 0; i < a.Count; ++i)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }
    }
}
