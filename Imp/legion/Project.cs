using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.schema;

namespace imperative.legion
{
    class Project_Source
    {
        public string name;
        public List<string> projects;
        public string output;
        public string target;
        public string[] inputs;
    }

    public class Project : Build_Orders
    {
        public string name { get; set; }
        public List<Project> projects;
        public List<string> inputs { get; set; }
        public string output { get; set; }
        public string target { get; set; }
        public string path { get; set; }
        public Dictionary<string, Dungeon> dungeons { get; set; }

        public Project()
        {
            dungeons = new Dictionary<string, Dungeon>();
        }
    }
}
