using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.schema;
using metahub.schema;
using runic.parser;

namespace imperative.summoner
{
    static class Tunneler
    {
        public static Expression process_anything(Summoner2 summoner, List<Legend> patterns,
            Summoner_Context context, int step = 0)
        {
            if (step >= patterns.Count)
                return null;

            var pattern = patterns[step];

            if (patterns.Count == 1 && pattern.text == "null")
                return new Null_Value();

//            Expression array_access = pattern.children[1] != null
//                     ? summoner.process_expression(pattern.children[1], context)
//                     : null;

            var token = pattern.children[0].text;

            var insert = context.get_expression_pattern(token);
            if (insert != null)
                return append(insert, process_anything(summoner, patterns, context, step + 1));

            if (token == "this")
            {
                if (step != 0)
                    throw new Parser_Exception("One simply cannot have a this there.", pattern.position);

                return append(new Self(context.dungeon), process_dungeon(context.dungeon,
                    summoner, patterns, context, 1));
            }

            var symbol = context.scope.find_or_null(token);
            if (symbol != null)
            {
                //                if (path_context.index == patterns.Count - 1 && symbol.profession.type == Kind.function)
                //                    return new Dynamic_Function_Call(symbol.name, null, args);

                var next = new Variable(symbol);
                var profession = next.get_profession();
                var dungeon = profession != null
                              ? profession.dungeon
                              : next.get_profession().dungeon;

                return append(next, process_dungeon(dungeon, summoner, patterns,
                        context, step + 1));
            }

            Expression result;

            result = process_dungeon(context.dungeon,
                summoner, patterns, context, step + 1);

            if (result != null)
                return result;

            result = process_realm(context.realm, summoner, patterns, context, step + 1);
            if (result != null)
                return result;

            result = process_realm(context.realm.overlord.root, summoner, patterns, context, step + 1);
            if (result != null)
                return result;

//            var func = context.dungeon == null || context.dungeon.GetType() == typeof(Dungeon)
//                ? process_function_call(token, path_context, args)
//                : null;
//
//            if (func != null)
//            {
//                if (func.type == Expression_Type.property_function_call && path_context.last.parent != null)
//                {
//                    //last.parent.child = null;
//                    var last2 = path_context.last.parent;
//                    path_context.last = path_context.last.parent;
//                    last2.next = null;
//                }
//                else
//                {
//                    path_context.is_finished = true;
//                }
//
//                return func;
//            }

            if (summoner.overlord.global_variables.ContainsKey(token))
            {
                symbol = summoner.overlord.global_variables[token];
                return append(new Variable(symbol), process_dungeon(symbol.profession.dungeon,
                summoner, patterns, context, step + 1));
            }

            throw new Parser_Exception("Unknown symbol: " + token, pattern.position);
        }

        static Expression process_realm(Realm realm, Summoner2 summoner, List<Legend> patterns,
            Summoner_Context context, int step)
        {
            if (step >= patterns.Count)
                return null;

            var pattern = patterns[step];
            var token = pattern.children[0].text;
            var dungeon = realm.get_dungeon(token);

            if (dungeon != null)
            {
                return process_dungeon(dungeon, summoner, patterns, context, step + 1);
            }

            var child_realm = realm.get_child_realm(token, false);
            if (child_realm != null)
            {
                return process_realm(child_realm, summoner, patterns, context, step + 1);
            }

            return null;
        }

        static Expression process_dungeon(IDungeon idungeon, Summoner2 summoner, List<Legend> patterns,
            Summoner_Context context, int step)
        {
            if (idungeon == null || step >= patterns.Count)
                return null;

            var pattern = patterns[step];
            var token = pattern.children[0].text;

            if (idungeon.GetType() == typeof (Dungeon))
            {
                var dungeon = (Dungeon) idungeon;
                if (dungeon.has_minion(token))
                {
                    List<Expression> args = pattern.children[1] == null
                        ? null
                        : pattern.children[1].children
                            .Select(p => summoner.process_expression(p, context))
                            .ToList();

                    var minion = dungeon.minions[token];
                    return append(new Method_Call(minion, null, args), process_dungeon(minion.return_type.dungeon,
                        summoner, patterns, context, step + 1));
                }

                if (dungeon.has_portal(token))
                {
                    var portal = dungeon.all_portals[token];
                    return append(new Portal_Expression(portal), process_dungeon(portal.other_dungeon,
                        summoner, patterns, context, step + 1));
                }
            }
            else
            {
                    if (step >= patterns.Count - 1)
                        throw new Exception("Enum " + token + " is missing a member value.");

                    var treasury = (Treasury)idungeon;
                    var jewel_name = patterns.Last().children[0].text;
                if (!treasury.jewels.Contains(jewel_name))
                    throw new Exception("Enum " + treasury.name 
                        + " does not contain member: " + jewel_name + ".");

                    return new Jewel(treasury, treasury.jewels.IndexOf(jewel_name));
            }

            return new Profession_Expression(new Profession(Kind.reference, idungeon));
//            return null;
        }

        static Expression append(Expression first, Expression second)
        {
            if (first == null)
                return second;

            if (second == null)
                return first;

            first.next = second;
            return second;
        }

        private static Expression process_function_call(Expression expression, Expression previous,
            string token, Dungeon dungeon, List<Expression> args)
        {
            var minion = dungeon != null
                             ? dungeon.summon_minion(token, true)
                             : null;

            if (minion != null)
                return new Method_Call(minion, expression, args);

            if (Minion.platform_specific_functions.Contains(token))
            {
                if (token == "add" || token == "setter")
                    return new Property_Function_Call(Property_Function_Type.set,
                        ((Portal_Expression)previous).portal, args);

                return new Platform_Function(token, expression, args);
            }

            return null;
        }

    }
}
