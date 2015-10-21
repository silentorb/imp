using System;
using System.Collections.Generic;
using System.IO;
using imperative;
using imperative.render.artisan;
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
                //                case "cpp":
                //                    return new Cpp(minion);

                case "js":
                    return new JavaScript(minion);

                case "php":
                    return new PHP(minion);

                default:
                    throw new Exception("Unsupported target: " + target_name + ".");
            }
        }

        public static Common_Target2 create_target2(Overlord minion, string target_name)
        {
            switch (target_name)
            {
                case "js":
                    return new imperative.render.artisan.targets.JavaScript(minion);

                default:
                    throw new Exception("Unsupported target: " + target_name + ".");
            }
        }

        public static void run(Target target, Overlord_Configuration config)
        {
            create_folder(config.output);
            //Utility.clear_folder(output_folder);
            target.run(config);
        }

        public static List<string> get_namespace_path(Dungeon region)
        {
            var tokens = new List<string>();
            while (region != null && region.name != "")
            {
                tokens.Insert(0, region.external_name ?? region.name);
                region = region.realm;
            }

            return tokens;
        }

        public static void create_folder(string url)
        {
            Directory.CreateDirectory(url);
        }

        public static void create_file(string url, string contents)
        {
            var directory = Path.GetDirectoryName(url);
            if (!Directory.Exists(directory))
                create_folder(directory);

//            if (File.Exists(url))
//            {
//                var current_contents = File.ReadAllText(url);
//                if (contents == current_contents)
//                    return;
//            }
            File.WriteAllText(url, contents);
        }

        public static void clear_folder(string path)
        {
            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                File.Delete(file);
            }

            var folders = Directory.GetDirectories(path);
            foreach (var folder in folders)
            {
                Directory.Delete(folder, true);
            }
        }
    }
}