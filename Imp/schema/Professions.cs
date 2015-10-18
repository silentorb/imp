using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;

namespace imperative.schema
{
    public class Professions
    {
        static bool initialized = false;
        public static Profession Int;
        public static Profession Float;
        public static Profession String;
        public static Profession Bool;
        public static Profession none;
        public static Profession any;
        public static Profession unknown;
        public static Profession Function;
        public static Dungeon List;
        public static Dungeon Dictionary;
        public static Dungeon exception;

        public Dictionary<string, List<Profession>> professions = new Dictionary<string, List<Profession>>();

        public Professions()
        {
            initialize();

            professions["int"] = new List<Profession> { Int };
            professions["float"] = new List<Profession> { Float };
            professions["string"] = new List<Profession> { String };
            professions["bool"] = new List<Profession> { Bool };
            professions["none"] = new List<Profession> { none };
            professions["any"] = new List<Profession> { any };
            professions["unknown"] = new List<Profession> { unknown };
            professions["function"] = new List<Profession> { Function };
        }

        public static void initialize()
        {
            if (initialized)
                return;

            initialized = true;
            Int = create_type("int");
            Float = create_type("float");
            String = create_type("string");
            Bool = create_type("bool");
            none = create_type("none");
            any = create_type("any", false);
            unknown = create_type("unknown", false);
            Function = create_type("function", false);

            initialize_list();
            initialize_exception();

            Dictionary = new Dungeon("Dictionary", null, null) { is_standard = true };

        }

        private static void initialize_list()
        {
            List = new Dungeon("List", null, null) { is_standard = true };
            List.spawn_simple_minion("get", new List<Parameter>
            {
                new Parameter(new Symbol("index", Int, null))
            });
            List.spawn_simple_minion("push", new List<Parameter>
            {
                new Parameter(new Symbol("item", any, null))
            });
            List.add_portal(new Portal("count", Professions.Int, List));
            List.spawn_simple_minion("last", null, null, Professions.any);
            List.spawn_simple_minion("pop", null, null, Professions.any);
        }

        private static void initialize_exception()
        {
            exception = new Dungeon("Exception", null, null) { is_standard = true };
        }

        static Profession create_type(string name, bool is_value = true)
        {
            var dungeon = new Dungeon(name, null, null)
            {
                is_value = is_value,
                is_standard = true
            };
            return new Profession(dungeon, null);
        }

        public Profession get(IDungeon dungeon, IDungeon one)
        {
            return get(dungeon, new List<Profession> { get(one) });
        }

        public Profession get(IDungeon dungeon, Profession one)
        {
            return get(dungeon, new List<Profession> { one });
        }

        public Profession get(IDungeon dungeon, List<Profession> children = null,
            Cpp_Type cpp_type = Cpp_Type.none)
        {
            var fullname = dungeon.fullname;
            if (!professions.ContainsKey(fullname))
            {
                var result = new Profession(dungeon, this, children, cpp_type);
                professions[fullname] = new List<Profession> { result };
                return result;
            }

            var group = professions[fullname];
            foreach (var item in group)
            {
                if (item.cpp_type != cpp_type)
                    continue;

                if ((item.children != null) == (children != null))
                {
                    if (children == null || compare_children(children, item.children))
                        return item;
                }
            }

            {
                var result = new Profession(dungeon,this, children, cpp_type);
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

        public static bool is_scalar(Profession profession)
        {
            return profession == Bool
                || profession == Float
                || profession == Function
                || profession == Int
                || profession == String
                || profession == any
                || profession == none
                || profession == unknown
            ;
        }
    }
}
