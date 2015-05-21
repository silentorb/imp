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
        public Dungeon root;
        public Target target;
        public Dungeon array;
        public Professions library;

        public Overlord()
        {
            if (Platform_Function_Info.functions == null)
                Platform_Function_Info.initialize();

            library = new Professions();

            root = new Dungeon("", this, null);
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

        public void summon_file(string path)
        {
            summon(File.ReadAllText(path), path);
        }

        public static void run(Overlord_Configuration config)
        {
            var overlord = new Overlord(config.target);
            if (File.Exists(config.input))
            {
                overlord.summon_file(config.input);
            }
            else
            {
                var files = aggregate_files(config.input);
                overlord.summon_many(files.Where(f => Path.GetExtension(f) == ".imp"));
            }

            overlord.generate(config);
        }

        public void generate(Overlord_Configuration config)
        {
            flatten();
            post_analyze();

            if (config.output == "")
            {
                config.output = Path.GetDirectoryName(config.input);
            }
            else
            {
                if (Directory.Exists(config.output))
                    Generator.clear_folder(config.output);
            }
            target.run(config);
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

        public Dungeon load_standard_library()
        {
            if (!root.dungeons.ContainsKey("imp"))
            {
                var code = Library.load_resource("imp.collections.Array.imp");
                summon2(code, "Standard Library");
                root.dungeons["imp"].dungeons["collections"].is_virtual = true;
                array = root.dungeons["imp"].dungeons["collections"].dungeons["Array"];
            }

            return root.dungeons["imp"];
        }
    }
}