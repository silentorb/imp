using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace imperative.schema
{
    public static class Enchantments
    {
        public const string Static = "static";
    }

    public class Enchantment
    {
        public string name;

        public Enchantment(string name)
        {
            this.name = name;
        }
    }
}
