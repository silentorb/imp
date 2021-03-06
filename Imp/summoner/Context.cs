﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.schema;
using imperative.expressions;
using imperative.legion;
using runic.parser;

namespace imperative.summoner
{
    public class Summoner_Context
    {
        public List<Dungeon> imported_realms = new List<Dungeon>();
        public Dungeon dungeon;
        public Scope scope;
        public Summoner_Context parent;
        public Dictionary<string, Summoner_Context> children = new Dictionary<string, Summoner_Context>();
        public List<Legend> legends = new List<Legend>();
        protected Dictionary<string, string> string_inserts = new Dictionary<string, string>();
        protected Dictionary<string, Profession> profession_inserts = new Dictionary<string, Profession>();
        protected Dictionary<string, Expression_Generator> expression_lambda_inserts = new Dictionary<string, Expression_Generator>();
        protected Dictionary<string, Expression> expression_inserts = new Dictionary<string, Expression>();
        public Project project;

        public Summoner_Context()
        {
        }

        public Summoner_Context(Legend legend, Summoner_Context parent)
        {
            if (legend != null)
                legends.Add(legend);

            this.parent = parent;
            project = parent.project;
        }

        public Summoner_Context(Legend legend, Project project, Dungeon dungeon = null)
        {
            if (legend != null)
                legends.Add(legend);

            this.dungeon = dungeon;
            this.project = project;
        }


        public Summoner_Context(Minion minion, Project project)
        {
            dungeon = minion.dungeon;
            scope = new Scope(minion.scope);
            this.project = project;
        }

        public Summoner_Context(Summoner_Context parent)
        {
            this.parent = parent;
            dungeon = parent.dungeon;
            scope = parent.scope;
            this.imported_realms = parent.imported_realms;
            project = parent.project;
        }

        public Profession set_pattern(string name, Profession profession)
        {
            profession_inserts[name] = profession;
            return profession;
        }

        public string set_pattern(string name, string text)
        {
            string_inserts[name] = text;
            return text;
        }

        public Expression_Generator set_pattern(string name, Expression_Generator generator)
        {
            expression_lambda_inserts[name] = generator;
            return generator;
        }

        public Expression set_pattern(string name, Expression generator)
        {
            expression_inserts[name] = generator;
            return generator;
        }

        public Profession get_profession_pattern(string name)
        {
            if (profession_inserts.ContainsKey(name))
                return profession_inserts[name];

            if (parent != null)
                return parent.get_profession_pattern(name);

            return null;
        }

        public string get_string_pattern(string name)
        {
            if (string_inserts.ContainsKey(name))
                return string_inserts[name];

            if (parent != null)
                return parent.get_string_pattern(name);

            return null;
        }

        public Expression get_expression_pattern(string name, Summoner_Context context = null)
        {
            context = context ?? this;
            if (expression_lambda_inserts.ContainsKey(name))
                return expression_lambda_inserts[name](context);

            if (expression_inserts.ContainsKey(name))
                return expression_inserts[name].clone();

            if (parent != null)
                return parent.get_expression_pattern(name, context);

            return null;
        }

        public IDungeon get_dungeon(string[] path)
        {
            var result = dungeon.get_dungeon(path, false);
            if (result != null)
                return result;

            if (dungeon.realm != null)
            {
                result = dungeon.realm.get_dungeon(path, false);
                if (result != null)
                    return result;
            }

            foreach (var imported_realm in imported_realms)
            {
                result = imported_realm.get_dungeon(path, false);
                if (result != null)
                    return result;
            }

            return dungeon.overlord.root.get_dungeon(path);
        }

        public void add_dungeon_to_project(Dungeon dungeon)
        {
            if (project != null && !dungeon.is_external)
            {
                project.dungeons[dungeon.name] = dungeon;
                dungeon.project = project;
            }
        }
    }
}
