using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.schema;
using imperative.expressions;

namespace imperative.summoner
{
    public class Summoner_Context
    {
        public Realm realm;
        public Dungeon dungeon;
        public Scope scope;
        public Summoner_Context parent;
        protected Dictionary<string, string> string_inserts = new Dictionary<string, string>();
        protected Dictionary<string, Profession> profession_inserts = new Dictionary<string, Profession>();
        protected Dictionary<string, Expression_Generator> expression_lambda_inserts = new Dictionary<string, Expression_Generator>();
        protected Dictionary<string, Expression> expression_inserts = new Dictionary<string, Expression>();

        public Summoner_Context()
        {
        }

        public Summoner_Context(Realm realm, Dungeon dungeon = null)
        {
            this.realm = realm;
            this.dungeon = dungeon;
        }

        public Summoner_Context(Dungeon dungeon)
        {
            realm = dungeon.realm;
            this.dungeon = dungeon;
        }

        public Summoner_Context(Minion minion)
        {
            realm = minion.dungeon.realm;
            dungeon = minion.dungeon;
            scope = new Scope(minion.scope);
        }

        public Summoner_Context(Summoner_Context parent)
        {
            this.parent = parent;
            realm = parent.realm;
            dungeon = parent.dungeon;
            scope = parent.scope;
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
    }
}
