using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using imperative.wizard;
using imperative.Properties;
using imperative.schema;
using imperative.expressions;
using imperative.legion;
using metahub.jackolantern.expressions;
using metahub.render;
using metahub.schema;
using runic.lexer;
using runic.parser;
using runic.parser.rhymes;
using Expression = imperative.expressions.Expression;
using Parser = runic.parser.Parser;

namespace imperative.summoner
{
    public static class Legend_Types
    {
        public const string dungeon_definition = "dungeon_definition";
    }

    public class Summoner
    {
        public Overlord overlord;
        public static Lexer lexer;
        public static Parser parser;
        private bool is_external;

        public Summoner(Overlord overlord, bool is_external = false)
        {
            this.overlord = overlord;
            this.is_external = is_external;
        }

        public void summon_file(string path, Project project, bool is_external = false)
        {
            var was_external = this.is_external;
            this.is_external = is_external;

            var code = File.ReadAllText(path);
            var legend = overlord.summon_legend(code, path);
            summon_many(new[] { legend }, project);

            this.is_external = was_external;
        }

        public void gather_parts(Summoner_Context parent, List<Legend> parts, Dictionary<string, List<Summoner_Context>> map)
        {
            foreach (var child in parts)
            {
                var list = map.ContainsKey(child.type)
                    ? map[child.type]
                    : map[child.type] = new List<Summoner_Context>();

                if (child.type == Legend_Types.dungeon_definition)
                {
                    var name = child.children[1].children.Select(c => c.text).join(".");
                    var child_context = parent.children.ContainsKey(name)
                        ? parent.children[name]
                        : parent.children[name] = new Summoner_Context(child, parent);

                    list.Add(child_context);
                    gather_parts(child_context, child.children[4].children, map);
                }
                else
                {
                    list.Add(new Summoner_Context(child, parent));
                }
            }
        }

        public void summon_many(IEnumerable<Legend> sources, Project project = null)
        {
            var context = new Summoner_Context(null, project, overlord.root);
            var map = new Dictionary<string, List<Summoner_Context>>();
            //            var contexts = new Summoner_Context[sources.Count()];
            map[Legend_Types.dungeon_definition] = new List<Summoner_Context>
            {
                context
            };

            foreach (var source in sources)
            {
                context.legends.Add(source);
                gather_parts(context, source.children, map);
            }

            ack2(context);
            //            foreach (var dungeon_context in dungeon_legends)
            //            {
            //                foreach (var legend in dungeon_context.legends)
            //                {
            //                    process_dungeon1(dungeon_context, legend);
            //                }
            //            }

            var dungeon_legends = map[Legend_Types.dungeon_definition];
            foreach (var dungeon_context in dungeon_legends)
            {
                foreach (var legend in dungeon_context.legends)
                {
                    process_dungeon2(dungeon_context, legend);
                }
            }

            foreach (var dungeon_context in dungeon_legends)
            {
                foreach (var legend in dungeon_context.legends)
                {
                    process_dungeon3(dungeon_context, legend);
                }
            }
        }

        void ack2(Summoner_Context context)
        {
            foreach (var dungeon_context in context.children.Values)
            {
                foreach (var legend in dungeon_context.legends)
                {
                    process_dungeon1(dungeon_context, legend);
                    ack2(dungeon_context);
                }
            }
        }

        private void ack(Group_Legend source, Summoner_Context context, int step,
                         Func<Legend, Summoner_Context, Dungeon> second)
        {
            foreach (var pattern in source.children)
            {
                var parts = pattern.children;
                switch (pattern.type)
                {
                    case "import_statement":
                        if (step != 1)
                            break;

                        var tokens = pattern.children[0].children.Select(p => p.text);
                        context.imported_realms.Add(overlord.root.get_realm(tokens));
                        break;

                    case "include_statement":
                        if (step != 0)
                            break;

                        summon_file(pattern.children[1].text, context.project, pattern.children[0] != null);
                        break;
                    //
                    //                    case "namespace_statement":
                    //
                    //                        var context2 = create_realm_context(pattern, context);
                    //                        var statements = pattern.children[1].children;
                    //                        process_namespace(statements, context2, second);
                    //                        break;

                    case "class_definition":
                        second(pattern, context);
                        break;

                    case "external_var":
                        if (step == 1)
                        {
                            var profession = parts[1] != null
                                ? parse_type2(parts[1], context)
                                : Professions.any;

                            var portal = new Portal(parts[0].text, profession, overlord.root);
                            portal.enchantments.Add(new Enchantment("static"));
                            overlord.root.add_portal(portal);
                        }
                        break;

                    case "function_definition":
                        if (step == 2)
                            process_function_definition(pattern, context, false, true);

                        break;

                    default:
                        throw new Exception("Not supported.");
                }
            }
        }

        public Dungeon process_dungeon1(Summoner_Context context, Legend legend)
        {
            if (context.dungeon != null)
                return context.dungeon;

            var parts = legend.children;

            var parent_dungeon = context.parent.dungeon;

            string name;
            var tokens = parts[1].children;
            if (tokens.Count == 1)
            {
                name = tokens[0].text;
                var replacement_name = context.get_string_pattern(name);
                if (replacement_name != null)
                    name = replacement_name;
            }
            else
            {
                for (var i = 0; i < tokens.Count - 1; ++i)
                {
                    var token = tokens[i].text;
                    if (!parent_dungeon.dungeons.ContainsKey(token))
                    {
                        parent_dungeon = parent_dungeon.create_dungeon(token);
                        context.add_dungeon_to_project(parent_dungeon);
                    }
                    else
                    {
                        parent_dungeon =  parent_dungeon.dungeons[token];
                    }
                }

                name = tokens[tokens.Count - 1].text;
            }

            if (!parent_dungeon.dungeons.ContainsKey(name))
            {
                var dungeon = context.dungeon = parent_dungeon.create_dungeon(name);
                context.add_dungeon_to_project(dungeon);
                //                if (parts[1].text == "struct")
                //                    dungeon.is_value = true;

                if (parts[0] != null)
                {
                    var attributes = parts[0].children;
                    dungeon.is_external = is_external || attributes.Any(p => p.text == "external");
                    dungeon.is_abstract = attributes.Any(p => p.text == "abstract");
                    dungeon.is_dynamic = attributes.Any(p => p.text == "dynamic");
                    //                    dungeon.is_value = attributes.Any(p => p.text == "value");
                }

                if (parts[2] != null)
                {
                    foreach (var type_name in parts[2].children)
                    {
                        var generic_dungeon = new Dungeon(type_name.text, null, null);
                        var profession = new Profession(generic_dungeon);
                        context.set_pattern(type_name.text, profession);
                        dungeon.generic_parameters[type_name.text] = profession;
                    }
                }

                //                dungeon.generate_code();
                context.dungeon = dungeon;
                return dungeon;
            }

            return null;
        }

        public Dungeon process_dungeon2(Summoner_Context context, Legend legend)
        {
            var source = legend;
            var dungeon = context.dungeon;
            var statements = dungeon.name == ""
                ? source.children
                : source.children[4].children;


            if (source.children.Count > 3 && source.children[3] != null)
            {
                var profession = parse_type2(source.children[3].children[0], context);
//                var parent = (Dungeon)get_dungeon(context.dungeon, source.children[3].children);
                var parent = (Dungeon)profession.dungeon;
                dungeon.parent = parent;
                parent.children.Add(dungeon);
            }

            foreach (var statement in statements)
            {
                process_dungeon_statement(statement, context, true);
            }

            return dungeon;
        }

        public Dungeon process_dungeon3(Summoner_Context context, Legend legend)
        {
            var source = legend;
            var dungeon = context.dungeon;
            var statements = dungeon.name == ""
                ? source.children
                : source.children[4].children;

            foreach (var statement in statements)
            {
                process_dungeon_statement(statement, context);
            }

            return dungeon;
        }

        private void process_namespace(IEnumerable<Legend> statements, Summoner_Context context,
                                       Func<Legend, Summoner_Context, Dungeon> dungeon_step)
        {
            foreach (var statement in statements)
            {
                if (statement.type == "class_definition")
                    dungeon_step(statement, context);
                //                else if (!context.dungeon.treasuries.ContainsKey(statement.children[0].text))
                //                    summon_enum(statement.children, context);
            }
        }

        public IDungeon get_dungeon(Dungeon dungeon, List<Legend> path)
        {
            try
            {
                return dungeon.get_dungeon(path.Select(p => p.text));
            }
            catch (Exception exception)
            {
                throw new Parser_Exception(exception.Message, path.First().position);
            }
        }

        private Summoner_Context create_realm_context(Legend source, Summoner_Context parent_context)
        {
            //            var name = source.children[0].text;
            //            if (!overlord.root.children.ContainsKey(name))
            //            {
            //                overlord.root.children[name] = new Realm(name, overlord);
            //            }
            //            var realm = overlord.root.children[name];
            var context = new Summoner_Context(parent_context);
            context.dungeon = overlord.root.get_or_create_realm(source.children[0].children.Select(p => p.text));

            return context;
        }

        private void process_dungeon_statement(Legend source, Summoner_Context context, bool as_stub = false)
        {
            switch (source.type)
            {
                //                case "abstract_function":
                //                    if (as_stub)
                //                        process_abstract_function(source, context);
                //                    break;

                case "function_definition":
                    process_function_definition(source, context, as_stub);
                    break;

                case "property_declaration":
                    process_property_declaration(source, context, as_stub);
                    break;
            }
        }

        private void process_function_definition(Legend source, Summoner_Context original_context, bool as_stub = false, bool simple = false)
        {
            var parts = source.children;
            var name = parts[1].text;
            var context = original_context;

            if (parts[2] != null)
            {
                context = new Summoner_Context(source, context) { dungeon = original_context.dungeon };
                foreach (var type_name in parts[2].children)
                {
                    var generic_dungeon = new Dungeon(type_name.text, null, null);
                    context.set_pattern(type_name.text, new Profession(generic_dungeon));
                }
            }

            var minion = context.dungeon.has_minion(name)
                ? context.dungeon.summon_minion(name)
                : simple
                ? context.dungeon.spawn_simple_minion(name,
                    parts[3].children.Select(p => process_parameter(p, context)).ToList())
                    : context.dungeon.spawn_minion(name,
                    parts[3].children.Select(p => process_parameter(p, context)).ToList());

            var new_context = new Summoner_Context(context) { scope = minion.scope };

            if (parts[0] != null)
            {
                foreach (var enchantment in parts[0].children)
                {
                    minion.add_enchantment(new Enchantment(enchantment.text));
                }
            }

            var return_type = parts[4];
            if (return_type != null)
                minion.return_type = parse_type2(return_type, context);

            if (as_stub)
            {

            }
            else
            {
                if (parts[5] == null)
                {
                    minion.add_enchantment(new Enchantment("abstract"));
                }
                else
                    minion.add_to_block(process_block(parts[5], new_context));
            }
        }

        private void process_property_declaration(Legend source, Summoner_Context context, bool as_stub = false)
        {
            if (!as_stub)
                return;

            var parts = source.children;

            var portal_name = parts[1].text;
            if (!context.dungeon.has_portal(portal_name))
            {
                var type_info = parts[2] != null
                    ? parse_type2(parts[2], context)
                    : Professions.unknown;

                var portal = new Portal(portal_name, type_info, context.dungeon);
                if (parts[0] != null)
                {
                    foreach (var enchantment in parts[0].children)
                    {
                        portal.enchant(new Enchantment(enchantment.text));
                    }
                }

                if (parts[3] != null)
                    portal.default_expression = process_expression(parts[3], context);

                context.dungeon.add_portal(portal);
            }
            else if (parts[3] != null)
            {
                var base_portal = context.dungeon.all_portals[portal_name];
                if (base_portal.dungeon != context.dungeon)
                {
                    var portal = context.dungeon.all_portals[portal_name] = new Portal(base_portal, context.dungeon);
                    portal.default_expression = process_expression(parts[3], context);
                }
            }
        }

        private List<Expression> process_block(Legend source, Summoner_Context context)
        {
            //            if (source.rhyme.type_rhyme.type != "statement" && source.type != "statement" && source.type != "long_block")
            //                return new List<Expression> { summon_statement(source, context) };

            return summon_statements(source.children, context);
        }

        public List<Expression> summon_statements(List<Legend> legends, Summoner_Context context)
        {
            var result = new List<Expression>();
            foreach (var pattern in legends)
            {
                var expression = summon_statement(pattern, context);
                if (expression == null)
                    continue;

                if (expression.type == Expression_Type.statements)
                {
                    var statements = (Block)expression;
                    result.AddRange(statements.body);
                }
                else
                {
                    result.Add(expression);
                }
            }

            return result;
        }

        public Expression summon_statement(Legend source, Summoner_Context context)
        {
            var parts = source.children;

            switch (source.type)
            {
                case "assignment":
                    return process_assignment(source, context);

                case "expression_part":
                    return process_expression(source, context);

                case "if_chain":
                    return summon_if_chain(source, context);

                case "import_statement":
                    process_import(parts, context);
                    return null;

                case "if_statement":
                    return new Flow_Control(Flow_Control_Type.If,
                                            process_expression(parts[0], context),
                                            process_block(parts[1], context)
                        ) { legend = source };

                case "while_statement":
                    return new Flow_Control(Flow_Control_Type.While,
                                            process_expression(parts[0], context),
                                            process_block(parts[1], context)
                        ) { legend = source };

                case "foreach_statement":
                    return process_iterator(source, context);

                case "return_statement":
                    return new Statement("return", parts[0] == null
                                                       ? null
                                                       : process_expression(parts[0], context)
                        , source);

                case "throw_statement":
                    return new Statement("throw", process_expression(parts[0], context), source);

                case "declare_variable":
                    return process_declare_variable(source, context);

                case "snippet_function":
                    {
                        return process_function_snippet(parts, context);
                    }

                case "snippets":
                    var snippets = parts.Select(p => summon_statement(p, context)).ToList();
                    return new Block(snippets);

                case "statement":
                    return summon_statement(source.children[0], context);

                case "expression":
                    return process_expression(source, context);

                case "statements":
                    {
                        var expressions = process_block(source, context);
                        return expressions.Count == 1
                                   ? expressions[0]
                                   : new Block(expressions);
                    }

                //                case "enum_definition":
                //                    return summon_enum(parts, context);

                case "preprocessor":
                    return process_preprocessor(parts, context);
            }

            throw new Exception("Unsupported statement type: " + source.type + ".");
        }

        private static string[][] operator_precedence =
        {
            new [] {"/", "*"},
            new [] {"+", "-"},
            new [] {"!=", "==", "<", "<=", ">", ">="},
            new [] {"&&"},
            new [] {"||"}
        };

        public Expression process_expression(Legend legend, Summoner_Context context)
        {
            var group = (Group_Legend)legend;
            var children = group.children;
            var dividers = group.dividers;

            if (children.Count == 1)
                return process_expression_part(children[0], context);

            if (children.Count == 2 || same_text(dividers))
            {
                var op = context.get_string_pattern(group.dividers[0].text) ?? dividers[0].text;
                return new Operation(op, children.Select(p => process_expression_part(p, context)));
            }

            var last_count = dividers.Count;

            while (dividers.Count > 1)
            {
                foreach (var op in operator_precedence)
                {
                    group_operators(op, children, dividers);
                    if (dividers.Count == 1 || same_text(dividers))
                        break;
                }

                if (last_count == dividers.Count)
                    throw new Exception("Dividers not fully supported.");

                last_count = dividers.Count;
            }

            {
                var op = context.get_string_pattern(group.dividers[0].text) ?? dividers[0].text;
                return new Operation(op, children.Select(p => process_expression_part(p, context)));
            }
        }

        public static bool same_text(List<Legend> dividers)
        {
            return dividers.Skip(1).All(d => d.text == dividers[0].text);
        }

        public static void group_operators(string[] operators, List<Legend> patterns, List<Legend> dividers)
        {
            var i = 0;
            for (var d = 0; d < dividers.Count; ++d)
            {
                var divider = dividers[d];
                if (operators.Contains(divider.text))
                {
                    var rhyme = new Repetition_Rhyme("expression");
                    patterns[i] = new Group_Legend(rhyme,
                        new List<Legend>
                        {
                            patterns[i],
                            patterns[i + 1]
                        }, patterns[i].position,
                        new List<Legend> { divider });

                    patterns.RemoveAt(i + 1);
                    dividers.RemoveAt(d);
                    --d;
                }
                else
                {
                    ++i;
                }
            }
        }

        private Expression process_declare_variable(Legend legend, Summoner_Context context)
        {
            var parts = legend.children;

            Profession profession;
            var expression_pattern = parts[2] == null
                                         ? null
                                         : process_expression(parts[2], context);

            if (parts[1] != null)
            {
                profession = parse_type2(parts[1], context);
            }
            else
            {
                if (expression_pattern == null)
                    throw new Exception("Cannot discern variable type.");

                profession = expression_pattern.get_end().get_profession();
            }

            var symbol = context.scope.create_symbol(parts[0].text, profession);
            return new Declare_Variable(symbol, expression_pattern, legend);
        }

        private Expression process_expression_part(Legend source, Summoner_Context context)
        {
            var parts = source.children;

            switch (source.type)
            {
                case "bool_value":
                    return new Literal(source.text == "true");

                case "int_value":
                    return new Literal(int.Parse(source.text));

                case "float_value":
                    return new Literal(float.Parse(source.text));

                case "string_value":
                    return new Literal(source.text);

                case "null":
                    return new Null_Value();

                case "empty_array":
                    return new Instantiate(overlord.library.get(Professions.List, Professions.unknown));

                case "reference":
                    return process_reference(source, context);

                case "expression":
                    return process_expression(source, context);

                case "instantiate":
                    return process_instantiate(parts, context);

                case "lambda":
                    return process_lambda(parts, context);

                case "instantiate_array":
                    return instantiate_array(parts, context);

                case "dictionary":
                    return summon_dictionary(parts, context);
            }

            throw new Exception("Unsupported statement type: " + source.type + ".");
        }

        class Path_Context
        {
            public IDungeon dungeon;
            public Dungeon realm;
            public int index = -1;
            public Expression result = null;
            public Expression last = null;
            public bool is_finished = false;
        }

        private Expression process_reference(Legend source, Summoner_Context context)
        {
            //            return Summoning.Tunnel.process_anything(this, source.children, context, 0);
            return Tunneler.process_anything(this, source.children, context);

        }

        private Parameter process_parameter(Legend source, Summoner_Context context)
        {
            var type = source.children[1] != null
                           ? parse_type2(source.children[1], context)
                           : Professions.unknown;
            Expression default_value = null;
            if (source.children[2] != null)
                default_value = process_expression(source.children[2], context);

            return new Parameter(new Symbol(source.children[0].text, type, null), default_value);
        }

        private Profession parse_type2(Legend source, Summoner_Context context)
        {
            var path = source.children[1].children.Select(p => p.text).ToArray();
            var result = parse_type2(path, context, source);
            if (source.children.Count > 3 && source.children[3] != null && result.dungeon != Professions.List)
            {
                result = context.dungeon.overlord.library.get(Professions.List, result.dungeon);
            }
            //            if (source.children[0] != null)
            //                result.is_const = true;

            if (source.children[2] != null)
            {

                result.get_deepest_child().children = source.children[2].children.Select(c => parse_type2(c, context)).ToList();
            }
            if (result.is_array(overlord) && result.children.Count == 0)
                throw new Parser_Exception("Missing generic parameters.", source.position);

            return result;
        }

        private Profession parse_type2(string[] path, Summoner_Context context, Legend source)
        {
            var text = path.Last();
            if (path.Length == 1)
            {
                switch (text)
                {
                    case "bool":
                        return Professions.Bool;
                    case "string":
                        return Professions.String;
                    case "float":
                        return Professions.Float;
                    case "int":
                        return Professions.Int;
                    case "any":
                        return Professions.any;
                }

                var profession = context.get_profession_pattern(text);
                if (profession != null)
                    return profession;
            }

            var dungeon = context.get_dungeon(path);
            if (dungeon != null)
                return overlord.library.get(dungeon);

            throw new Parser_Exception("Invalid type: " + text + ".", source.position);
        }

        private Expression process_assignment(Legend legend, Summoner_Context context)
        {
            var parts = legend.children;
            var reference = process_reference(parts[0], context);
            var expression = process_expression(parts[2], context);
            var op = parts[1].text;
            var last = reference.get_end();
            if (reference != null && reference.type == Expression_Type.operation)
                throw new Exception("Cannot call function on operation.");

            return new Assignment(
                reference,
                op,
                expression
                )
            {
                legend = legend
            };
        }

        public Expression process_iterator(Legend legend, Summoner_Context context)
        {
            var parts = legend.children;
            var reference = process_expression_part(parts[1], context);
            //            throw new Exception("Not implemented.");

            var profession = reference.get_end().get_profession().children[0];
            var symbol = context.scope.create_symbol(parts[0].text, profession);
            context.scope.add_map(symbol.name, c => new Variable(symbol));
            return new Iterator(symbol,
                reference, process_block(parts[2], context)
            ) { legend = legend };
        }

        public Expression summon_if_chain(Legend legend, Summoner_Context context)
        {
            var parts = legend.children;
            var ifs = parts[0].children.Select(e => (Flow_Control)summon_statement(e, context)).ToList();
            //            var expressions = summon_statements(parts[0].children, context).ToList();
            var result = new If(ifs);
            if (parts[1] != null)
            {
                result.if_statements.Add(new Flow_Control(Flow_Control_Type.Else, null,
                    summon_statements(parts[1].children, context)));
            }

            return result;
        }

        private Expression process_instantiate(List<Legend> parts, Summoner_Context context)
        {
            var type = parse_type2(parts[0], context);
            var args = parts[1].children.Select(p => process_expression(p, context));
            return new Instantiate(type, args);
        }

        //        private Expression summon_enum(List<Legend> parts, Summoner_Context context)
        //        {
        //            var items = new List<string>();
        //            foreach (var item in parts[1].children)
        //            {
        //                int? value = null;
        //                if (item.children[1] != null)
        //                    value = int.Parse(item.children[1].text);
        //
        //                items.Add(item.children[0].text);
        //            }
        //            var treasury = context.dungeon.create_treasury(parts[0].text, items);
        //            return new Treasury_Definition(treasury);
        //        }

        private Expression process_function_snippet(List<Legend> parts, Summoner_Context context)
        {
            var name = parts[1].text;
            var body = parts[4];
            var parameters = parts[2].children.Select(p => p.text).ToArray();
            return new Snippet(name, body, parameters);
        }

        private Expression process_lambda(List<Legend> parts, Summoner_Context context)
        {
            var parameters = parts[0].children.Select(p => process_parameter(p, context));
            var minion = new Ethereal_Minion(parameters, context.scope);
            var new_context = new Summoner_Context(context) { scope = minion.scope };
            var block = process_block(parts[1], new_context);
            minion.expressions.AddRange(block);
            minion.return_type = Professions.none;

            return new Anonymous_Function(minion);
        }

        private Expression process_preprocessor(List<Legend> parts, Summoner_Context context)
        {
            var condition = process_expression(parts[1], context);
            if (!(bool)Wizard.resolve_expression(condition, context))
                return null;

            return new Block(process_block(parts[2], context));
        }

        private Expression instantiate_array(List<Legend> parts, Summoner_Context context)
        {
            return new Instantiate(overlord.library.get(Professions.List, Professions.unknown),
                parts[0].children.Select(p => process_expression(p, context)));
        }

        private Expression summon_dictionary(List<Legend> parts, Summoner_Context context)
        {
            var dictionary = new Dictionary<string, Expression>();
            foreach (var child in parts[0].children)
            {
                dictionary[child.children[0].text] = process_expression(child.children[1], context);
            }

            return new Create_Dictionary(overlord.library, dictionary);
        }

        public static List<Rune> read_runes(string input, string filename)
        {
            if (lexer == null)
                lexer = new Lexer(Resources.imp_lexer);

            return lexer.read(input, filename);
        }

        public static Legend translate_runes(string source, List<Rune> runes, string start = "start")
        {
            if (parser == null)
                parser = new Parser(lexer, Resources.imp_grammar);

            return parser.read(source, runes, start);
        }

        public void process_import(List<Legend> parts, Summoner_Context context)
        {


        }
    }
}
