using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace imperative.legion
{
    static class Mobilize
    {
        public static Project load_project(string path)
        {
            var json = File.ReadAllText(path);
            var source = JsonConvert.DeserializeObject<Project_Source>(json);
            var project = new Project()
            {
                name = source.name,
                target = source.target,
                inputs = source.input
            };

            if (source.projects != null)
            {
                project.projects = source.projects.Select(load_project).ToList();
            }

            return project;
        }
    }
}
