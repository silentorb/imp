using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative;
using imperative.expressions;
using imperative.schema;
using imperative.summoner;
using metahub.render;
using runic.parser;

namespace imp_test.fixtures
{
    public static class Imp_Fixture
    {
        public static Overlord create_overlord(string target, string script_name)
        {
            return create_overlord(target, new[] { script_name });
        }

        public static Overlord create_overlord(string target_name, string[] script_names)
        {
            var overlord = new Overlord(target_name);

            foreach (var script_name in script_names)
            {
                var code = Utility.load_resource(script_name);
                overlord.summon(code, script_name);
            }

            overlord.flatten();
            overlord.post_analyze();
            return overlord;
        }

        public static Overlord create_overlord_with_path(string target_name, string script_path)
        {
            var overlord = new Overlord(target_name);
            overlord.summon_input(script_path);
            overlord.flatten();
            overlord.post_analyze();
            return overlord;
        }

        public static Expression summon_statement(Legend legend)
        {
            var overlord = new Overlord();
            var summoner = new Summoner2(overlord);
            var context = new Summoner_Context();
            context.scope = new Scope();
            return summoner.summon_statement(legend, context);
        }

        public static List<Expression> summon_statements(List<Legend> legend)
        {
            var overlord = new Overlord();
            var summoner = new Summoner2(overlord);
            var context = new Summoner_Context();
            context.scope = new Scope();
            return summoner.summon_statements(legend, context);
        }

    }
}
