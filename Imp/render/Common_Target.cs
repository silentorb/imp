using System;
using System.Collections.Generic;
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
                {"none", "void"}
            };

        protected Common_Target(Overlord overlord)
            : base(overlord)
        {

        }

        protected Realm current_realm;
        protected Dungeon current_dungeon;
        protected Minion_Base current_minion;
        protected List<Dictionary<string, Profession>> scopes = new List<Dictionary<string, Profession>>();
        protected Dictionary<string, Profession> current_scope;

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
                    return "null";

                case Expression_Type.profession:
                    result = expression.get_profession().dungeon.name;
                    break;

                case Expression_Type.create_array:
                    result = "FOOO";
                    break;

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

                case Expression_Type.jewel:
                    var jewel = (Jewel) expression;
                    return render_enum_value(jewel.treasury, jewel.value);

                default:
                    throw new Exception("Unsupported Expression type: " + expression.type + ".");
            }

            if (expression.next != null)
            {
                result += "." + render_expression(expression.next, expression);
            }

            return result;
        }

        virtual protected string render_this()
        {
            return "this";
        }

        virtual protected string render_portal(Portal_Expression portal_expression)
        {
            var result = portal_expression.portal.name;
            if (!config.implicit_this && (portal_expression.parent == null || portal_expression.parent.next == null))
                result = render_this() + "." + result;

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
                    return line(render_expression(statement) + terminate_statement());
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

            var intro = "public class " + render_dungeon_path(dungeon);
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

            if (config.explicit_public_members)
                main = "public " + main;

            return line(main + " = " + get_default_value(portal) + terminate_statement());
        }

        virtual protected string get_default_value(Portal portal)
        {
            if (portal.is_list)
                return instantiate_list(portal);

            return render_literal(portal.get_default_value(), portal.get_target_profession());
        }

        protected virtual string instantiate_list(Portal portal)
        {
            return "[]";
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

            switch (profession.type)
            {
                case Kind.unknown:
                    return value.ToString();

                case Kind.Float:
                    return value.ToString();

                case Kind.Int:
                    return value.ToString();

                case Kind.String:
                    return "\"" + value + "\"";

                case Kind.Bool:
                    return (bool)value ? "true" : "false";

                case Kind.reference:
                    if (!profession.dungeon.is_value)
                        throw new Exception("Literal expressions must be scalar values.");

                    if (profession.dungeon.GetType() == typeof (Treasury))
                        return render_enum_value((Treasury)profession.dungeon, (int)value);

                    if (value != null)
                        return value.ToString();

                    return render_dungeon_name(profession.dungeon) + "()";

                default:
                    throw new Exception("Invalid literal " + value + " type " + profession.type + ".");
            }
        }

        virtual protected string render_enum_value(Treasury treasury, int value)
        {
            return config.supports_enums
                ? treasury.name + get_connector(new Profession(Kind.reference, treasury)) + treasury.jewels[value]
                : value.ToString();
        }

        virtual protected string render_dungeon_name(IDungeon dungeon)
        {
            if (dungeon.realm != current_realm)
                return render_realm_name(dungeon.realm) + "." + dungeon.name;

            return dungeon.name;
        }

        virtual protected string render_realm_name(Realm realm)
        {
            var path = Generator.get_namespace_path(realm);
            return path.join(".");
        }

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
            var it = parameter.scope.create_symbol("it", parameter.profession);
            var expression = render_iterator(it, statement.expression);

            var result = add("for (" + expression + ")") + render_scope(new List<Expression> { 
                    new Declare_Variable(parameter, new Insert("*" + it.name))
                }.Concat(statement.body).ToList()
            );
            return result;
        }

        virtual protected string render_operation(Operation operation)
        {
            return operation.children.Select(c =>
                c.type == Expression_Type.operation && ((Operation)c).is_condition() == operation.is_condition()
                ? "(" + render_expression(c) + ")"
                : render_expression(c)
            ).join(" " + operation.op + " ");
        }

        virtual protected string render_iterator(Symbol parameter, Expression expression)
        {
            var path_string = render_expression(expression);
            return
                "::const_iterator " + parameter.name + " = "
                + path_string + ".begin(); " + parameter.name + " != "
                + path_string + ".end(); " + parameter.name + "++";
        }


        virtual protected string render_property_function_call(Property_Function_Call expression, Expression parent)
        {
            var ref_full = expression.reference != null
                ? render_expression(expression.reference) + "."
                : "";

            if (!config.implicit_this)
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
            var ref_string = expression.reference != null
               ? render_expression(expression.reference)
               : "";

            var ref_full = ref_string.Length > 0
                ? ref_string + "."
                : "";

            return ref_full + expression.get_name() + "(" +
                expression.args.Select(a => render_expression(a))
                .join(", ") + ")";
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

        virtual protected string render_instantiation(Instantiate expression)
        {
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
            current_minion = minion;
            var result = render_scope(() =>
               {
                   foreach (var parameter in minion.parameters)
                   {
                       current_scope[parameter.symbol.name] = parameter.symbol.profession;
                   }

                   return render_statements(minion.expressions);
               });

            current_minion = null;
            return result;
        }

        virtual protected string render_realm(Realm realm, String_Delegate action)
        {
            var space = Generator.get_namespace_path(realm);
            current_realm = realm;
            var result = add(config.namespace_keyword + " " + space.join(config.namespace_separator)) + render_scope(action);

            current_realm = null;
            return result;
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

        virtual protected string render_treasury(Treasury treasury)
        {
            var i = treasury.jewels.Count;
            return add("enum " + treasury.name) + render_scope(() =>
                treasury.jewels.Select(j => 
                    add(j + (--i > 0 ? "," : "")) + newline()).join("")
                );
        }

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
            var name = dungeon.name;
//            if (dungeon.realm.external_name != null)
//            {
//                name = dungeon.realm.external_name + config.namespace_separator + name;
//            }
//            else 
                if (dungeon.realm != current_realm || !config.supports_namespaces)
            {
                name = dungeon.realm.name + config.namespace_separator + name;
            }

            return name;
        }

        virtual protected string render_profession(Symbol symbol, bool is_parameter = false)
        {
            if (symbol.profession != null)
                return render_profession(symbol.profession, is_parameter);

            return render_profession(symbol.profession, is_parameter);
        }

        virtual protected string render_profession(Profession signature, bool is_parameter = false)
        {
            var name = signature.dungeon != null
                ? signature.dungeon.name
                : types[signature.type.ToString().ToLower()];

            return signature.is_list
                ? listify(name)
                : name;
        }

        virtual protected string listify(string type)
        {
            return type + "[]";
        }

        virtual protected string render_function_definition(Function_Definition definition)
        {
            if (definition.is_abstract)
                return "";

            var intro = (config.explicit_public_members ? "public " : "")
                + (definition.return_type != null ? render_profession(definition.return_type) + " " : "")
                + definition.name
                + "(" + definition.parameters.Select(render_definition_parameter).join(", ") + ")";

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
    }
}
