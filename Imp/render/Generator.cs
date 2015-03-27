using System;
using System.Collections.Generic;
using System.IO;
using imperative;
using imperative.schema;
using metahub.render.targets;
using metahub.render.targets.php;

namespace metahub.render
{

    public static class Generator
    {
        public static Target create_target(Overlord minion, string target_name)
        {
            switch (target_name)
            {
                case "cpp":
                    return new Cpp(minion);

                case "js":
                    return new JavaScript(minion);

                case "php":
                    return new PHP(minion);

                default:
                    throw new Exception("Unsupported target: " + target_name + ".");
            }
        }

        public static void run(Target target, string output_folder)
        {
            create_folder(output_folder);
            //Utility.clear_folder(output_folder);
            target.run(output_folder);
        }

        public static List<string> get_namespace_path(Realm region)
        {
            var tokens = new List<string>();
            while (region != null && region.name != "root")
            {
                tokens.Insert(0, region.external_name ?? region.name);
                //region = region.parent;
                break;
            }

            return tokens;
        }

        public static void create_folder(string url)
        {
            Directory.CreateDirectory(url);
        }

        public static void create_file(string url, string contents)
        {
            File.WriteAllText(url, contents);
        }
    }
}