using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.schema;

namespace imperative.scholar.crawling
{
    public delegate void Dungeon_Delegate(Dungeon dungeon);
    public delegate List<Expression> Expression_Lister(Expression expression);

    public static class Dungeon_Crawler
    {
        public static void crawl(Dungeon dungeon, Dungeon_Delegate action, Expression_Lister expression_action = null)
        {
            if (dungeon.parent != null && !dungeon.parent.dungeon.is_abstract)
                action(dungeon.parent.dungeon);

            foreach (var face in dungeon.interfaces)
            {
                action(face.dungeon);
            }

            crawl_portals(dungeon, action);
            crawl_minions(dungeon, action, expression_action);
        }

        public static void crawl_portals(Dungeon dungeon, Dungeon_Delegate action)
        {
            foreach (var portal in dungeon.all_portals.Values)
            {
                var other_dungeon = portal.other_dungeon;
                if (other_dungeon != null && (other_dungeon.GetType() != typeof(Dungeon) || !other_dungeon.is_abstract))
                {
                    action(portal.other_dungeon);
                    if (portal.profession.children != null)
                    {
                        foreach (var profession in portal.profession.children)
                        {
                            action(profession.dungeon);
                        }
                    }
                }
            }
        }

        public static void crawl_minions(Dungeon dungeon, Dungeon_Delegate action,
            Expression_Lister expression_action = null)
        {
            foreach (var minion in dungeon.minions_old.Values)
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

        public static void crawl_expressions(List<Expression> expressions, Dungeon_Delegate action,
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

        public static void crawl_expression(Expression expression, Dungeon_Delegate action)
        {
            switch (expression.type)
            {
                case Expression_Type.declare_variable:
                    var declare_variable = (Declare_Variable)expression;
                    analyze_profession(declare_variable.symbol.profession, action);
                    break;

                case Expression_Type.portal:
                    var portal_expression = (Portal_Expression)expression;
                    action(portal_expression.portal.dungeon);
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

        public static void analyze_profession(Profession profession, Dungeon_Delegate action)
        {
            if (profession.dungeon != null)
                action(profession.dungeon);

            if (profession.children == null)
                return;

            foreach (var child in profession.children)
            {
                analyze_profession(child, action);
            }
        }
    }
}
