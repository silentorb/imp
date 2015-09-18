using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.legion;
using imperative.schema;
using metahub.render;

namespace imperative.render.artisan.targets.cpp
{
    static class CMake
    {
        public static void create_files(Project orders, Overlord overlord)
        {
            var dir = orders.output + "/";
            create_cmakelists_txt(orders, overlord, dir);
            create_config(orders, overlord, dir);
        }

        static void create_cmakelists_txt(Project orders, Overlord overlord, string dir)
        {
            var sources = new List<String>();
            gather_source_paths(orders.dungeons.Values, sources);

            var dependencies = get_project_dependencies(orders.dungeons.Values, new[] { orders }).ToList();
            var load_projects = dependencies.Select(render_find_package).join("");

            string output = ""
                + load_projects + "\r\n"
                + "set(" + orders.name + "_includes\r\n"
                + dependencies.Select(d => "\t${" + d.name + "_includes}\r\n").join("")
                + "  ${CMAKE_CURRENT_LIST_DIR}\r\n"
                + ")\r\n"
                + "\r\n"
                + "set(" + orders.name + "_sources\r\n"
                + sources.join("\r\n")
                + "\r\n"
                + ")\r\n";

            Generator.create_file(dir + orders.name + "-config.cmake", output);

        }

        private static void create_config(Build_Orders orders, Overlord overlord, string dir)
        {
            Generator.create_file(dir + "CMakeLists.txt", "project(" + orders.name + ")");
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
                //                gather_source_paths(child, sources);
            }
        }

        public static IEnumerable<Dungeon> get_dungeon_dependencies(IEnumerable<Dungeon> dungeons)
        {
            return dungeons.SelectMany(d =>
                d.dependencies.Values.Select(d2 => (Dungeon)d2.dungeon)).Distinct();
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
