using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.schema;
using metahub.render;
using metahub.schema;

namespace imperative.render
{
    public abstract class Common_Target : Target
    {
        protected Dictionary<string, string> types = new Dictionary<string, string>
            {
                {"string", "string"},
                {"int", "int"},
                {"bool", "bool"},
                {"float", "float"},
                {"none", "void"},
                {"reference", "void"}
            };

        protected Common_Target(Overlord overlord)
            : base(overlord)
        {

        }

        protected Dungeon current_realm;
        protected Dungeon current_dungeon;

        protected List<Dictionary<string, Profession>> scopes = new List<Dictionary<string, Profession>>();
        protected Dictionary<string, Profession> current_scope;
        protected Stack<Minion_Base> minion_stack = new Stack<Minion_Base>();
        protected Minion_Base current_minion
        {
            get
            {
                if (minion_stack.Count == 0)
                    return null;

                return minion_stack.Peek();
            }
        }

        virtual protected void push_scope()
        {
            current_scope = new Dictionary<string, Profession>();
            scopes.Add(current_scope);
        }

        virtual protected void pop_scope()
        {
            scopes.RemoveAt(scopes.Count - 1);
            current_scope = scopes.Count > 0
                ? scopes[scopes.Count - 1]
                : null;
        }

        virtual protected string render_expression(Expression expression, Expression parent = null)
        {
            string result;
            switch (expression.type)
            {
                case Expression_Type.literal:
                    var literal = (Literal)expression;
                    return render_literal(literal.value, literal.profession);

                case Expression_Type.operation:
                    return render_operation((Operation)expression);

                case Expression_Type.portal:
                    result = render_portal((Portal_Expression)expression);
                    break;

                case Expression_Type.function_call:
                    result = render_function_call((Abstract_Function_Call)expression, parent);
                    break;

                case Expression_Type.property_function_call:
                    result = render_property_function_call((Property_Function_Call)expression, parent);
                    break;

                case Expression_Type.platform_function:
                    return render_platform_function_call((Platform_Function)expression, null);

                case Expression_Type.instantiate:
                    result = render_instantiation((Instantiate)expression);
                    break;

                case Expression_Type.self:
                    result = render_this();
                    break;

                case Expression_Type.null_value:
                    return render_null();

                case Expression_Type.profession:
                    result = expression.get_profession().dungeon.name;
                    break;

                //                case Expression_Type.create_array:
                //                    result = "[" + render_arguments(((Create_Array)expression).items) + "]";
                //                    break;

                case Expression_Type.anonymous_function:
                    return render_anonymous_function((Anonymous_Function)expression);

                case Expression_Type.comment:
                    return render_comment((Comment)expression);

                case Expression_Type.variable:
                    var variable_expression = (Variable)expression;
                    //if (find_variable(variable_expression.symbol.name) == null)
                    //    throw new Exception("Could not find variable: " + variable_expression.symbol.name + ".");

                    result = variable_expression.symbol.name;
                    if (variable_expression.index != null)
                        result += "[" + render_expression(variable_expression.index) + "]";

                    break;

                case Expression_Type.parent_class:
                    result = current_dungeon.parent.name;
                    break;

                case Expression_Type.insert:
                    result = ((Insert)expression).code;
                    break;

                //                case Expression_Type.jewel:
                //                    var jewel = (Jewel)expression;
                //                    return render_enum_value(jewel.treasury, jewel.value);

                case Expression_Type.create_dictionary:
                    return render_dictionary((Create_Dictionary)expression);

                default:
                    throw new Exception("Unsupported Expression type: " + expression.type + ".");
            }

            if (expression.next != null)
            {
                var child = render_expression(expression.next, expression);
                result += child[0] == '['
                    ? child 
                    : "." + child;
            }

            return result;
        }

        virtual protected string render_null()
        {
            return "null";
        }

        virtual protected string render_this()
        {
            return "this";
        }

        virtual protected string render_portal(Portal_Expression portal_expression)
        {
            var portal = portal_expression.portal;
            var result = portal.name;
            if (portal_expression.parent == null || portal_expression.parent.next != portal_expression)
            {
                if (portal.has_enchantment(Enchantments.Static))
                {
                    if (portal.dungeon.name != "")
                        result = render_dungeon_path(portal.dungeon) + "." + result;
                }
                else if (!config.implicit_this && portal.dungeon.name != "")
                {
                    result = render_this() + "." + result;
                }
            }
            if (portal_expression.index != null)
                result += "[" + render_expression(portal_expression.index) + "]";

            return result;
        }

        virtual protected string render_statement(Expression statement)
        {
            Expression_Type type = statement.type;
            switch (type)
            {
                case Expression_Type.space:
                    var space = (Namespace)statement;
                    return render_realm(space.realm, () => render_statements(space.body));

                case Expression_Type.class_definition:
                    var definition = (Class_Definition)statement;
                    return render_dungeon(definition.dungeon, definition.body);

                case Expression_Type.function_definition:
                    return render_function_definition((Function_Definition)statement);

                case Expression_Type.flow_control:
                    var flow_control = (Flow_Control)statement;
                    return render_flow_control(flow_control, flow_control.flow_type == Flow_Control_Type.If);

                case Expression_Type.if_statement:
                    return render_if((If)statement);

                case Expression_Type.iterator:
                    return render_iterator_block((Iterator)statement);

                case Expression_Type.assignment:
                    return render_assignment((Assignment)statement);

                case Expression_Type.comment:
                    return line(render_comment((Comment)statement));

                case Expression_Type.declare_variable:
                    return render_variable_declaration((Declare_Variable)statement);

                case Expression_Type.statement:
                    var state = (Statement)statement;
                    return line(state.name + (state.next != null
                        ? " " + render_expression(state.next)
                        : "") + terminate_statement());

                case Expression_Type.insert:
                    return line(((Insert)statement).code);

                default:
                    return add("") + render_expression(statement) + terminate_statement() + newline();
            }
        }

        virtual protected string terminate_statement()
        {
            return config.statement_terminator;
        }

        virtual protected string render_statements(IEnumerable<Expression> statements, string glue = "")
        {
            return statements.Select(render_statement).join(glue);
        }

        virtual protected string render_dungeon(Dungeon dungeon, IEnumerable<Expression> statements)
        {
            if (dungeon.is_abstract)
                return "";

            current_dungeon = dungeon;

            var abstract_keyword = dungeon.minions.Any(m => m.Value.is_abstract)
                ? "abstract "
                : "";

            var intro = "public " + abstract_keyword + "class " + render_dungeon_path(dungeon);
            var result = add(intro) + render_scope(() =>
                render_properties(dungeon) + newline()
                + render_statements(statements, newline())
            );

            current_dungeon = null;

            return result;
        }

        virtual protected string render_properties(Dungeon dungeon)
        {
            return dungeon.core_portals.Values.Select(render_property).join("");
        }

        virtual protected string render_property(Portal portal)
        {
            var main = portal.name;
            if (config.type_mode == Type_Mode.required_prefix)
                main = render_profession(portal.profession) + " " + main;
            else if (config.type_mode == Type_Mode.optional_suffix)
                main += ":" + render_profession(portal.profession);

            if (portal.has_enchantment("static"))
                main = "static " + main;

            if (config.explicit_public_members)
                main = "public " + main;

            var assignment = portal.other_dungeon == null || !portal.other_dungeon.is_value
                ? " = " + get_default_value(portal)
                : "";

            return line(main + assignment + terminate_statement());
        }

        virtual protected string get_default_value(Portal portal)
        {
            if (portal.is_list)
            {
                if (portal.profession.is_array(overlord))
                    return render_null();

                return render_list(portal.profession, null);
            }
            if (portal.default_expression != null)
            {
                return render_expression(portal.default_expression);
            }
            return render_literal(portal.get_default_value(), portal.get_target_profession());
        }

        protected virtual string render_list(Profession profession, List<Expression> args)
        {
            if (args == null)
                return "[]";

            indent();
            var arg_string = args.Select(a => render_expression(a)).join(", ");
            if (arg_string.Contains("\n"))
            {
                arg_string = newline() + add() + arg_string + newline() + unindent() + add();
            }
            else
            {
                unindent();
            }
            return "[" + arg_string + "]";
        }

        virtual protected string render_variable_declaration(Declare_Variable declaration)
        {
            var profession = declaration.symbol.get_profession(overlord);
            current_scope[declaration.symbol.name] = profession;

            return add("var " + declaration.symbol.name)
                + (declaration.expression != null
                    ? " = " + render_expression(declaration.expression)
                    : "")
                + terminate_statement() + newline();
        }

        virtual protected string render_literal(Object value, Profession profession)
        {
            if (profession == null)
                return value.ToString();

            if (profession == Professions.unknown)
                return value.ToString();

            if (profession == Professions.Float)
            {
                var result = value.ToString();
                return config.float_suffix && result.Contains('.')
                    ? result + "f"
                    : result;
            }

            if (profession == Professions.Int)
                return value.ToString();

            if (profession == Professions.String)
                return config.primary_quote + value + config.primary_quote;

            if (profession == Professions.Bool)
                return (bool)value ? "true" : "false";

            if (profession == Professions.any)
            {
                if (value == null)
                    return render_null();

                if (!profession.dungeon.is_value)
                    throw new Exception("Literal expressions must be scalar values.");

                //                    if (profession.dungeon.GetType() == typeof(Treasury))
                //                        return render_enum_value((Treasury)profession.dungeon, (int)value);

                if (value != null)
                    return value.ToString();

                return render_dungeon_path(profession.dungeon) + "()";
            }

            return null;
//            throw new Exception("Invalid literal " + value + " type " + profession.dungeon.name + ".");
        }

        //        virtual protected string render_enum_value(Treasury treasury, int value)
        //        {
        //            return config.supports_enums
        //                ? treasury.name + get_connector(new Profession(Kind.reference, treasury)) + treasury.jewels[value]
        //                : value.ToString();
        //        }

//        virtual protected string render_dungeon_name(IDungeon dungeon)
//        {
//            if (dungeon.realm != current_realm)
//                return render_dungeon_path(dungeon.realm) + "." + dungeon.name;
//
//            return dungeon.name;
//        }

//        virtual protected string render_realm_name(Dungeon realm)
//        {
//            var path = Generator.get_namespace_path(realm);
//            return path.join(".");
//        }

        virtual protected string render_scope(List<Expression> statements, bool minimal = false, bool is_succeeded = false)
        {
            indent();
            push_scope();
            var first = add(minimal ? "" : " {") + newline();
            var lines = line_count;
            var block = render_statements(statements);
            pop_scope();
            unindent();

            if (minimal)
            {
                minimal = line_count == lines + 1;
            }

            return first + block + (minimal
                ? is_succeeded ? "" : newline()
                : "}" + newline());
        }

        virtual protected string render_iterator_block(Iterator statement)
        {
            var parameter = statement.parameter;
            //            var it = parameter.scope.create_symbol(parameter.name, parameter.profession);
            var expression = render_iterator(parameter, statement.expression);

            var result = add(config.foreach_symbol + " (" + expression + ")") + render_scope(statement.body);
            return result;
        }

        virtual protected string render_iterator(Symbol parameter, Expression expression)
        {
            var path_string = render_expression(expression);
            return "var " + parameter.name + " in " + path_string;
        }

        virtual protected string render_operation(Operation operation)
        {
            return operation.children.Select(c =>
                c.type == Expression_Type.operation && ((Operation)c).is_condition() == operation.is_condition()
                ? "(" + render_expression(c) + ")"
                : render_expression(c)
            ).join(" " + operation.op + " ");
        }

        virtual protected string render_property_function_call(Property_Function_Call expression, Expression parent)
        {
            var ref_full = expression.reference != null
                ? render_expression(expression.reference) + "."
                : "";

            if (!config.implicit_this && (expression.reference == null
                || (expression.reference.type == Expression_Type.portal 
                && ((Portal_Expression)expression.reference).portal.dungeon.name != "")))
                ref_full = render_this() + "." + ref_full;

            var args = expression.args.Select(e => render_expression(e)).join(", ");
            var portal = expression.portal;
            var setter = portal.setter;
            if (setter != null)
                return ref_full + setter.name + "(" + args + ")";

            if (expression.portal.is_list)
            {
                Expression reference;

                if (expression.reference != null)
                {
                    reference = expression.reference.clone();
                    reference.next = new Portal_Expression(expression.portal);
                }
                else
                {
                    reference = new Portal_Expression(expression.portal);
                }

                var add = new Platform_Function("add", reference, expression.args);
                return render_expression(add);
            }

            return ref_full + portal.name + " = " + args;
        }

        protected abstract string render_platform_function_call(Platform_Function expression, Expression parent);

        virtual protected string render_function_call(Abstract_Function_Call expression, Expression parent)
        {
            var method_call = expression as Method_Call;
            string this_string = "";

            if (method_call != null)
            {
                if (method_call.minion == Professions.List.minions["get"])
                    return render_list(parent.get_profession(), expression.args);

                if (method_call.parent == null || !method_call.parent.is_token())
                {
                    if (method_call.minion != null && method_call.minion.has_enchantment(Enchantments.Static))
                    {
                        this_string = render_dungeon_path(current_dungeon);
                    }
                    else if (!config.implicit_this
                             && method_call.minion != null
                             && method_call.minion.dungeon.realm != null)
                    {
                        this_string = render_this();
                    }
                }
            }

            var ref_string = expression.reference != null
               ? this_string + render_expression(expression.reference)
               : this_string;

            var ref_full = ref_string.Length > 0
                ? ref_string + "."
                : "";

            return ref_full + expression.get_name() + "(" + render_arguments(expression.args)
                 + ")";
        }

        private string render_arguments(List<Expression> args)
        {
            return args.Select(a => render_expression(a)).join(", ");
        }

        virtual protected string render_assignment(Assignment statement)
        {
            return add(render_expression(statement.target) + " " + statement.op + " " + render_expression(statement.expression))
                + terminate_statement() + newline();
        }

        virtual protected string render_comment(Comment comment)
        {
            return comment.is_multiline
                ? "/* " + comment.text + "*/"
                : "// " + comment.text;
        }

        private string render_instantiation(Instantiate expression)
        {
            if (expression.profession.dungeon == Professions.List)
                return render_list(expression.profession, expression.args);

            var args = expression.args.Select(a => render_expression(a)).join(", ");
            return "new " + render_profession(expression.profession) + "(" + args + ")";
        }

        //        protected abstract string render_function_definition(Function_Definition definition);
        //        {
        //            if (definition.is_abstract)
        //                return "";
        //
        //            return render.get_indentation() + definition.name + ": function(" + definition.parameters.Select(p => p.symbol.name).join(", ") + ")"
        //                + render_minion_scope(definition.minion);
        //        }

        virtual protected string render_anonymous_function(Anonymous_Function definition)
        {
            return "function(" + definition.parameters.Select(p => p.symbol.name).join(", ") + ")"
            + render_minion_scope(definition.minion);
        }

        virtual protected string render_minion_scope(Minion_Base minion)
        {
            minion_stack.Push(minion);

            var result = render_scope(() =>
               {
                   foreach (var parameter in minion.parameters)
                   {
                       current_scope[parameter.symbol.name] = parameter.symbol.profession;
                   }

                   return render_statements(minion.expressions);
               });

            minion_stack.Pop();
            return result;
        }

        virtual protected string render_realm(Dungeon realm, String_Delegate action)
        {
            current_realm = realm;
            var result = add(config.namespace_keyword + " " + render_realm_path(realm, config.namespace_separator) + render_scope(action));

            current_realm = null;
            return result;
        }

        protected string render_realm_path(Dungeon realm, string separator)
        {
            return realm.parent != null && realm.parent.name != ""
                ? render_realm_path(realm.parent, separator) + separator + realm.name
                : realm.name;
        }

        virtual protected string render_scope(String_Delegate action)
        {
            push_scope();
            var result = config.block_brace_same_line
                ? " {" + newline()
                : newline() + line("{");

            indent();
            result += action();
            unindent();
            result += line("}");
            pop_scope();
            return result;
        }

        //        virtual protected string render_treasury(Treasury treasury)
        //        {
        //            var i = treasury.jewels.Count;
        //            return add("enum " + treasury.name) + render_scope(() =>
        //                treasury.jewels.Select(j =>
        //                    add(j + (--i > 0 ? "," : "")) + newline()).join("")
        //                );
        //        }

        virtual protected string render_flow_control(Flow_Control statement, bool minimal, bool is_succeeded = false)
        {
            var expression = render_expression(statement.condition);

            return add(statement.flow_type.ToString().ToLower() + " (" + expression + ")")
                + render_scope(statement.body, minimal, is_succeeded);
        }

        virtual protected string render_if(If statement)
        {
            var minimal = statement.if_statements.All(e => e.body.Count == 1);
            var block_count = statement.if_statements.Count;
            if (statement.else_block != null)
                ++block_count;

            //            if (statement.else_block != null)
            //                minimal = false;
            var i = 0;
            var result = statement.if_statements.Select(e => render_flow_control(e, minimal, ++i < block_count)).join("");
            if (statement.else_block != null)
                result += add("else") + render_scope(statement.else_block, minimal);

            return result;
        }

        virtual protected string render_dungeon_path(IDungeon dungeon)
        {
            return dungeon.realm != null && dungeon.realm.name != ""
                && (dungeon.realm != current_realm || !config.supports_namespaces)
                ? render_dungeon_path(dungeon.realm) + config.namespace_separator + dungeon.name
                : dungeon.name;
        }

        virtual protected string render_profession(Symbol symbol, bool is_parameter = false)
        {
            if (symbol.profession != null)
                return render_profession(symbol.profession, is_parameter);

            return render_profession(symbol.profession, is_parameter);
        }

        virtual protected string render_profession(Profession signature, bool is_parameter = false)
        {
//            throw new Exception("Not implemented.");
            var lower_name = signature.dungeon.name.ToLower();
            var name = types.ContainsKey(lower_name)
                ? types[lower_name]
                : render_dungeon_path(signature.dungeon);

            return signature.dungeon == Professions.List
                ? listify(name, signature)
                : name;
        }

        virtual protected string listify(string type, Profession signature)
        {
            return type + "[]";
        }

        virtual protected string render_function_definition(Function_Definition definition)
        {
            if (definition.is_abstract && !config.supports_abstract)
                return "";

            var intro = (config.explicit_public_members ? "public " : "")
                + (definition.minion.is_abstract ? "abstract " : "")
                + (definition.minion.is_static ? "static " : "")
                + (definition.return_type != null ? render_profession(definition.return_type) + " " : "")
                + definition.name
                + "(" + definition.parameters.Select(render_definition_parameter).join(", ") + ")";

            if (definition.is_abstract)
                return line(intro + terminate_statement());

            return add(intro) + render_scope(() =>
            {
                foreach (var parameter in definition.parameters)
                {
                    current_scope[parameter.symbol.name] = parameter.symbol.profession;
                }

                return render_statements(definition.expressions);
            });
        }

        virtual protected string render_definition_parameter(Parameter parameter)
        {
            return render_profession(parameter.symbol, true) + " " + parameter.symbol.name;
        }

        virtual protected string get_connector(Expression expression)
        {
            if (expression.type == Expression_Type.parent_class)
                return config.namespace_separator;

            return config.path_separator;
        }

        virtual protected string get_connector(Profession profession)
        {
            return config.path_separator;
        }

        private string render_dictionary(Create_Dictionary dictionary)
        {
            if (dictionary.items.Count == 0)
                return "{}";

            var items = dictionary.items;
            var most = items.Take(items.Count - 1);
            var last = items.Last();

            var first = add("{") + newline();

            indent();

            var second = most.Select(pair => add("") + pair.Key + ": " + render_expression(pair.Value) + "," + newline())
                .@join("")
                         + add("") + last.Key + ": " + render_expression(last.Value) + newline();

            unindent();

            return first + second + add("}");
        }
    }
}
