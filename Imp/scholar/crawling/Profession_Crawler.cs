using System;
using System.Collections.Generic;
using System.Linq;
using imperative.expressions;
using imperative.schema;

namespace imperative.scholar.crawling
{
    public static class Profession_Crawler
    {
        public delegate void Profession_Delegate(Profession profession);
        public static void crawl(Dungeon dungeon, Profession_Delegate action, Expression_Lister expression_action = null)
        {
            crawl_portals(dungeon, action);
            crawl_minions(dungeon, action, expression_action);
        }

        public static void crawl_portals(Dungeon dungeon, Profession_Delegate action)
        {
            foreach (var portal in dungeon.all_portals.Values)
            {
                var other_dungeon = portal.other_dungeon;
                if (other_dungeon != null && (other_dungeon.GetType() != typeof(Dungeon) || !other_dungeon.is_abstract))
                {
                    analyze_profession(portal.profession, action);
                }
            }
        }

        public static void crawl_minions(Dungeon dungeon, Profession_Delegate action,
            Expression_Lister expression_action = null)
        {
            foreach (var minion in dungeon.minions.Values)
            {
                if (minion.return_type != null)
                    analyze_profession(minion.return_type, action);

                foreach (var parameter in minion.parameters)
                {
                    analyze_profession(parameter.symbol.profession, action);
                }

                crawl_expressions(minion.expressions, action, expression_action);
            }
        }

        public static void crawl_expressions(List<Expression> expressions, Profession_Delegate action,
            Expression_Lister expression_action = null)
        {
            Crawler.analyze_expressions(expressions, e =>
            {
                crawl_expression(e, action);
                if (expression_action != null)
                {
                    var children = expression_action(e);
                    if (children != null)
                    {
                        foreach (var ex in children)
                        {
                            Crawler.analyze_expression(ex, e2 => crawl_expression(e2, action));
                        }
                    }
                }
            });
        }

        public static void crawl_expression(Expression expression, Profession_Delegate action)
        {
            switch (expression.type)
            {
                case Expression_Type.declare_variable:
                    var declare_variable = (Declare_Variable)expression;
                    analyze_profession(declare_variable.symbol.profession, action);
                    break;

                case Expression_Type.portal:
                    var portal_expression = (Portal_Expression)expression;
                    analyze_profession(portal_expression.get_profession(), action);
                    break;

                case Expression_Type.instantiate:
                    var instantiation = (Instantiate)expression;
                    analyze_profession(instantiation.profession, action);
                    break;

                case Expression_Type.variable:
                    var variable_expression = (Variable)expression;
                    if (!Professions.is_scalar(variable_expression.symbol.profession))
                        analyze_profession(variable_expression.symbol.profession, action);

                    break;

            }
        }

        public static void analyze_profession(Profession profession, Profession_Delegate action)
        {
            action(profession);

            if (profession.children == null)
                return;

            foreach (var child in profession.children)
            {
                analyze_profession(child, action);
            }
        }
    }
}
