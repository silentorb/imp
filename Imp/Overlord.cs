using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using imperative.schema;
using imperative.summoner;
using imperative.expressions;
using imperative.render.targets;
using library;
using metahub.jackolantern;
using metahub.jackolantern.expressions;

using metahub.render;
using metahub.render.targets;
using metahub.schema;
using runic.parser;
using Literal = imperative.expressions.Literal;

namespace imperative
{
    public class Overlord_Configuration
    {
        public string input = "";
        public string output = "";
        public string target = "";
    }

    public class Overlord
    {
        public List<Dungeon> dungeons = new List<Dungeon>();
        public Realm root;
        public Target target;
        public Dungeon array;

        public Overlord()
        {
            if (Platform_Function_Info.functions == null)
                Platform_Function_Info.initialize();

            root = new Realm("", this);
        }

        public Overlord(string target_name)
            : this()
        {
            target = create_target(target_name);
        }

        public Target create_target(string name)
        {
            switch (name)
            {
                case "js":
                    return new JavaScript(this);

                case "cs":
                    return new Csharp(this);
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

        public void flatten()
        {
            var temp = dungeons.Where(d => !d.is_external).Select(d => d.name);

            foreach (var dungeon in dungeons.Where(d => !d.is_external))
            {
                dungeon.flatten();
            }
        }

        public void summon(string code, string filename)
        {
            summon2(code, filename);
        }

        public void summon2(string code, string filename)
        {
            var legend = summon_legend(code, filename);
            var summoner = new Summoner2(this);
            summoner.summon(legend);
        }

        Legend summon_legend(string code, string filename)
        {
            var runes = Summoner2.read_runes(code, filename);
            return Summoner2.translate_runes(code, runes);
        }

        public void summon_many(IEnumerable<string> files)
        {
            var pre_summoners = files.Select(file =>
                summon_legend(File.ReadAllText(file), file)
            );
            var summoner = new Summoner2(this);
            summoner.summon_many(pre_summoners);
        }

        public Dungeon summon_dungeon(Snippet template, Summoner_Context context)
        {
            var summoner = new Summoner2(this);
            summoner.process_dungeon1(template.source, context);
            return summoner.process_dungeon2(template.source, context);
        }

        public Expression summon_snippet(Snippet template, Summoner_Context context)
        {
            var summoner = new Summoner2(this);
            return summoner.summon_statement(template.source, context);
        }

        public Dictionary<string, Snippet> summon_snippets(string code, string filename)
        {
            var templates = new Dictionary<string, Snippet>();
            //var match = Regex.Matches(code,
            //    @"@@@[ \t]*(\w+)[ \t]*\([ \t]*(.*?)[ \t]*\)[ \t]*\r\n(.*?)(?=@@@|\z)", RegexOptions.Singleline);
            //foreach (Match item in match)
            //{
            //    foreach (Match capture in item.Captures)
            //    {
            //        var name = capture.Groups[1].Value;
            //        var parameters = Regex.Split(capture.Groups[2].Value, @"\s*,\s*");
            //        var block = capture.Groups[3].Value;
            //        var pre_summoner = pre_summon(block, Pre_Summoner.Mode.snippet);
            //        templates[name] = new Template(name, parameters, pre_summoner.output.patterns[1]);
            //    }
            //}

            //            var pre_summoner = pre_summon(code, Pre_Summoner.Mode.snippet);
            var runes = Summoner2.read_runes(code, filename);
            var legend = Summoner2.translate_runes(code, runes, "snippets");
            var summoner = new Summoner2(this);

            var context = new Summoner_Context();
            var statements = (Block)summoner.summon_statement(legend, context);
            foreach (Snippet snippet in statements.body)
            {
                templates[snippet.name] = snippet;
            }
            return templates;
        }

        public static List<string> aggregate_files(string path)
        {
            var result = new List<string>();
            result.AddRange(Directory.GetFiles(path));
            foreach (var directory in Directory.GetDirectories(path))
            {
                result.AddRange(aggregate_files(directory));
            }

            return result;
        }

        public static void run(Overlord_Configuration config)
        {
            var overlord = new Overlord(config.target);
            var files = Directory.Exists(config.input)
                ? Overlord.aggregate_files(config.input)
                : new List<string> { config.input };

            overlord.summon_many(files);

            overlord.flatten();
            overlord.post_analyze();

            overlord.target.run(config.output);
        }

        public Realm load_standard_library()
        {
            if (!root.children.ContainsKey("metahub"))
            {
                var code = Library.load_resource("metahub.collections.Array.imp");
                summon2(code, "Standard Library");
                root.children["metahub"].children["collections"].is_virtual = true;
                array = root.children["metahub"].children["collections"].dungeons["Array"];
            }

            return root.children["metahub"];
        }
    }
}