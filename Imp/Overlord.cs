using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using imperative.schema;
using imperative.summoner;
using imperative.expressions;
using imperative.legion;
using imperative.render.artisan;
using imperative.render.artisan.targets;
using imperative.render.artisan.targets.cpp;
using library;
using metahub.jackolantern.expressions;
using metahub.render;
using runic.parser;

namespace imperative
{
    public interface Build_Orders
    {
        List<string> inputs { get; }
        string output { get; }
        string target { get; }
        string name { get; }
        string path { get; set; }
        Dictionary<string, Dungeon> dungeons { get; }
        List<IProject> projects { get; }
    }

    public class Overlord_Configuration : Build_Orders
    {
        public List<string> inputs { get; set; }
        public string output { get; set; }
        public string target { get; set; }
        public string name { get; set; }
        public string path { get; set; }
        public Dictionary<string, Dungeon> dungeons { get; set; }
        public List<IProject> projects { get; private set; }

        public Overlord_Configuration()
        {
            inputs = new List<string>();
            output = "";
            target = "";
            name = "";
            projects = new List<IProject>();
        }
    }

    public class Overlord
    {
        public List<Dungeon> dungeons = new List<Dungeon>();
        public Dungeon root;
        public Common_Target2 target;
        public Dungeon array;
        public Professions library;

        public Overlord()
        {
            if (Platform_Function_Info.functions == null)
                Platform_Function_Info.initialize();

            library = new Professions();

            root = new Dungeon("", this, null);

            root.dungeons["Dictionary"] = Professions.Dictionary;
        }

        public Overlord(string target_name)
            : this()
        {
            target = create_target(target_name);
        }

        public Common_Target2 create_target(string name)
        {
            switch (name)
            {
                case "js":
                    return new JavaScript(this);

                case "cs":
                    return new Csharp(this);

                case "cpp":
                    return new Cpp(this);
            }

            throw new Exception("Invalid imp target: " + name + ".");
        }

        public void post_analyze()
        {
            foreach (var dungeon in dungeons.Where(d => !d.is_external))
            {
                dungeon.analyze();
            }
        }

//        public void flatten()
//        {
//            var temp = dungeons.Where(d => !d.is_external).Select(d => d.name);
//
//            foreach (var dungeon in dungeons.Where(d => !d.is_external))
//            {
//                dungeon.flatten();
//            }
//        }

        //        public void summon(string code, string filename, bool is_external = false)
        //        {
        //            summon2(code, filename, is_external);
        //        }

        public void summon(string code, string filename,Project project = null, bool is_external = false)
        {
            var legend = summon_legend(code, filename);
            var summoner = new Summoner(this, is_external);
            summoner.summon_many(new[] { legend }, project);
        }

        public Legend summon_legend(string code, string filename)
        {
            var runes = Summoner.read_runes(code, filename);
            return Summoner.translate_runes(code, runes);
        }

        public void summon_many(IEnumerable<string> files, Project project)
        {
            var pre_summoners = files.Select(file =>
                summon_legend(File.ReadAllText(file), file)
            );
            var summoner = new Summoner(this);
            summoner.summon_many(pre_summoners, project);
        }

        public Dungeon summon_dungeon(Snippet template, Summoner_Context context)
        {
            var summoner = new Summoner(this);
            throw new Exception("Not implemented.");
            //            summoner.process_dungeon1(template.source, context);
            //            return summoner.process_dungeon2(template.source, context);
        }

        public Expression summon_snippet(Snippet template, Summoner_Context context)
        {
            var summoner = new Summoner(this);
            return summoner.summon_statement(template.source, context);
        }

        public Dictionary<string, Snippet> summon_snippets(string code, string filename)
        {
            var templates = new Dictionary<string, Snippet>();
            var runes = Summoner.read_runes(code, filename);
            var legend = Summoner.translate_runes(code, runes, "snippets");
            var summoner = new Summoner(this);

            var context = new Summoner_Context();
            var statements = (Block)summoner.summon_statement(legend, context);
            foreach (Snippet snippet in statements.body)
            {
                templates[snippet.name] = snippet;
            }
            return templates;
        }

        public void summon_file(string path, Project project, bool is_external = false)
        {
            summon(File.ReadAllText(path), path,project, is_external);
        }

        public static void run(Build_Orders config)
        {
            var overlord = new Overlord(config.target);
            foreach (var input in config.inputs)
            {
                var sources = get_source_files(input);
                overlord.summon_input(sources, null);
                overlord.generate(config, sources);
            }
        }

        public static string[] get_source_files(string input)
        {
            if (File.Exists(input))
                return new[] { input };

            return aggregate_files(input).ToArray();
        }

        public void summon_input(string[] sources, Project project)
        {
            if (sources.Length == 1)
            {
                // Very quick-and-dirty but it works for now.
                var cwd = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(Path.GetDirectoryName(sources[0]));
                summon_file(sources[0], project);
                Directory.SetCurrentDirectory(cwd);
            }
            else
            {
                summon_many(sources.Where(f => Path.GetExtension(f) == ".imp"), project);
            }
        }

        public void generate(Build_Orders config, string[] sources)
        {
            post_analyze();

//            if (Directory.Exists(config.output))
//                Generator.clear_folder(config.output);

            target.run(config, sources);
        }

        public static List<string> aggregate_files(string path)
        {
            var result = new List<string>();
            result.AddRange(Directory.GetFiles(path, "*.imp"));
            foreach (var directory in Directory.GetDirectories(path))
            {
                result.AddRange(aggregate_files(directory));
            }

            return result;
        }

        public Dungeon load_standard_library()
        {
            if (!root.dungeons.ContainsKey("imp"))
            {
                var code = Library.load_resource("imp.collections.Array.imp");
                summon(code, "Standard Library", null);
                root.dungeons["imp"].dungeons["collections"].is_virtual = true;
                root.dungeons["imp"].dungeons["collections"].is_standard = true;
                array = root.dungeons["imp"].dungeons["collections"].dungeons["Array"];
            }

            return root.dungeons["imp"];
        }

        public static string strokes_to_string(List<Stroke> strokes)
        {
            var passages = Painter.render_root(strokes).ToList();
            var segments = new List<Segment>();
            return Scribe.render(passages, segments);
        }

        public static string stroke_to_string(Stroke strokes)
        {
            return strokes_to_string(new List<Stroke> { strokes });
        }
    }
}