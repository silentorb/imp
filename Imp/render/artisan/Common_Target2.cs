using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.legion;
using imperative.schema;
using metahub.render;
using metahub.schema;

namespace imperative.render.artisan
{
    public delegate Stroke Stroke_Delegate();
    public delegate List<Stroke> Stroke_List_Delegate();

    public abstract class Common_Target2
    {
        public Overlord overlord;
        public Target_Configuration config;
        protected static Dictionary<string, string> types = new Dictionary<string, string>
            {
                {"string", "string"},
                {"int", "int"},
                {"bool", "bool"},
                {"float", "float"},
                {"none", "void"},
                {"reference", "void"}
            };

        protected Common_Target2(Overlord overlord)
        {
            this.overlord = overlord;
        }

        protected Dungeon current_realm;
        public Dungeon current_dungeon;

        protected Dictionary<Minion, string> minion_names = new Dictionary<Minion, string>();
        protected List<Dictionary<Stroke, Profession>> scopes = new List<Dictionary<Stroke, Profession>>();
        protected Dictionary<Stroke, Profession> current_scope;
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

        public abstract void run(Build_Orders config1, string[] sources);
        public abstract void build_wrapper_project(Project project);

        public static string render_strokes(List<Stroke> strokes)
        {
            var passages = Painter.render_root(strokes).ToList();
            var segments = new List<Segment>();
            return Scribe.render(passages, segments);
        }

        virtual protected void push_scope()
        {
            current_scope = new Dictionary<Stroke, Profession>();
            scopes.Add(current_scope);
        }

        virtual protected void pop_scope()
        {
            scopes.RemoveAt(scopes.Count - 1);
            current_scope = scopes.Count > 0
                ? scopes[scopes.Count - 1]
                : null;
        }

        virtual public Stroke render_expression(Expression expression, Expression parent = null)
        {
            Stroke result;
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

                case Expression_Type.if_statement:
                    return render_if((If)expression);

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
                    result = render_profession(expression.get_profession());
                    break;

                case Expression_Type.anonymous_function:
                    return render_anonymous_function((Anonymous_Function)expression);

                case Expression_Type.comment:
                    return render_comment((Comment)expression);

                case Expression_Type.variable:
                    var variable_expression = (Variable)expression;

                    result = new Stroke_Token(variable_expression.symbol.name, variable_expression);
                    if (variable_expression.index != null)
                    {
                        result = result + new Stroke_Token("[")
                                 + render_expression(variable_expression.index) + new Stroke_Token("]");
                    }
                    break;

                case Expression_Type.parent_class:
                    result = render_dungeon_path(current_dungeon.parent.dungeon);
                    break;

                case Expression_Type.insert:
                    //                    throw new Exception("Not implemented");
                    result = new Stroke_Token(((Insert)expression).code);
                    break;

                case Expression_Type.create_dictionary:
                    return render_dictionary((Create_Dictionary)expression);

                default:
                    throw new Exception("Unsupported Expression type: " + expression.type + ".");
            }

            if (expression.next != null)
            {
                var child = render_expression(expression.next, expression);
                var text = child.full_text();
                result += !string.IsNullOrEmpty(text) && text[0] == '['
                    ? child
                    : get_connector(expression) + child;
            }

            return result;
        }

        virtual protected Stroke render_null()
        {
            return new Stroke_Token("null");
        }

        virtual protected Stroke render_this()
        {
            return new Stroke_Token("this");
        }

        protected bool is_start_portal(Portal_Expression portal_expression)
        {
            return portal_expression.parent == null
                || portal_expression.parent.next != portal_expression;
        }

        protected Stroke get_delimiter(Portal portal)
        {
            var delimiter = portal.has_enchantment(Enchantments.Static)
                ? config.namespace_separator
                : config.path_separator;

            return new Stroke_Token(delimiter);
        }

        virtual protected Stroke render_portal(Portal_Expression portal_expression)
        {
            var portal = portal_expression.portal;
            Stroke result = new Stroke_Token(portal.name);
            if (is_start_portal(portal_expression))
            {
                if (portal.has_enchantment(Enchantments.Static))
                {
                    if (portal.dungeon.name != "")
                        result = render_dungeon_path(portal.dungeon) + get_delimiter(portal) + result;
                }
                else if (!config.implicit_this && portal.dungeon.name != "")
                {
                    result = render_this() + new Stroke_Token(".") + result;
                }
            }
            if (portal_expression.index != null)
                result += new Stroke_Token("[") + render_expression(portal_expression.index) + new Stroke_Token("]");

            return result;
        }

        virtual protected Stroke render_statement(Expression statement)
        {
            Expression_Type type = statement.type;
            switch (type)
            {
                case Expression_Type.function_definition:
                    return render_function_definition(((Function_Definition)statement).minion);

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
                    return render_comment((Comment)statement);

                case Expression_Type.declare_variable:
                    return render_variable_declaration((Declare_Variable)statement);

                case Expression_Type.statement:
                    var state = (Statement)statement;
                    var statement_result = (Stroke)new Stroke_Token(state.name);
                    if (state.next != null)
                        statement_result += new Stroke_Token(" ") + render_expression(state.next);

                    statement_result.expression = statement;
                    return statement_result + terminate_statement();

                case Expression_Type.insert:
                    return new Stroke_Token(((Insert)statement).code);

                default:
                    return render_expression(statement) + terminate_statement();
            }
        }

        virtual public Stroke terminate_statement()
        {
            return new Stroke_Token(config.statement_terminator);
        }

        virtual public List<Stroke> render_statements(IEnumerable<Expression> statements)
        {
            return statements.Select(render_statement).ToList();
        }

        virtual protected Stroke render_dungeon(Dungeon dungeon)
        {
            if (dungeon.is_abstract)
                return new Stroke_Token("");

            current_dungeon = dungeon;

            var abstract_keyword = dungeon.minions.Any(m => m.Value.is_abstract)
                ? "abstract "
                : "";

            var intro = new Stroke_Token("public " + abstract_keyword + "class ")
                + render_dungeon_path(dungeon);

            var result = intro + render_block(render_properties(dungeon), false);
            //                + render_statements(statements, newline())
            //            );

            current_dungeon = null;

            return result;
        }

        virtual protected List<Stroke> render_properties(Dungeon dungeon)
        {
            return dungeon.core_portals.Values.Select(render_property).ToList();
        }

        virtual protected Stroke render_property(Portal portal)
        {
            Stroke main = new Stroke_Token(portal.name);
            if (config.type_mode == Type_Mode.required_prefix)
                main = render_profession(portal.profession) + new Stroke_Token(" ") + main;
            else if (config.type_mode == Type_Mode.optional_suffix)
                main += new Stroke_Token(":") + render_profession(portal.profession);

            if (portal.has_enchantment("static"))
                main = new Stroke_Token("static ") + main;

            if (config.explicit_public_members)
                main = new Stroke_Token("public ") + main;

            var assignment = portal.other_dungeon == null || !portal.other_dungeon.is_value
                ? new Stroke_Token(" = ") + get_default_value(portal)
                : new Stroke_Token("");

            return main + assignment + terminate_statement();
        }

        virtual protected Stroke get_default_value(Portal portal)
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

        protected virtual Stroke render_list(Profession profession, List<Expression> args)
        {
            if (args == null)
                return new Stroke_Token(config.list_start + config.list_end);

            var arg_string = Stroke.join(args.Select(a => render_expression(a)).ToList(), ", ");

            if (arg_string.Any(Stroke.contains_block))
            {
                return new Stroke_Token(config.list_start)
                    + new Stroke_List(Stroke_Type.block, arg_string)
                    + new Stroke_Newline() + new Stroke_Token(config.list_end);
            }

            return new Stroke_Token(config.list_start) + arg_string + new Stroke_Token(config.list_end);
        }

        virtual protected Stroke render_variable_declaration(Declare_Variable declaration)
        {
            var result = new Stroke_Token("var " + declaration.symbol.name)
                + (declaration.expression != null
                    ? new Stroke_Token(" = ") + render_expression(declaration.expression)
                    : null)
                + terminate_statement();

            result.expression = declaration;
            return result;
        }

        virtual protected Stroke render_literal(object value, Profession profession)
        {
            if (profession == null)
                return new Stroke_Token(value.ToString());

            if (profession == Professions.unknown)
                return new Stroke_Token(value.ToString());

            if (profession == Professions.Float)
            {
                var result = value.ToString();
                return new Stroke_Token(config.float_suffix && result.Contains('.')
                    ? result + "f"
                    : result);
            }

            if (profession == Professions.Int)
                return new Stroke_Token(value.ToString());

            if (profession == Professions.String)
                return new Stroke_Token(config.primary_quote + value + config.primary_quote);

            if (profession == Professions.Bool)
                return new Stroke_Token((bool)value ? "true" : "false");

            if (profession == Professions.any)
            {
                if (value == null)
                    return render_null();

                if (!profession.dungeon.is_value)
                    throw new Exception("Literal expressions must be scalar values.");

                //                    if (profession.dungeon.GetType() == typeof(Treasury))
                //                        return render_enum_value((Treasury)profession.dungeon, (int)value);

                if (value != null)
                    return new Stroke_Token(value.ToString());

                //                return new Stroke(render_dungeon_path(profession.dungeon) + "()");
            }

            return null;
        }

        virtual protected Stroke render_iterator_block(Iterator statement)
        {
            var parameter = statement.parameter;
            //            var it = parameter.scope.create_symbol(parameter.name, parameter.profession);
            var expression = render_iterator(parameter, statement.expression);

            var result = new Stroke_Token(config.foreach_symbol + " (") + expression + new Stroke_Token(")")
                + render_block(render_statements(statement.body));

            result.expression = statement;
            return result;
        }

        virtual protected Stroke render_iterator(Symbol parameter, Expression expression)
        {
            var path_string = render_expression(expression);
            return new Stroke_Token("var " + parameter.name + " in ") + path_string;
        }

        virtual protected Stroke render_operation(Operation operation)
        {
            return new Stroke_List(Stroke_Type.chain, Stroke.join(
                operation.children.Select(c =>
                    c.type == Expression_Type.operation
                        && ((Operation)c).is_condition() == operation.is_condition()
                        ? new Stroke_Token("(") + render_expression(c) + new Stroke_Token(")")
                        : render_expression(c)
                    ).ToList(), " " + operation.op + " "));
        }

        virtual protected Stroke render_property_function_call(Property_Function_Call expression, Expression parent)
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
                return new Stroke_Token(ref_full + setter.name + "(" + args + ")");

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

            return new Stroke_Token(ref_full + portal.name + " = " + args);
        }

        protected abstract Stroke render_platform_function_call(Platform_Function expression, Expression parent);

        virtual protected Stroke render_function_call(Abstract_Function_Call expression, Expression parent)
        {
            var method_call = expression as Method_Call;
            Stroke this_string = null;

            Minion minion = null;
            if (method_call != null)
            {
                minion = method_call.minion;
                if (minion == Professions.List.minions["get"])
                    return render_list(parent.get_profession(), expression.args);

                if (method_call.parent == null || !method_call.parent.is_token())
                {
                    if (minion != null && minion.has_enchantment(Enchantments.Static))
                    {
                        this_string = render_dungeon_path(minion.dungeon) + new Stroke_Token(config.namespace_separator);
                    }
                    else if (!config.implicit_this
                             && minion != null
                             && minion.dungeon.realm != null)
                    {
                        this_string = render_this();
                    }
                }
            }

            var ref_string = expression.reference != null
               ? this_string + render_expression(expression.reference)
               : this_string;

            var result = render_function_call2(expression, ref_string, minion);

            result.expression = expression;
            return result;
        }

        private string get_minion_name(Abstract_Function_Call call)
        {
            var method_call = call as Method_Call;
            if (method_call != null)
            {
                var minion = method_call.minion;
                if (minion_names.ContainsKey(minion))
                    return minion_names[minion];
            }

            return call.get_name();
        }

        protected virtual Stroke render_function_call2(Abstract_Function_Call expression, Stroke ref_string, Minion minion)
        {
            var second = new Stroke_Token(get_minion_name(expression) + "(");
            var ref_full = ref_string != null
                ? ref_string + second
                : second;

            return ref_full + render_arguments(expression.args)
                 + new Stroke_Token(")");
        }

        private Stroke render_arguments(List<Expression> args)
        {
            return new Stroke_List(Stroke_Type.chain, Stroke.join(args.Select(a => render_expression(a)).ToList(), ", "));
        }

        virtual protected Stroke render_assignment(Assignment statement)
        {
            var result = render_expression(statement.target) + new Stroke_Token(" " + statement.op + " ") + render_expression(statement.expression)
                + terminate_statement();

            result.expression = statement;
            return result;
        }

        virtual protected Stroke render_comment(Comment comment)
        {
            return new Stroke_Token(comment.is_multiline
                ? "/* " + comment.text + "*/"
                : "// " + comment.text);
        }

        virtual protected Stroke render_instantiation(Instantiate expression)
        {
            if (expression.profession.dungeon == Professions.List)
                return render_list(expression.profession, expression.args);

            var args = expression.args.Select(a => render_expression(a).full_text()).join(", ");
            return new Stroke_Token("new ") + render_profession(expression.profession)
                + new Stroke_Token("(" + args + ")");
        }

        //        protected abstract Stroke render_function_definition(Function_Definition definition);
        //        {
        //            if (definition.is_abstract)
        //                return "";
        //
        //            return render.get_indentation() + definition.name + ": function(" + definition.parameters.Select(p => p.symbol.name).join(", ") + ")"
        //                + render_minion_scope(definition.minion);
        //        }

        virtual protected Stroke render_anonymous_function(Anonymous_Function definition)
        {
            minion_stack.Push(definition.minion);
            var result = new Stroke_Token("function(" + definition.parameters.Select(p => p.symbol.name).join(", ") + ")")
            + render_block(render_statements(definition.minion.expressions), false);
            minion_stack.Pop();
            return result;
        }

        virtual protected Stroke render_minion_scope(Minion_Base minion)
        {
            minion_stack.Push(minion);

            var result = render_block(render_statements(minion.expressions));

            minion_stack.Pop();
            return result;
        }

        virtual public Stroke render_realm(Dungeon realm, Stroke_List_Delegate action)
        {
            if (realm == null || realm.name == "")
                return new Stroke_Token() + action();

            current_realm = realm;
            var result = new Stroke_Token(config.namespace_keyword + " ")
                + render_realm_path(realm, config.namespace_separator)
                + render_block(action(), false);

            current_realm = null;
            return result;
        }

        protected Stroke render_realm_path(Dungeon realm, string separator)
        {
            return new Stroke_Token(realm.parent != null && realm.parent.name != ""
                ? render_realm_path(realm.parent.dungeon, separator) + separator + realm.name
                : realm.name);
        }

        //        virtual protected Stroke render_scope(String_Delegate action)
        //        {
        //            push_scope();
        //            var result = config.block_brace_same_line
        //                ? " {" + newline()
        //                : newline() + line("{");
        //
        //            indent();
        //            result += action();
        //            unindent();
        //            result += line("}");
        //            pop_scope();
        //            return result;
        //        }

        //        virtual protected Stroke render_treasury(Treasury treasury)
        //        {
        //            var i = treasury.jewels.Count;
        //            return add("enum " + treasury.name) + render_scope(() =>
        //                treasury.jewels.Select(j =>
        //                    add(j + (--i > 0 ? "," : "")) + newline()).join("")
        //                );
        //        }

        virtual protected Stroke render_flow_control(Flow_Control statement, bool minimal, bool is_succeeded = false)
        {
            var start = new Stroke_Token(statement.flow_type.ToString().ToLower());
            var block = render_block(render_statements(statement.body), true, is_succeeded);

            if (statement.flow_type == Flow_Control_Type.Else)
                return start + block;

            var result = start + new Stroke_Token(" (") +
                render_expression(statement.condition) + new Stroke_Token(")")
                + block;

            result.expression = statement;
            return result;
        }

        virtual protected Stroke render_if(If statement)
        {
            var minimal = statement.if_statements.All(e => e.body.Count == 1);
            var block_count = statement.if_statements.Count;
            //            if (statement.else_block != null)
            //                ++block_count;

            //            throw new Exception("Not implemented");
            var i = 0;
            var strokes = statement.if_statements.Select(e => render_flow_control(e, minimal, ++i < block_count)).ToList();
            //            if (statement.else_block != null)
            //            {
            //                if (strokes.Last().type == Stroke_Type.newline)
            //                   strokes.RemoveAt(strokes.Count - 1);
            //
            //                strokes.Add(new Stroke_Token("else") + render_block(render_statements(statement.else_block)));
            //            }
            return strokes.Count == 1
                ? strokes.First()
                : new Stroke_List(Stroke_Type.statements, strokes);
        }

        virtual public Stroke render_dungeon_path(IDungeon dungeon)
        {
            return dungeon.realm != null && dungeon.realm.name != ""
                && (dungeon.realm != current_realm || !config.supports_namespaces)
                ? render_dungeon_path(dungeon.realm) + new Stroke_Token(config.namespace_separator + dungeon.name)
                : new Stroke_Token(dungeon.name);
        }

        //        virtual public Stroke render_profession(Symbol symbol, bool is_parameter = false)
        //        {
        //            return render_profession(symbol.profession, is_parameter);
        //        }

        virtual public Stroke render_profession(Profession signature, bool is_parameter = false)
        {
            if (signature.dungeon == Professions.List)
                return listify(render_profession(signature.children[0]), signature);
            //            throw new Exception("Not implemented.");
            var lower_name = signature.dungeon.name.ToLower();
            var name = types.ContainsKey(lower_name)
                ? new Stroke_Token(types[lower_name])
                : render_dungeon_path(signature.dungeon);

            return name;
        }

        virtual public Stroke listify(Stroke type, Profession signature)
        {
            return type + new Stroke_Token("[]");
        }

        virtual public Stroke render_function_definition(Minion definition)
        {
            minion_stack.Push(definition);
            if (definition.is_abstract && !config.supports_abstract)
                return new Stroke_Token("");

            var intro = new Stroke_Token((config.explicit_public_members ? "public " : "")
                + (definition.is_abstract ? "abstract " : "")
                + (definition.is_static ? "static " : ""))
                + (definition.return_type != null
                    ? render_profession(definition.return_type) + new Stroke_Token(" ")
                    : new Stroke_Token())
                + new Stroke_Token(definition.name
                + "(") + Stroke.join(definition.parameters
                    .Select(render_definition_parameter).ToList(), ", ") + new Stroke_Token(")");

            if (definition.is_abstract)
                return intro + terminate_statement();

            var result = intro + render_block(render_statements(definition.expressions));
            minion_stack.Pop();
            return result;
        }

        virtual public Stroke render_definition_parameter(Parameter parameter)
        {
            return render_profession(parameter.symbol.profession, true) + new Stroke_Token(" " + parameter.symbol.name);
        }

        virtual protected Stroke get_connector(Expression expression)
        {
            if (expression.type == Expression_Type.parent_class)
                return new Stroke_Token(config.namespace_separator);

            return new Stroke_Token(config.path_separator);
        }

        virtual protected string get_connector(Profession profession)
        {
            return config.path_separator;
        }

        private Stroke render_dictionary(Create_Dictionary dictionary)
        {
            if (dictionary.items.Count == 0)
                return new Stroke_Token("{}", dictionary);

            var items = dictionary.items;
            var most = items.Take(items.Count - 1);
            var last = items.Last();

            var result = new Stroke_List(Stroke_Type.chain, new List<Stroke>
            {
                new Stroke_Token("{")
            });

            var entries = most.Select(pair =>
                new Stroke_Token(pair.Key + ": ") + render_expression(pair.Value) + new Stroke_Token(",")
            ).ToList();

            entries.Add(new Stroke_Token(last.Key + ": ") + render_expression(last.Value));
            result.children.Add(new Stroke_List(Stroke_Type.block, entries));
            result.children.Add(new Stroke_Newline());
            result.children.Add(new Stroke_Token("}"));

            return result;
        }

        public Stroke render_block(List<Stroke> strokes, bool try_minimal = true,
            bool is_succeeded = false)
        {
            var block = new Stroke_List(Stroke_Type.block, strokes);
            if (strokes.Count == 1 && try_minimal)
            {
                return is_succeeded
                ? block
                : block + new Stroke_Newline { ignore_on_block_end = true };
            }

            return new Stroke_Token(" {") + new Stroke_List(Stroke_Type.block, strokes)
                + new Stroke_Newline()
                + new Stroke_Token("}")
                ;
        }
    }
}
