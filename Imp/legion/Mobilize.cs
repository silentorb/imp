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
            var dir = Path.GetDirectoryName(path);
            var json = File.ReadAllText(path);
            var source = JsonConvert.DeserializeObject<Project_Source>(json);

            if (source.target == null)
                throw new Exception("Missing required target property.");

            var project = new Project
            {
                name = source.name,
                target = source.target,
                path = path
            };

            if (source.inputs != null)
                project.inputs = source.inputs.Select(p => dir + "/" + p).ToList();

            if (source.output != null)
                project.output = dir + "/" + source.output;

            if (source.projects != null)
            {
                project.projects = source.projects.Select(p =>
                    load_project(dir, p)).ToList();
            }
            else
            {
                project.projects = new List<IProject>();
            }

            return project;
        }

        public static IProject load_project(string first, string second)
        {
            var path = first + "/" + second + "/imp.json";
            if (!File.Exists(path))
                return new External_Project(second);

            var result = load_project(path);
            result.relative_path = second;
            return result;
        }

        public static void build_all(Project project, Overlord overlord)
        {
            if (project.output != null)
            {
                build_project(project, overlord);
            }

            foreach (var child in project.projects.OfType<Project>())
            {
                build_all(child, overlord);
            }

            if (project.output == null)
            {
                overlord.target.build_wrapper_project(project);
            }
        }

        static void build_project(Project project, Overlord overlord)
        {
            foreach (var input in project.inputs)
            {
                var sources = Overlord.get_source_files(input);
                overlord.summon_input(sources, project);
                overlord.generate(project, sources);
            }
        }

    }
}
