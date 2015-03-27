using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace imperative.schema
{
    public interface IDungeon
    {
        string name { get; set; }
        string source_file { get; set; }
        Realm realm { get; set; }
        bool is_value { get; }
        object default_value { get; set; }
    }
}
