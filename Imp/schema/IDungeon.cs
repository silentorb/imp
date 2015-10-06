using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace imperative.schema
{
    public interface IDungeon
    {
        string name { get; set; }
        Dungeon realm { get; set; }
        bool is_value { get; }
        object default_value { get; set; }
        string fullname { get; }
    }
}
