using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using imperative.legion;
using imperative.schema;
using metahub.render;

namespace imperative.render.artisan.targets.cpp
{
    static class CMake
    {
        public static void create_files(Project project, Overlord overlord)
        {
            var dependencies = get_project_dependencies(project);

            create_cmakelists_txt(project, dependencies);
            create_config(project, dependencies);
        }

        static void create_config(Project project, List<Project> dependencies)
        {
            var dir = project.output + "/";
            var sources = new List<String>();
            gather_source_paths(project.dungeons.Values, sources);

            var all_dependencies = dependencies.Concat(project.projects).ToList();

            var load_projects = all_dependencies.Select(render_find_package).join("");

            var name = project.name;

            string output = ""
                + load_projects + "\r\n"

                + "set(" + name + "_includes\r\n"
                + all_dependencies.Select(d => "\t${" + d.name + "_includes}\r\n").join("")
                + "  ${CMAKE_CURRENT_LIST_DIR}\r\n"
                + ")\r\n\r\n"

                + "set(" + name + "_sources\r\n"
                + sources.join("\r\n")
                + "\r\n"
                + ")\r\n\r\n"

                + "string(REPLACE \"\\\\\" \"/\" \"BIN_PATH\" \"${CMAKE_RUNTIME_OUTPUT_DIRECTORY}\")\r\n"

                + "set(" + name + "_libs\r\n"
                + "\t${BIN_PATH}/lib" + name + ".dll.a" + "\r\n"
                + ")\r\n"

                + "";

            Generator.create_file(dir + project.name + "-config.cmake", output);
        }

        public static string render_cmakelists_txt_header(string name)
        {
            return ""
                + "cmake_minimum_required(VERSION 3.3)\r\n\r\n"

                + (string.IsNullOrEmpty(name) ? "" : "project(" + name + ")\r\n\r\n"

                + "set(CMAKE_CXX_FLAGS \"${CMAKE_CXX_FLAGS} -std=c++11\")\r\n\r\n")

                + "";
        }

        public static void create_cmakelists_txt(Build_Orders project, List<Project> dependencies)
        {
            var dir = project.output + "/";
            var name = project.name;
            var all_dependencies = dependencies.Concat(project.projects).ToList();

            var text = ""
                + render_cmakelists_txt_header(name)

                + "include(" + name + "-config.cmake)\r\n\r\n"

                + "add_library(" + name + " SHARED\r\n"
                + "\t${" + name + "_sources}\r\n"
                + ")\r\n\r\n"

                + "include_directories(\r\n"
                + "\t${" + name + "_includes}\r\n"
                + ")\r\n\r\n"

                + "target_link_libraries(" + name + "\r\n"
                + all_dependencies.Select(d => "\t${" + d.name + "_libs}\r\n").join("")
                + ")\r\n\r\n"

                + "";

            Generator.create_file(dir + "CMakeLists.txt", text);
        }

        public static void create_wrapper_cmakelists_txt(Project project)
        {
            var dir = Path.GetDirectoryName(project.path).Replace("\\", "/") + "/";
            var name = project.name;

            var text = ""
                + render_cmakelists_txt_header(name)

                + great_wrapper_project_entries(project.projects).join("\r\n\r\n")

                + "";

            Generator.create_file(dir + "CMakeLists.txt", text);
        }

        public static List<string> great_wrapper_project_entries(List<IProject> projects)
        {
            var result = new List<string>();
            for (var i = 0; i < projects.Count; ++i)
            {
                var project = projects[i];
                var entry = render_sub_project_include(project);
                if (i > 0)
                {
                    entry += "\r\n"
                        + "add_dependencies("
                        + project.name.Replace('/', '_')
                        + " " + projects[i - 1].name.Replace('/', '_')
                        + ")";
                }
                result.Add(entry);
            }

            return result;
        }

        public static string render_sub_project_include(IProject project)
        {
            var dir = project.relative_path + (project.GetType() == typeof(Project) ? "/output" : "");
            var name = project.name.Replace('/', '_');

            return ""
                + "set(" + name + "_DIR ${CMAKE_SOURCE_DIR}/" + dir + ")\r\n"
                + "add_subdirectory(" + dir + ")"
                + "";
        }

        public static void gather_source_paths(IEnumerable<Dungeon> dungeons, List<String> sources)
        {
            foreach (var dungeon in dungeons)
            {
                if (dungeon.is_external || dungeon.is_enum || (dungeon.is_abstract && dungeon.is_external))
                    continue;

                if (dungeon.portals.Length > 0 || dungeon.minions_old.Count > 0)
                {
                    var space = Generator.get_namespace_path(dungeon.realm).join("/") + "/";
                    if (space == "/")
                        space = "";

                    sources.Add("\t" + space + dungeon.name + ".cpp");
                }
            }
        }

        public static IEnumerable<Dungeon> get_dungeon_dependencies(IEnumerable<Dungeon> dungeons)
        {
            return dungeons.SelectMany(d =>
                d.dependencies.Values.Select(d2 => d2.dungeon)).Distinct();
        }

        public static List<Project> get_project_dependencies(Project project)
        {
            var projects = get_project_dependencies(project.dungeons.Values, new[] { project }).ToList();
            return projects.Concat(projects.SelectMany(get_project_dependencies))
                .Distinct()
                .ToList();
        }

        public static IEnumerable<Project> get_project_dependencies(IEnumerable<Dungeon> dungeons, IEnumerable<Project> exclude)
        {
            return get_dungeon_dependencies(dungeons)
                .Where(d => d.project != null)
                .Select(d => d.project).Distinct()
                .Except(exclude);
        }

        public static string render_find_package(IProject project)
        {
            return "find_package(" + project.name + ")\r\n";
        }
    }
}
