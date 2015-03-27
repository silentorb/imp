using System;
using System.Collections.Generic;
using System.Linq;
using imperative;
using imperative.render;
using imperative.schema;
using imperative.expressions;
using metahub.schema;

namespace metahub.render.targets
{
    public class External_Header
    {
        public string name;
        public bool is_standard;

        public External_Header(string name, bool is_standard = false)
        {
            this.name = name;
            this.is_standard = is_standard;
        }
    }

    public class Cpp : Common_Target
    {
        public Cpp(Overlord overlord = null)
            : base(overlord)
        {
            config = new Target_Configuration()
                {
                    statement_terminator = ";"
                };

            types["string"] = "std::string";
        }

        override public void run(string output_folder)
        {
            foreach (var dungeon in overlord.dungeons)
            {
                if (dungeon.is_external || (dungeon.is_abstract && dungeon.is_external))
                    continue;

                //Console.WriteLine(dungeon.realm.name + "." + dungeon.name);

                var space = Generator.get_namespace_path(dungeon.realm);
                var dir = output_folder + "/" + space.join("/");
                Generator.create_folder(dir);

                line_count = 0;
                Generator.create_file(dir + "/" + dungeon.name + ".cpp", create_header_file(dungeon));
                Generator.create_file(dir + "/" + dungeon.name + ".cpp", create_class_file(dungeon));
            }

            {
                var dir = output_folder + "/metahub";
                Generator.create_folder(dir);
                //Generator.create_file(dir + "/" + "list.h", Resources.list_h);
            }
        }

        override public void generate_dungeon_code(Dungeon dungeon)
        {
            var func = dungeon.spawn_minion(dungeon.name);
            func.return_type = null;
            func = dungeon.spawn_minion("~" + dungeon.name);
            func.return_type = null;
        }

        public override void generate_code2(Dungeon dungeon)
        {
            List<Portal> references = new List<Portal>();
            List<Portal> scalars = new List<Portal>();
            foreach (var portal in dungeon.core_portals.Values)
            {
                if (portal.type == Kind.reference && !portal.is_list && !portal.is_value)
                    references.Add(portal);
                else if (!portal.is_list)
                    scalars.Add(portal);
            }
            var block = references.Select(portal => 
                                          (Expression)new Assignment(new Portal_Expression(portal), "=", new Null_Value()))
                                  .Union(scalars.Select(portal => 
                                                        new Assignment(new Portal_Expression(portal), "=", 
                                                                       new Literal(portal.get_default_value(),
                                                                                   portal.get_profession())))
                );

            var func = dungeon.summon_minion(dungeon.name);
            func.add_to_block(block);
        }

        public string create_header_file(Dungeon dungeon)
        {
            List<External_Header> headers = new List<External_Header> { new External_Header("stdafx") }.Concat(
                dungeon.dependencies.Values.Where(d => !d.allow_partial)
                       .OrderBy(d => d.dungeon.source_file)
                       .Select(d => new External_Header(d.dungeon.source_file))
                ).ToList();

            render = new Renderer();
            var result = line("#pragma once")
                + render_includes(headers) + newline()
                + render_outer_dependencies(dungeon)
                + render_realm(dungeon.realm, () => newline() + render_inner_dependencies(dungeon) + class_declaration(dungeon));

            return result;
        }

        public string create_class_file(Dungeon dungeon)
        {
            scopes = new List<Dictionary<string, Profession>>();
            List<External_Header> headers = new List<External_Header> { new External_Header("stdafx") }.Concat(
                new List<External_Header> { new External_Header(dungeon.source_file) }.Concat(
                    dungeon.dependencies.Values.Where(d => d.dungeon != dungeon.parent && d.dungeon.source_file != null)
                           .Select(d => new External_Header(d.dungeon.source_file)))
                                                                                      .OrderBy(h => h.name)
                ).ToList();

            //foreach (var d in dungeon.dependencies.Values)
            //{
            //    var dependency = d.dungeon;
            //    if (dependency != dungeon.parent && dependency.source_file != null)
            //    {
            //        headers.Add(new External_Header(dependency.source_file));
            //    }
            //}

            foreach (var func in dungeon.used_functions.Values)
            {
                if (func.name == "rand" && func.is_platform_specific)
                {
                    if (!has_header(headers, "stdlib"))
                        headers.Add(new External_Header("stdlib", true));
                }
            }

            render = new Renderer();
            return render_includes(headers) + newline()
                         + render_statements(dungeon.code);

        }
//
//        string render_statement(Expression statement)
//        {
//            Expression_Type type = statement.type;
//            switch (type)
//            {
//                case Expression_Type.space:
//                    var space = (Namespace)statement;
//                    return render_realm(space.realm, () => render_statements(space.body));
//
//                case Expression_Type.class_definition:
//                    var definition = (Class_Definition)statement;
//                    return class_definition(definition.dungeon, definition.body);
//
//                case Expression_Type.function_definition:
//                    return render_function_definition((Function_Definition)statement);
//
//                case Expression_Type.flow_control:
//                    return render_flow_control((Flow_Control)statement);
//
//                case Expression_Type.iterator:
//                    return render_iterator_block((Iterator)statement);
//
//                    //case Expression_Type.function_call:
//                    //    return line(render_function_call((Class_Function_Call)statement, null) + ";");
//
//                    //case Expression_Type.platform_function:
//                    //    return line(render_platform_function_call((Platform_Function)statement, null) + ";");
//
//                case Expression_Type.assignment:
//                    return render_assignment((Assignment)statement);
//
//                case Expression_Type.comment:
//                    return line(render_comment((Comment)statement));
//
//                case Expression_Type.declare_variable:
//                    return render_variable_declaration((Declare_Variable)statement);
//
//                case Expression_Type.statement:
//                    var state = (Statement)statement;
//                    return line(state.name + (state.next != null
//                                                  ? " " + render_expression(state.next)
//                                                  : "") + ";");
//
//                case Expression_Type.insert:
//                    return line(((Insert)statement).code);
//
//                default:
//                    return line(render_expression(statement) + ";");
//                    //throw new Exception("Unsupported statement type: " + statement.type + ".");
//            }
//        }

//        string render_variable_declaration(Declare_Variable declaration)
//        {
//            var profession = declaration.symbol.get_profession(overlord);
//            var first = render_signature(profession) + " " + declaration.symbol.name;
//            if (declaration.expression != null)
//                first += " = " + render_expression(declaration.expression);
//
//            current_scope[declaration.symbol.name] = profession;
//            return line(first + ";");
//        }

        private class Temp
        {
            public Realm realm;
            public List<IDungeon> dependencies;
        }

        string render_outer_dependencies(Dungeon dungeon)
        {
            bool lines = false;
            var result = "";
            Dictionary<string, Temp> realms = new Dictionary<string, Temp>();

            foreach (var d in dungeon.dependencies.Values)
            {
                var dependency = d.dungeon;
                if (d.allow_partial && dependency.realm != dungeon.realm)
                {
                    if (!realms.ContainsKey(dependency.realm.name))
                    {
                        realms[dependency.realm.name] = new Temp
                            {
                                realm = dependency.realm,
                                dependencies = new List<IDungeon>()
                            };
                    }
                    realms[dependency.realm.name].dependencies.Add(dependency);
                    lines = true;
                }
            }

            foreach (var r in realms.Values)
            {
                result += render_realm(r.realm, () => r.dependencies.Select(d => line("class " + d.name + ";"))
                                                       .join("")
                    );
            }

            if (result.Length > 0)
                result += newline();

            return result;
        }

        string render_inner_dependencies(Dungeon dungeon)
        {
            bool lines = false;
            var result = "";
            foreach (var d in dungeon.dependencies.Values)
            {
                var dependency = d.dungeon;
                if (d.allow_partial && dependency.realm == dungeon.realm)
                {
                    result += line("class " + render_dungeon_path(dependency) + ";");
                    lines = true;
                }
            }

            if (result.Length > 0)
                result += newline();

            return result;
        }

        static List<Dungeon> get_dungeon_parents(Dungeon dungeon)
        {
            var parents = new List<Dungeon>();
            if (dungeon.parent != null)
                parents.Add(dungeon.parent);

            return parents.Concat(dungeon.interfaces).ToList();
        }

        string class_declaration(Dungeon dungeon)
        {
            current_dungeon = dungeon;
            var result = "";
            var first = "class ";
            if (dungeon.class_export.Length > 0)
                first += dungeon.class_export + " ";

            first += dungeon.name;
            var parents = get_dungeon_parents(dungeon);

            if (parents.Count > 0)
            {
                first += " : " + parents.Select(p => "public " + render_trellis_name(p)).join(", ");
            }

            result = line(first + " {")
                     + "public:" + newline();
            indent();

            foreach (var portal in dungeon.core_portals.Values)
            {
                result += property_declaration(portal);
            }

            foreach (var portal in dungeon.all_portals.Values.Except(dungeon.core_portals.Values))
            {
                if (portal.dungeon.is_abstract)
                    result += property_declaration(portal);
            }

            result += pad(render_function_declarations(dungeon))
                      + unindent().line("};");

            return result;
        }

        override protected string render_dungeon(Dungeon dungeon, IEnumerable<Expression> statements)
        {
            var result = "";

            //result += pad(render_functions(trellis));
            result += newline() + render_statements(statements, newline());
            unindent();

            return result;
        }

        override protected string render_realm(Realm realm, String_Delegate action)
        {
            var space = Generator.get_namespace_path(realm);
            var result = line("namespace " + space.join("::") + " {");
            current_realm = realm;
            indent();
            result += action() + unindent().line("}");

            current_realm = null;
            return result;
        }

        string render_trellis_name(IDungeon dungeon)
        {
            if (dungeon.realm != current_realm)
                return render_realm_name(dungeon.realm) + "::" + dungeon.name;

            return dungeon.name;
        }

//        string render_realm_name(Realm realm)
//        {
//            var path = Generator.get_namespace_path(realm);
//            return path.join("::");
//        }

       override protected string render_function_definition(Function_Definition definition)
        {
            if (definition.is_abstract)
                return "";

            var intro = (definition.return_type != null ? render_profession(definition.return_type) + " " : "")
                        + current_dungeon.name + "::" + definition.name
                        + "(" + definition.parameters.Select(render_definition_parameter).join(", ") + ")";

            return render_scope(intro, () =>
                {
                    foreach (var parameter in definition.parameters)
                    {
                        current_scope[parameter.symbol.name] = parameter.symbol.profession;
                    }

                    return render_statements(definition.expressions);
                });
        }

        string render_declaration_parameter(Parameter parameter)
        {
            return render_profession(parameter.symbol, true) + " " + parameter.symbol.name
                   + (parameter.default_value != null
                          ? " = " + render_expression(parameter.default_value)
                          : ""
                     );
        }

        string render_function_declarations(Dungeon dungeon)
        {
            var declarations = dungeon.stubs.Select(line).ToList();

            if (dungeon.hooks.ContainsKey("initialize_post"))
            {
                declarations.Add(line("void initialize_post(); // Externally defined."));
            }

            declarations.AddRange(dungeon.minions.Values.Select(render_function_declaration));

            return declarations.join("");
        }

        static bool has_header(IEnumerable<External_Header> list, string name)
        {
            return list.Any(header => header.name == name);
        }

        string property_declaration(Portal portal)
        {
            return line(render_profession(portal.get_profession()) + " " + portal.name + ";");
        }

        string render_includes(IEnumerable<External_Header> headers)
        {
            return headers
                .Select(h => line(h.is_standard
                                      ? "#include <" + h.name + ".h>"
                                      : "#include \"" + h.name + ".h\""
                                 )).join("");
        }

        string render_function_declaration(Minion definition)
        {
            return line((definition.return_type != null ? "virtual " : "")
                        + (definition.return_type != null ? render_profession(definition.return_type) + " " : "")
                        + definition.name
                        + "(" + definition.parameters.Select(render_declaration_parameter).join(", ") + ")"
                        + (definition.is_abstract ? " = 0;" : ";"));
        }

       override protected string render_profession(Profession signature, bool is_parameter = false)
        {
            if (signature.dungeon == null)
            {
                var name = signature.type == Kind.reference
                               ? "void*"
                               : types[signature.type.ToString().ToLower()];
                
                return signature.is_list
                    ? "std::vector<" + name + ">"
                    : name;
            }
            else if (signature.dungeon.GetType() == typeof (Dungeon))
            {
                var dungeon = (Dungeon) signature.dungeon;
                var name = dungeon.is_abstract
                    ? "void"
                    : render_dungeon_path(signature.dungeon);

                if (!signature.is_list)
                {
                    return
                        dungeon.is_value
                            ? is_parameter ? name + "&" : name
                            : name + "*";
                }
                else
                {
                    return "std::vector<" + (dungeon.is_value
                        ? name
                        : name + "*")
                           + ">";
                }
            }
            else
            {
//                var treasury = (Treasury) signature.dungeon;
                return render_dungeon_path(signature.dungeon);
            }
        }

//        string render_symbol_signature(Symbol symbol, bool is_parameter = false)
//        {
//            if (symbol.profession != null)
//                return render_signature(symbol.profession, is_parameter);
//
//            return render_signature(symbol.profession, is_parameter);
//        }

//        override protected string render_profession(Profession signature, bool is_parameter = false)
//        {
//            if (signature.dungeon == null)
//            {
//                return signature.type == Kind.reference
//                           ? "void*"
//                           : types[signature.type.ToString().ToLower()];
//            }
//
//            var name = signature.dungeon.is_abstract && !signature.dungeon.is_value
//                           ? "void"
//                           : render_dungeon_path(signature.dungeon);
//
//            if (!signature.is_list)
//            {
//                return signature.dungeon.is_value ? is_parameter ? name + "&" : name :
//                           name + "*";
//            }
//
//            var name2 = signature.dungeon.is_value
//                            ? name
//                            : name + "*";
//
//            return "std::vector<" + name2 + ">";
//        }

        public string render_block(string command, string expression, String_Delegate action)
        {
            var result = line(command + " (" + expression + ") {");
            indent();
            result += action();
            unindent();
            result += line("}");
            return result;
        }

        public string render_scope(string intro, String_Delegate action)
        {
            push_scope();
            var result = line(intro + " {");
            indent();
            result += action();
            unindent();
            result += line("}");
            pop_scope();
            return result;
        }

        public string render_scope2(string intro, List<Expression> statements, bool minimal = false)
        {
            indent();
            push_scope();
            var lines = line_count;
            var block = render_statements(statements);
            pop_scope();
            unindent();

            if (minimal)
            {
                minimal = line_count == lines + 1;
            }
            var result = line(intro + (minimal ? "" : " {"));
            result += block;
            result += line((minimal ? "" : "}"));
            return result;
        }

        string render_flow_control(Flow_Control statement)
        {
            var expression = render_expression(statement.condition);

            return render_scope2(
                statement.flow_type.ToString().ToLower() + " (" + expression + ")"
                , statement.body, statement.flow_type == Flow_Control_Type.If);
        }

        string render_iterator_block(Iterator statement)
        {
            var parameter = statement.parameter;
            var it = parameter.scope.create_symbol("it", parameter.profession);
            var expression = render_iterator(it, statement.expression);

            var result = render_scope2(
                "for (" + expression + ")"
                , new List<Expression> { 
                    new Declare_Variable(parameter, new Insert("*" + it.name))
                }.Concat(statement.body).ToList()
                );
            return result;
        }

        string render_operation(Operation operation)
        {
            return operation.children.Select(c =>
                                             c.type == Expression_Type.operation && ((Operation)c).is_condition() == operation.is_condition()
                                                 ? "(" + render_expression(c) + ")"
                                                 : render_expression(c)
                ).join(" " + operation.op + " ");
        }

        string render_iterator(Symbol parameter, Expression expression)
        {
            var signature = expression.get_end().get_profession();
            var path_string = render_expression(expression);
            return
                render_profession(signature)
                + "::const_iterator " + parameter.name + " = "
                + path_string + ".begin(); " + parameter.name + " != "
                + path_string + ".end(); " + parameter.name + "++";
        }

        string render_expression(Expression expression, Expression parent = null)
        {
            string result;
            switch (expression.type)
            {
                case Expression_Type.literal:
                    return render_literal((Literal)expression);

                case Expression_Type.operation:
                    return render_operation((Operation)expression);

                case Expression_Type.portal:
                    var portal_expression = (Portal_Expression)expression;
                    result = portal_expression.portal.name;
                    if (portal_expression.index != null)
                        result += "[" + render_expression(portal_expression.index) + "]";

                    break;

                case Expression_Type.function_call:
                    result = render_function_call((Method_Call)expression, parent);
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
                    result = "this";
                    break;

                case Expression_Type.null_value:
                    return "NULL";

                case Expression_Type.create_array:
                    result = "FOOO";
                    break;

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

                default:
                    throw new Exception("Unsupported Expression type: " + expression.type + ".");
            }

            if (expression.next != null)
            {
                result += get_connector(expression) + render_expression(expression.next, expression);
            }

            return result;
        }

        string render_literal(Literal expression)
        {
            var signature = expression.profession;
            if (signature == null)
                return expression.value.ToString();

            switch (signature.type)
            {
                case Kind.unknown:
                    return expression.value.ToString(); ;

                case Kind.Float:
                    var result = expression.value.ToString();
                    return result.Contains(".")
                               ? result + "f"
                               : result;

                case Kind.Int:
                    return expression.value.ToString();

                case Kind.String:
                    return "\"" + expression.value + "\"";

                case Kind.Bool:
                    return (bool)expression.value ? "true" : "false";

                case Kind.reference:
//                    if (!signature.dungeon.is_value)
//                        throw new Exception("Literal expressions must be scalar values.");

                    if (expression.value != null)
                        return expression.value.ToString();

                    return render_trellis_name(signature.dungeon) + "()";

                default:
                    throw new Exception("Invalid literal " + expression.value + " type " + expression.profession.type + ".");
            }
        }

        bool is_pointer(Profession signature)
        {
            if (signature.type == null)
                throw new Exception();

            if (signature.dungeon == null)
                return false;

            return !signature.dungeon.is_value && !signature.is_list;
        }

        override protected string get_connector(Expression expression)
        {
            if (expression.type == Expression_Type.parent_class)
                return "::";

            if (expression.type == Expression_Type.portal && ((Portal_Expression)expression).index != null)
                return "->";

            var profession = expression.get_profession();
            return profession == null
                       ? is_pointer(expression.get_profession()) ? "->" : "."
                       : is_pointer(profession) ? "->" : ".";
        }

        override protected string get_connector(Profession profession)
        {
            return is_pointer(profession) ? "->" : ".";
        }

        string render_instantiation(Instantiate expression)
        {
            return "new " + render_profession(expression.profession) + "()";
        }

        override protected string render_property_function_call(Property_Function_Call expression, Expression parent)
        {
            var ref_full = expression.reference != null
                               ? render_expression(expression.reference) + get_connector(expression.reference)
                               : "";

            var args = expression.args.Select(e => render_expression(e)).join(", ");
            var portal = expression.portal;
            var setter = portal.setter;
            if (setter != null)
                return ref_full + setter.name + "(" + args + ")";

            return expression.portal.is_list
                       ? ref_full + portal.name + get_connector(expression.portal.profession) + "push_back(" + args + ")"
                       : ref_full + portal.name + " = " + args;
        }

        override protected string render_platform_function_call(Platform_Function expression, Expression parent)
        {
            var ref_string = expression.reference != null
                                 ? render_expression(expression.reference)
                                 : "";

            var ref_full = ref_string.Length > 0
                               ? ref_string + get_connector(expression.reference.get_end())
                               : "";

            switch (expression.name)
            {
                case "count":
                    return ref_full + "size()";

                case "add":
                    {
                        var first = render_expression(expression.args[0]);
                        //var dereference = is_pointer(expression.args.Last().get_signature()) ? "*" : "";
                        return ref_full + "push_back(" + first + ")";
                    }

                case "contains":
                    {
                        var first = render_expression(expression.args[0]);
                        return "std::find(" + ref_full + "begin(), "
                               + ref_full + "end(), " + first + ") != " + ref_full + "end()";
                    }

                case "distance":
                    {
                        //var signature = expression.args[0].get_signature();
                        var first = render_expression(expression.args[0]);
                        //var dereference = is_pointer(signature) ? "*" : "";
                        return ref_full + "distance(" + first + ")";
                    }

                case "first":
                    return "[0]";

                case "last":
                    return ref_full + "back()";

                case "pop":
                    return ref_full + "pop_back()";

                case "remove":
                    {
                        var first = render_expression(expression.args[0]);
                        return ref_full + "erase(std::remove(" + ref_full + "begin(), "
                               + ref_full + "end(), " + first + "), " + ref_full + "end())";
                    }

                case "rand":
                    float min = ((Literal)expression.args[0]).get_float();
                    float max = ((Literal)expression.args[1]).get_float();
                    return "rand() % " + (max - min) + (min < 0 ? " - " + -min : " + " + min);

                default:
                    throw new Exception("Unsupported platform-specific function: " + expression.name + ".");
            }
        }

        string render_function_call(Method_Call expression, Expression parent)
        {
            var ref_string = expression.reference != null
                                 ? render_expression(expression.reference)
                                 : "";

            var ref_full = ref_string.Length > 0
                               ? ref_string + get_connector(expression.reference.get_end())
                               : "";

            return ref_full + expression.name + "(" +
                   expression.args.Select(a => render_expression(a))
                             .join(", ") + ")";
        }

        string render_assignment(Assignment statement)
        {
            return line(render_expression(statement.target) + " " + statement.op + " " + render_expression(statement.expression) + ";");
        }

        string render_comment(Comment comment)
        {
            return comment.is_multiline
                       ? "/* " + comment.text + "*/"
                       : "// " + comment.text;
        }
    }

}