using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.schema;
using metahub.render;

namespace imperative.render.artisan.targets.cpp
{
    static class CMake
    {
        public static void create_files(Build_Orders orders, Overlord overlord)
        {
            var dir = orders.output + "/";
            create_cmakelists_txt(orders, overlord, dir);
            create_config(orders, overlord, dir);
        }

        static void create_cmakelists_txt(Build_Orders orders, Overlord overlord, string dir)
        {
            var sources = new List<String>();

            gather_source_paths(overlord.root, sources);
            string output =
                "set(" + orders.name + "_includes\r\n"
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

        public static void gather_source_paths(Dungeon dungeon, List<String> sources)
        {
            if (dungeon.portals.Length > 0 || dungeon.minions.Count > 0)
            {
                var space = Generator.get_namespace_path(dungeon.realm).join("/") + "/";
                if (space == "/")
                    space = "";

                sources.Add("\t" + space + dungeon.name + ".cpp");
            }

            foreach (var child in dungeon.dungeons.Values)
            {
                if (child.is_external || (child.is_abstract && child.is_external))
                    continue;

                gather_source_paths(child, sources);
            }
        }

    }
}
