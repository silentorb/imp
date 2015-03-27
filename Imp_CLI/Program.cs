using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using imperative;
using metahub.render.targets;

namespace imp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
                throw new Exception("Missing configuration path argument.");

            var input = "";
            var output = "";
            for (var i = 0; i < args.Length; ++i)
            {
                var arg = args[i];
                switch (arg)
                {
                    case "-i":
                        input = args[i + 1];
                        break;

                    case "-o":
                        output = args[i + 1];
                        break;
                }
            }

            var overlord = new Overlord(new JavaScript());
            var files = Directory.Exists(input)
                ? aggregate_files(input)
                : new List<string> { input };

            var codes = files.Select(File.ReadAllText);
            overlord.summon_many(codes);

            overlord.flatten();
            overlord.post_analyze();

            overlord.target.run(output);

            Console.WriteLine("done.");
        }

        static List<string> aggregate_files(string path)
        {
            var result = new List<string>();
            result.AddRange(Directory.GetFiles(path));
            foreach (var directory in Directory.GetDirectories(path))
            {
                result.AddRange(aggregate_files(directory));
            }

            return result;
        }
    }
}
