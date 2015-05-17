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

//            var insert = context.get_expression_pattern(token);
//            if (insert != null)
//                return append(insert, process_anything(summoner, patterns, context, step + 1));

            if (token == "this")
            {
                if (step != 0)
                    throw new Parser_Exception("One simply cannot have a this there.", pattern.position);

                return append(new Self(context.dungeon), process_idungeon(context.dungeon,
                    summoner, patterns, context, 1));
            }

            var symbol = context.scope.find_or_null(token);
            if (symbol != null)
            {
                return process_symbol(symbol, summoner, patterns, context, step);
                //                if (path_context.index == patterns.Count - 1 && symbol.profession.type == Kind.function)
                //                    return new Dynamic_Function_Call(symbol.name, null, args);

//                var next = new Variable(symbol);
//                var profession = next.get_profession();
//                var dungeon = profession != null
//                              ? profession.dungeon
//                              : next.get_profession().dungeon;
//
//                return append(next, process_idungeon(dungeon, summoner, patterns,
//                        context, step + 1));
            }

            Expression result;

            result = process_idungeon(context.dungeon,
                summoner, patterns, context, 0);

            if (result != null)
                return result;

            if (context.dungeon.realm != null)
            {
                result = process_idungeon(context.dungeon.realm, summoner, patterns, context, 0);
                if (result != null)
                    return result;
            }

            result = process_idungeon(context.dungeon.overlord.root, 
                summoner, patterns, context, 0);

            if (result != null)
                return result;

            throw new Parser_Exception("Unknown symbol: " + token, pattern.position);
        }

        static Expression process_idungeon(IDungeon idungeon, Summoner2 summoner, List<Legend> patterns,
            Summoner_Context context, int step)
        {
            if (idungeon == null || step >= patterns.Count)
                return null;

//            if (idungeon.GetType() == typeof (Dungeon))
                return process_dungeon((Dungeon)idungeon, summoner, patterns, context, step);
//            else
//                return process_treasury((Treasury)idungeon, summoner, patterns, context, step);
        }

        private static Expression process_dungeon(Dungeon dungeon, Summoner2 summoner,
            List<Legend> patterns, Summoner_Context context, int step)
        {
            var pattern = patterns[step];
            var token = pattern.children[0].text;
            if (dungeon.has_minion(token))
                return process_minion(dungeon.minions[token], summoner, patterns, context, step);

            if (dungeon.has_portal(token))
                return process_portal(dungeon.all_portals[token], summoner, patterns, context, step);

            if (dungeon.dungeons.ContainsKey(token))
                return process_profession(dungeon.dungeons[token], summoner, patterns, context, step);

            return null;
        }

        static Expression process_minion(Minion minion, Summoner2 summoner,
            List<Legend> patterns, Summoner_Context context, int step)
        {
            var pattern = patterns[step];
            List<Expression> args = pattern.children[1] == null
                ? null
                : pattern.children[1].children
                    .Select(p => summoner.process_expression(p, context))
                    .ToList();

            var result = new Method_Call(minion, null, args);
            if (step == patterns.Count - 1)
                return result;

            if (minion.return_type.dungeon == null)
            {
                if (minion.return_type == Professions.none)
                    throw new Exception("Spell " + minion.name 
                        + " does not return anything.");

                throw new Parser_Exception(minion.return_type
                    + " does not have a member named " + patterns[step + 1] + ".", pattern.position);
            }

            var child = process_dungeon((Dungeon)minion.return_type.dungeon,
                summoner, patterns, context, step + 1);

            if (child == null)
                throw new Parser_Exception("Dungeon " + minion.return_type.dungeon.name
                    + " does not have a member named " + patterns[step + 1].children[0].text + ".", pattern.position);

            return append(result, child);  
        }

        static Expression process_portal(Portal portal, Summoner2 summoner,
            List<Legend> patterns, Summoner_Context context, int step)
        {
            var pattern = patterns[step];
            List<Expression> args = pattern.children[1] == null
                ? null
                : pattern.children[1].children
                    .Select(p => summoner.process_expression(p, context))
                    .ToList();

            var result = new Portal_Expression(portal);
            if (step == patterns.Count - 1)
                return result;

            var child = process_dungeon((Dungeon)portal.other_dungeon,
                summoner, patterns, context, step + 1);

            if (child == null)
                throw new Parser_Exception("Dungeon " + portal.other_dungeon
                    + " does not have a member named " + patterns[step + 1].children[0].text + ".", pattern.position);

            return append(result, child);
        }

        static Expression process_symbol(Symbol symbol, Summoner2 summoner,
            List<Legend> patterns, Summoner_Context context, int step)
        {
            var result = new Variable(symbol);
            var profession = result.get_profession();
            var dungeon = profession != null
                          ? profession.dungeon
                          : result.get_profession().dungeon;

            if (step == patterns.Count - 1)
                return result;

            var child = process_dungeon((Dungeon)dungeon, summoner, patterns,
                    context, step + 1);

            if (child == null)
                throw new Exception("Dungeon " + symbol.profession.dungeon.name
                    + " does not have a member named " + patterns[step + 1].children[0].text + ".");

            return append(result, child);
        }

        static Expression process_profession(Dungeon dungeon, Summoner2 summoner,
            List<Legend> patterns, Summoner_Context context, int step)
        {
            if (step == patterns.Count - 1)
                return new Profession_Expression(summoner.overlord.library.get(dungeon));

            var child = process_dungeon(dungeon, summoner, patterns,
                    context, step + 1);

            if (child == null)
                throw new Exception("Dungeon " + dungeon.name
                    + " does not have a member named " + patterns[step + 1].children[0].text + ".");

            return child;
        }

        static Expression append(Expression first, Expression second)
        {
            if (first == null)
                return second;

            if (second == null)
                return first;

            first.next = second;
            return first;
        }

//        private static Expression process_treasury(Treasury treasury, Summoner2 summoner,
//            List<Legend> patterns, Summoner_Context context, int step)
//        {
//            var pattern = patterns[step];
//            var token = pattern.children[0].text;
//            if (step >= patterns.Count - 1)
//                throw new Exception("Enum " + token + " is missing a member value.");
//
//            var jewel_name = patterns.Last().children[0].text;
//            if (!treasury.jewels.Contains(jewel_name))
//                throw new Exception("Enum " + treasury.name
//                    + " does not contain member: " + jewel_name + ".");
//
//            return new Jewel(treasury, treasury.jewels.IndexOf(jewel_name));
//        }

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
