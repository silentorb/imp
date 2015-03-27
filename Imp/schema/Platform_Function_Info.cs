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

            add(new Platform_Function_Info("count", new Profession(Kind.Int)));
            add(new Platform_Function_Info("add", new Profession(Kind.none)));
            add(new Platform_Function_Info("contains", new Profession(Kind.Bool)));
            add(new Platform_Function_Info("distance", new Profession(Kind.Float)));
            add(new Platform_Function_Info("first", new Profession(Kind.reference)));
            add(new Platform_Function_Info("last", new Profession(Kind.reference)));
            add(new Platform_Function_Info("pop", new Profession(Kind.reference)));
            add(new Platform_Function_Info("remove", new Profession(Kind.none)));
            add(new Platform_Function_Info("rand", new Profession(Kind.Float)));
        }

        static void add(Platform_Function_Info info)
        {
            functions[info.name] = info;
        }
    }
}
