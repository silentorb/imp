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

    public interface IProject
    {
        string name { get; set; }
    }

    public class External_Project : IProject
    {
        public string name { get; set; }

        public External_Project(string name)
        {
            this.name = name;
        }
    }

    public class Project : Build_Orders, IProject
    {
        public string name { get; set; }
        public List<IProject> projects;
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
