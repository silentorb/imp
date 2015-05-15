using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using metahub.schema;

namespace imperative.schema
{
    public class Platform_Function_Info
    {
        public static Dictionary<string, Platform_Function_Info> functions;
        public string name;
        public Profession return_type;

        public Platform_Function_Info(string name, Profession return_type)
        {
            this.name = name;
            this.return_type = return_type;
        }

        public static void initialize()
        {
            functions = new Dictionary<string, Platform_Function_Info>();

            add(new Platform_Function_Info("count", Professions.Int));
            add(new Platform_Function_Info("add", Professions.none));
            add(new Platform_Function_Info("contains", Professions.Bool));
            add(new Platform_Function_Info("distance", Professions.Float));
            add(new Platform_Function_Info("first", Professions.any));
            add(new Platform_Function_Info("last", Professions.any));
            add(new Platform_Function_Info("pop", Professions.any));
            add(new Platform_Function_Info("remove", Professions.none));
            add(new Platform_Function_Info("rand", Professions.Float));
        }

        static void add(Platform_Function_Info info)
        {
            functions[info.name] = info;
        }
    }
}
