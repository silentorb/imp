using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using metahub.schema;

namespace imperative.schema
{
    public enum Cpp_Type
    {
        none,
        value,
        pointer,
        shared_pointer
    }

    public class Profession
    {
        public Dungeon dungeon { get; private set; }
        public List<Profession> children { get; private set; }
        public bool is_generic_parameter { get; protected set; }
        public Cpp_Type cpp_type { get; private set; }
        private Professions library;

        public string name { get { return dungeon.name; } }

        public Profession(Dungeon dungeon, Professions library, List<Profession> children = null
            , Cpp_Type cpp_type = Cpp_Type.none)
        {
            is_generic_parameter = false;
            this.dungeon = dungeon;
            this.children = children;
            this.cpp_type = cpp_type;
            this.library = library;
        }

        public Profession(IDungeon dungeon, Professions library, List<Profession> children = null
            , Cpp_Type cpp_type = Cpp_Type.none)
        {
            is_generic_parameter = false;
            this.dungeon = (Dungeon)dungeon;
            this.children = children;
            this.cpp_type = cpp_type;
            this.library = library;
        }

        public static Profession create_generic_symbol(Dungeon dungeon)
        {
            var profession = new Profession(dungeon, null);
            profession.is_generic_parameter = true;
            return profession;
        }

        public Profession change_cpp_type(Cpp_Type new_type)
        {
            if (new_type == cpp_type)
                return this;

            return library.get(dungeon, children, new_type);
        }

        public Profession with_children(List<Profession> new_children)
        {
            return library.get(dungeon, new_children, cpp_type);
        }

        public bool is_array(Overlord overlord)
        {
            return dungeon != null && dungeon == overlord.array;
        }

//        public static Profession create(IDungeon dungeon, bool is_list = false, List<Profession> children = null)
//        {
//            return new Profession(dungeon, children);
//        }

        public Profession get_deepest_child()
        {
            return children != null && children.Count > 0
                ? children[0].get_deepest_child()
                : this;
        }
    }
}
