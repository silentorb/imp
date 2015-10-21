using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.schema;
using imperative.scholar;

namespace imperative.render.artisan.targets.cpp
{
    public static class Ownership
    {
        public static void analyze_dungeon(Dungeon dungeon)
        {
            analyze_minions(dungeon);
        }

        public static void analyze_minions(Dungeon dungeon)
        {
            foreach (var group in dungeon.minions_more.Values)
            {
                foreach (var minion in group)
                {
                    if (minion.name == "constructor")
                    {
                        analyze_constructor(minion);
                    }

                    analyze_minion(minion);
                }
            }
        }

        static List<Symbol> get_local_variables(Minion minion)
        {
            var result = new List<Symbol>();
            Crawler.analyze_expressions(minion.expressions, expression =>
            {
                if (expression.type == Expression_Type.declare_variable)
                {
                    var declaration = (Declare_Variable)expression;
                    result.Add(declaration.symbol);
                }
            });

            return result;
        }

        static void analyze_minion(Minion minion)
        {
            var symbols = get_local_variables(minion);
            foreach (var symbol in symbols)
            {
                analyze_local_variable(minion, symbol);
            }
        }

        static void analyze_local_variable(Minion minion, Symbol symbol)
        {
            if (local_variable_is_beyond_scope(minion, symbol))
            {
                symbol.is_owner = false;
            }
        }

        static bool local_variable_is_beyond_scope(Minion minion, Symbol symbol)
        {
            bool result = false;
            Crawler.analyze_expressions(minion.expressions, expression =>
            {
                if (result)
                    return;

                if (expression.type == Expression_Type.assignment)
                {
                    var assignment = (Assignment)expression;
                    var end = assignment.expression.get_end();
                    if (end.type == Expression_Type.variable)
                    {
                        var variable = (Variable)end;
                        if (variable.symbol == symbol)
                        {
                            var target_end = assignment.target.get_end();
                            if (target_end.type == Expression_Type.portal)
                            {
                                result = true;
                            }
                        }
                    }
                }
                else if (expression is Method_Call)
                {
                    var method_call = (Method_Call)expression;

                    for (int i = 0; i < method_call.args.Count; ++i)
                    {
                        var arg = method_call.args[i];
                        var end = arg.get_end();
                        if (end.type == Expression_Type.variable)
                        {
                            var variable = (Variable) end;
                            if (variable.symbol == symbol && method_call.minion.parameters[i].is_persistent)
                            {
                                result = true;
                            }
                        }
                    }
                }
            });

            return result;
        }

        static void analyze_constructor(Minion constructor)
        {
            Crawler.analyze_expressions(constructor.expressions, expression =>
            {
                if (expression.type == Expression_Type.assignment)
                {
                    var assignment = (Assignment)expression;
                    var target_end = assignment.target.get_end();
                    if (target_end.type == Expression_Type.portal)
                    {
                        var portal = ((Portal_Expression)target_end).portal;
                        if (assignment.expression.type == Expression_Type.variable
                            && Utility.is_pointer_or_shared(assignment.expression.get_profession()))
                        {
                            var variable = (Variable)assignment.expression;
                            var parameter = constructor.parameters.FirstOrDefault(p => p.symbol == variable.symbol);
                            if (parameter != null)
                            {
                                portal.is_owner = false;
                                parameter.symbol.profession = portal.profession =
                                    portal.profession.change_cpp_type(Cpp_Type.pointer);
                            }
                        }
                        else if (assignment.expression.type == Expression_Type.instantiate)
                        {
                            portal.is_owner = true;
                        }
                    }
                }
            });
        }
    }
}
