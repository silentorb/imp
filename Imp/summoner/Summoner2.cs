using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using imperative.wizard;
using imperative.Properties;
using imperative.schema;
using imperative.expressions;
using metahub.jackolantern.expressions;

using metahub.schema;
using runic.lexer;
using runic.parser;
using Expression = imperative.expressions.Expression;
using Parser = runic.parser.Parser;

namespace imperative.summoner
{
    public class Summoner2
    {
        public Overlord overlord;
        private static Lexer lexer;
        private static Parser parser;

        public Summoner2(Overlord overlord)
        {
            this.overlord = overlord;
        }

        public void summon(Legend source)
        {
            summon((Group_Legend)source);
        }

        public void summon(Group_Legend source)
        {
            var context = new Summoner_Context(overlord.root);
            ack(source, context, 0, process_dungeon1);
            ack(source, context, 1, process_dungeon2);
            ack(source, context, 2, process_dungeon3);
        }

        public void summon_many(IEnumerable<Legend> sources)
        {
            var contexts = new Summoner_Context[sources.Count()];
            var i = 0;
            foreach (Group_Legend source in sources)
            {
                var context = contexts[i++] = new Summoner_Context(overlord.root);
                ack(source, context, 0, process_dungeon1);
            }

            i = 0;
            foreach (Group_Legend source in sources)
            {
                ack(source, contexts[i++], 1, process_dungeon2);
            }

            i = 0;
            foreach (Group_Legend source in sources)
            {
                ack(source, contexts[i++], 2, process_dungeon3);
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

                    case "namespace_statement":

                        var context2 = create_realm_context(pattern, context);
                        var statements = pattern.children[1].children;
                        process_namespace(statements, context2, second);
                        break;

                    case "class_definition":
                        second(pattern, context);
                        break;

                    case "external_var":
                        if (step == 1)
                        {
                            var profession = parts[1] != null
                                ? parse_type2(parts[1], context)
                                : new Profession(Kind.reference);

                            var symbol = new Symbol(parts[0].text, profession, null);
                            overlord.global_variables[symbol.name] = symbol;
                        }
                        break;

                    default:
                        throw new Exception("Not supported.");
                }
            }
        }

        public Dungeon process_dungeon1(Legend source, Summoner_Context context)
        {
            var parts = source.children;
            var name = source.children[2].text;
            var replacement_name = context.get_string_pattern(name);
            if (replacement_name != null)
                name = replacement_name;

            if (!context.realm.dungeons.ContainsKey(name))
            {
                var dungeon = context.realm.create_dungeon(name);
                if (parts[1].text == "struct")
                    dungeon.is_value = true;

                if (parts[0] != null)
                {
                    var attributes = parts[0].children;
                    dungeon.is_external = attributes.Any(p => p.text == "external");
                    dungeon.is_abstract = attributes.Any(p => p.text == "abstract");
                    //                    dungeon.is_value = attributes.Any(p => p.text == "value");
                }

                var parent_dungeons = source.children[3].children;
                if (parent_dungeons.Count > 0)
                    dungeon.parent = (Dungeon)get_dungeon(parent_dungeons[0].children);

                dungeon.generate_code();
                return dungeon;
            }

            return null;
        }

        public Dungeon process_dungeon2(Legend source, Summoner_Context context)
        {
            var name = source.children[2].text;

            var replacement_name = context.get_string_pattern(name);
            if (replacement_name != null)
                name = replacement_name;

            var statements = source.children[4].children;
            var dungeon = context.realm.dungeons[name];
            var dungeon_context = new Summoner_Context(context) { dungeon = dungeon };
            foreach (var statement in statements)
            {
                process_dungeon_statement(statement, dungeon_context, true);
            }

            return dungeon;
        }

        public Dungeon process_dungeon3(Legend source, Summoner_Context context)
        {
            var name = source.children[2].text;

            var replacement_name = context.get_string_pattern(name);
            if (replacement_name != null)
                name = replacement_name;

            var statements = source.children[4].children;
            var dungeon = context.realm.dungeons[name];
            var dungeon_context = new Summoner_Context(context) { dungeon = dungeon };
            foreach (var statement in statements)
            {
                process_dungeon_statement(statement, dungeon_context);
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
                else if (!context.realm.treasuries.ContainsKey(statement.children[0].text))
                    summon_enum(statement.children, context);
            }
        }

        public IDungeon get_dungeon(List<Legend> path)
        {
            return overlord.root.get_dungeon(path.Select(p => p.text));
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
            context.realm = overlord.root.get_or_create_realm(source.children[0].children.Select(p => p.text));

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

        private void process_function_definition(Legend source, Summoner_Context context, bool as_stub = false)
        {
            var parts = source.children;
            var name = parts[1].text;
            var minion = context.dungeon.has_minion(name)
                             ? context.dungeon.summon_minion(name)
                             : context.dungeon.spawn_minion(
                                 name,
                                 parts[2].children.Select(p => process_parameter(p, context)).ToList()
                                   );

            var new_context = new Summoner_Context(context) { scope = minion.scope };

            if (parts[0] != null)
            {
                foreach (var enchantment in parts[0].children)
                {
                    minion.add_enchantment(new Enchantment(enchantment.text));
                }
            }

            if (as_stub)
            {
                var return_type = parts[3];
                if (return_type != null)
                    minion.return_type = parse_type2(return_type, context);
            }
            else
            {
                if (parts[4] == null)
                {
                    minion.add_enchantment(new Enchantment("abstract"));
                }
                else
                    minion.add_to_block(process_block(parts[4], new_context));
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
                var type_info = parse_type2(parts[2], context);
                var portal = new Portal(portal_name, type_info);
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
                    return process_assignment(parts, context);

                case "expression_part":
                    return process_expression(source, context);

                case "if_chain":
                    return summon_if_chain(parts, context);

                case "import_statement":
                    process_import(parts, context);
                    return null;

                case "if_statement":
                    return new Flow_Control(Flow_Control_Type.If,
                                            process_expression(parts[0], context),
                                            process_block(parts[1], context)
                        );

                case "while_statement":
                    return new Flow_Control(Flow_Control_Type.While,
                                            process_expression(parts[0], context),
                                            process_block(parts[1], context)
                        );

                case "for_statement":
                    return process_iterator(parts, context);

                case "return_statement":
                    return new Statement("return", parts[0] == null
                                                       ? null
                                                       : process_expression(parts[0], context)
                        );

                case "declare_variable":
                    return process_declare_variable(parts, context);

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

                case "enum_definition":
                    return summon_enum(parts, context);

                case "preprocessor":
                    return process_preprocessor(parts, context);
            }

            throw new Exception("Unsupported statement type: " + source.type + ".");
        }

        public Expression process_expression(Legend legend, Summoner_Context context)
        {
            var group = (Group_Legend)legend;
            var children = group.children;

            if (children.Count == 1)
                return process_expression_part(children[0], context);

            if (children.Count == 2)
            {
                var op = context.get_string_pattern(group.dividers[0].text) ?? group.dividers[0].text;
                return new Operation(op, children.Select(p => process_expression_part(p, context)));
            }

            throw new Exception("Not supported.");
        }

        private Expression process_declare_variable(List<Legend> parts, Summoner_Context context)
        {
            Profession profession;
            var expression_pattern = parts[2].children.Count == 0
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

                profession = expression_pattern.get_profession();
            }

            var symbol = context.scope.create_symbol(parts[0].text, profession);
            return new Declare_Variable(symbol, expression_pattern);
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
                    return new Instantiate(new Profession(Kind.reference, null));

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
            public Realm realm;
            public int index = -1;
            public Expression result = null;
            public Expression last = null;
            public bool is_finished = false;
        }

        private Expression process_reference(Legend source, Summoner_Context context)
        {
            return Pathfinder.process_anything(this, source.children[0].children, context);

            var path_context = new Path_Context()
            {
                dungeon = (IDungeon)context.dungeon,
                realm = context.realm
            };
            var patterns = source.children[0].children;
            if (patterns.Count == 1)
            {
                if (patterns[0].text == "null")
                    return new Null_Value();
            }
            List<Expression> args = null;

            if (source.children[1] != null)
            {
                args = source.children[1].children
                                            .Select(p => process_expression(p, context))
                                            .ToList();
            }

            foreach (var pattern in patterns)
            {
                ++path_context.index;
                var token = pattern.children[0].text;

                var next = process_token(token, pattern, patterns, args, path_context, context);
                if (next.type == Expression_Type.empty)
                    continue;

                if (path_context.is_finished)
                    return next;

                if (path_context.result == null)
                    path_context.result = next;
                else
                {
                    if (path_context.last.type == Expression_Type.property_function_call)
                        ((Property_Function_Call)path_context.last).args.Add(next);
                    else
                        path_context.last.next = next;
                }
                path_context.last = next.get_end();
            }

            return path_context.result;
        }

        Expression process_token(string token, Legend pattern, List<Legend> patterns, List<Expression> args,
            Path_Context path_context, Summoner_Context context)
        {
            Expression array_access = pattern.children[1] != null
                     ? process_expression(pattern.children[1], context)
                     : null;

            var insert = context.get_expression_pattern(token);
            if (insert != null)
                return insert;

            if (token == "this")
            {
                return new Self((Dungeon)path_context.dungeon);
            }

            var symbol = context.scope.find_or_null(token);
            if (symbol != null)
            {
                if (path_context.index == patterns.Count - 1 && symbol.profession.type == Kind.function)
                    return new Dynamic_Function_Call(symbol.name, null, args);

                var next = new Variable(symbol) { index = array_access };
                var profession = next.get_profession();
                path_context.dungeon = profession != null
                              ? profession.dungeon
                              : next.get_profession().dungeon;

                return next;
            }

            if (path_context.dungeon != null)
            {
                if (path_context.dungeon.GetType() == typeof(Dungeon))
                {
                    var portal = ((Dungeon)path_context.dungeon).get_portal_or_null(token);
                    if (portal != null)
                    {
                        path_context.dungeon = portal.other_dungeon;
                        return new Portal_Expression(portal) { index = array_access };
                    }
                }
            }

            var func = path_context.dungeon == null || path_context.dungeon.GetType() == typeof(Dungeon)
                ? process_function_call(token, path_context, args)
                : null;

            if (func != null)
            {
                if (func.type == Expression_Type.property_function_call && path_context.last.parent != null)
                {
                    //last.parent.child = null;
                    var last2 = path_context.last.parent;
                    path_context.last = path_context.last.parent;
                    last2.next = null;
                }
                else
                {
                    path_context.is_finished = true;
                }

                return func;
            }

            var dungeon = path_context.realm.get_dungeon(new[] { token });
            if (dungeon != null)
            {
                path_context.dungeon = dungeon;
                if (dungeon.GetType() == typeof(Treasury))
                {
                    if (path_context.index >= patterns.Count - 1)
                        throw new Exception("Enum " + token + " is missing a member value.");

                    var treasury = (Treasury)dungeon;
                    var jewel_name = patterns.Last().children[0].text;
                    if (!treasury.jewels.Contains(jewel_name))
                        throw new Exception("Enum " + treasury.name + " does not contain member: " + jewel_name + ".");

                    path_context.is_finished = true;
                    return new Jewel(treasury, treasury.jewels.IndexOf(jewel_name));
                }

                return new Profession_Expression(new Profession(Kind.reference, dungeon));
            }

            var realm = path_context.realm.overlord.root.get_child_realm(token, false);
            if (realm != null)
            {
                path_context.realm = realm;
                return new Empty_Expression();
            }

            if (overlord.global_variables.ContainsKey(token))
            {
                symbol = overlord.global_variables[token];
                path_context.dungeon = symbol.profession.dungeon;
                return new Variable(symbol);
            }

            throw new Parser_Exception("Unknown symbol: " + token, pattern.position);
        }

        private Expression process_function_call(string token, Path_Context path_context, List<Expression> args)
        {
            var dungeon = (Dungeon)path_context.dungeon;
            var minion = path_context.dungeon != null
                             ? dungeon.summon_minion(token, true)
                             : null;

            if (minion != null)
                return new Method_Call(minion, path_context.result, args);

            if (Minion.platform_specific_functions.Contains(token))
            {
                if (token == "add" || token == "setter")
                    return new Property_Function_Call(Property_Function_Type.set,
                        ((Portal_Expression)path_context.last).portal, args);

                return new Platform_Function(token, path_context.result, args);
            }

            return null;
        }

        private Parameter process_parameter(Legend source, Summoner_Context context)
        {
            var type = source.children[1] != null
                           ? parse_type2(source.children[1], context)
                           : new Profession(Kind.unknown);

            return new Parameter(new Symbol(source.children[0].text, type, null));
        }

        private Profession parse_type2(Legend source, Summoner_Context context)
        {
            var path = source.children[1].children.Select(p => p.text).ToArray();
            var result = parse_type2(path, context, source);
            result.is_list = result.is_list || source.children.Count > 3 && source.children[3] != null;
            if (source.children[0] != null)
                result.is_const = true;

            if (source.children[2] != null)
            {
                result.children = source.children[2].children.Select(c => parse_type2(c, context)).ToList();
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
                        return new Profession(Kind.Bool);
                    case "string":
                        return new Profession(Kind.String);
                    case "float":
                        return new Profession(Kind.Float);
                    case "int":
                        return new Profession(Kind.Int);
                }

                var profession = context.get_profession_pattern(text);
                if (profession != null)
                {
                    var result = profession.clone();
                    return result;
                }
            }

            var dungeon = context.get_dungeon(path);
            if (dungeon != null)
                return new Profession(Kind.reference, dungeon);

            throw new Parser_Exception("Invalid type: " + text + ".", source.position);
        }

        private Expression process_assignment(List<Legend> parts, Summoner_Context context)
        {
            var reference = process_reference(parts[0], context);
            var expression = process_expression(parts[2], context);
            var op = parts[1].text;
            var last = reference.get_end();
            if (reference != null && reference.type == Expression_Type.operation)
                throw new Exception("Cannot call function on operation.");

            if (last.type == Expression_Type.portal && op != "@=" && (!reference.get_profession().is_list || op != "="))
            {
                var portal_expression = (Portal_Expression)last;
                var portal = portal_expression.portal;
                if (portal.is_list && op != "=")
                {
                    expression = Minion.operation(op[0].ToString(), reference.clone(), expression);
                }
                var args = new List<Expression> { expression };

                // The setter absorbs the portal token, so remove it from the reference.
                if (last == reference)
                {
                    reference = null;
                }
                else
                {
                    //if (last.parent.type == Expression_Type.operation || last.type== Expression_Type.operation)
                    //    throw new Exception();
                    //last.parent.child = null;
                    last.parent.next = null;
                    last.parent = null;
                }

                // Check for origin parameter
                if (portal.setter != null && portal.setter.parameters.Count > 1)
                {
                    args.Add(new Self(context.dungeon));
                }

                return new Property_Function_Call(Property_Function_Type.set, portal, args) { reference = reference };
            }

            // @= forces direct assignment without setters
            if (op == "@=")
                op = "=";

            return new Assignment(
                reference,
                op,
                expression
                );
        }

        public Expression process_iterator(List<Legend> parts, Summoner_Context context)
        {
            var reference = process_expression_part(parts[1], context);
            var profession = reference.get_end().get_profession().get_reference();
            var symbol = context.scope.create_symbol(parts[0].text, profession);
            context.scope.add_map(symbol.name, c => new Variable(symbol));
            return new Iterator(symbol,
                                reference, process_block(parts[2], context)
                );
        }

        public Expression summon_if_chain(List<Legend> parts, Summoner_Context context)
        {
            var ifs = parts[0].children.Select(e => (Flow_Control)summon_statement(e, context)).ToList();
            //            var expressions = summon_statements(parts[0].children, context).ToList();
            var result = new If(ifs);
            if (parts[1] != null)
                result.else_block = summon_statements(parts[1].children, context);

            return result;
        }

        private Expression process_instantiate(List<Legend> parts, Summoner_Context context)
        {
            var type = parse_type2(parts[0], context);
            var args = parts[1].children.Select(p => process_expression(p, context));
            return new Instantiate(type, args);
        }

        private Expression summon_enum(List<Legend> parts, Summoner_Context context)
        {
            var items = new List<string>();
            foreach (var item in parts[1].children)
            {
                int? value = null;
                if (item.children[1] != null)
                    value = int.Parse(item.children[1].text);

                items.Add(item.children[0].text);
            }
            var treasury = context.realm.create_treasury(parts[0].text, items);
            return new Treasury_Definition(treasury);
        }

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
            minion.return_type = new Profession(Kind.none);

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
            return new Create_Array(parts[0].children.Select(p => process_expression(p, context)));
        }

        private Expression summon_dictionary(List<Legend> parts, Summoner_Context context)
        {
            var dictionary = new Dictionary<string, Expression>();
            foreach (var child in parts[0].children)
            {
                dictionary[child.children[0].text] = process_expression(child.children[1], context);
            }

            return new Create_Dictionary(dictionary);
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
                parser = new Parser(lexer, Resources.imp2_grammar);

            return parser.read(source, runes, start);
        }

        public void process_import(List<Legend> parts, Summoner_Context context)
        {


        }
    }
}
