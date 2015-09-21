using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            var load_projects = dependencies.Select(render_find_package).join("");
            var name = project.name;

            string output = ""
                + load_projects + "\r\n"

                + "set(" + name + "_includes\r\n"
                + dependencies.Select(d => "\t${" + d.name + "_includes}\r\n").join("")
                + "  ${CMAKE_CURRENT_LIST_DIR}\r\n"
                + ")\r\n\r\n"

                + "set(" + name + "_sources\r\n"
                + sources.join("\r\n")
                + "\r\n"
                + ")\r\n\r\n"

                + "set(" + name + "_libs\r\n"
                + "\t${CMAKE_BINARY_DIR}/lib" + name + ".a" + "\r\n"
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
            var text = ""
                + render_cmakelists_txt_header(name)

                + "include(" + name + "-config.cmake)\r\n\r\n"

                + "add_library(" + name + "\r\n"
                + "\t${" + name + "_sources}\r\n"
                + ")\r\n\r\n"

                + "include_directories(\r\n"
                + "\t${" + name + "_includes}\r\n"
                + ")\r\n\r\n"

                + "target_link_libraries(" + name + "\r\n"
                + dependencies.Select(d => "\t${" + d.name + "_libs}\r\n").join("")
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
               
                + project.projects.Select(render_sub_project_include).join("\r\n\r\n")
                
                + "";

            Generator.create_file(dir + "CMakeLists.txt", text);
        }

        public static string render_sub_project_include(Project project)
        {
            return ""
                + "set(" + project.name + "_DIR ${CMAKE_SOURCE_DIR}/" + project.name + "/output)\r\n"
                + "add_subdirectory(" + project.name + "/output)"
                + "";
        }

        public static void gather_source_paths(IEnumerable<Dungeon> dungeons, List<String> sources)
        {
            foreach (var dungeon in dungeons)
            {
                if (dungeon.is_external || (dungeon.is_abstract && dungeon.is_external))
                    continue;

                if (dungeon.portals.Length > 0 || dungeon.minions.Count > 0)
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
                d.dependencies.Values.Select(d2 => (Dungeon)d2.dungeon)).Distinct();
        }

        public static List<Project> get_project_dependencies(Project project)
        {
            return get_project_dependencies(project.dungeons.Values, new[] { project }).ToList();
        }

        public static IEnumerable<Project> get_project_dependencies(IEnumerable<Dungeon> dungeons, IEnumerable<Project> exclude)
        {
            return get_dungeon_dependencies(dungeons)
                .Where(d => d.project != null)
                .Select(d => d.project).Distinct()
                .Except(exclude);
        }

        public static string render_find_package(Project project)
        {
            return "find_package(" + project.name + ")\r\n";
        }
    }
}
