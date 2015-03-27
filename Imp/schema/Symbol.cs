using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace imperative.schema
{
    public class Symbol
    {
        public string name;
        public Profession profession;
        public Scope scope;

        public Symbol(string name, Profession profession, Scope scope)
        {
            this.name = name;
            this.profession = profession;
            this.scope = scope;
        }

        public Profession get_profession(Overlord overlord)
        {
            return profession;
        }
    }
}
