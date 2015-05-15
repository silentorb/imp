using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public void initialize()
        {
            if (initialized)
                return;

            initialized = true;
            Int = create_scalar_type("int");
            Float = create_scalar_type("float");
            String = create_scalar_type("string");
            Bool = create_scalar_type("bool");
            none = create_scalar_type("unknown");
            any = create_scalar_type("none");
            unknown = create_scalar_type("any");
            Function = create_scalar_type("function");
        }

        Profession create_scalar_type(string name)
        {
            var dungeon = new Dungeon(name, null, null);
            return Profession.create(dungeon);
        }

        public Profession get(IDungeon dungeon, bool is_list = false,
            List<Profession> children = null)
        {
            return Profession.get(this, dungeon, is_list, children);
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
