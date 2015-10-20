using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.legion;
using imperative.schema;
using imperative.scholar;
using imperative.scholar.crawling;
using metahub.render;
using metahub.render.targets;

namespace imperative.render.artisan.targets.cpp
{
    public class Cpp : Common_Target2
    {
        public static Target_Configuration static_config;

        public static Statement_Router statement_router = new Statement_Router
        {
            render_function_definition = Source_File.render_function_definition
        };

        public Cpp(Overlord overlord = null)
            : base(overlord)
        {
            static_config = config = new Target_Configuration()
             {
                 float_suffix = true,
                 statement_terminator = ";",
                 dependency_keyword = "using",
                 space_tabs = true,
                 indent = 4,
                 block_brace_same_line = false,
                 explicit_public_members = false,
                 type_mode = Type_Mode.required_prefix,
                 namespace_separator = "::",
                 list_start = "{",
                 list_end = "}"
             };

            types["string"] = "std::string";
            minion_names[Professions.List.minions_old["push"]] = "push_back";
        }

        public override void run(Build_Orders config1, string[] sources)
        {
            if (config1.name != "" && config1 is Project)
                CMake.create_files((Project)config1, overlord);

            foreach (var dungeon in config1.dungeons.Values)
            {
                prepare_dungeon(dungeon);
            }

            foreach (var dungeon in config1.dungeons.Values)
            {
                if (dungeon.is_external || (dungeon.is_abstract && dungeon.is_external))
                    continue;

                render_full_dungeon(dungeon, config1);
                //Console.WriteLine(dungeon.realm.name + "." + dungeon.name);
            }
        }

        public override void build_wrapper_project(Project project)
        {
            //            var dependencies = CMake.get_project_dependencies(project);
            CMake.create_wrapper_cmakelists_txt(project);
        }

        public void render_full_dungeon(Dungeon dungeon, Build_Orders config1)
        {
            if (dungeon.portals.Length > 0 || dungeon.minions_old.Count > 0)
            {
                var space = Generator.get_namespace_path(dungeon.realm);
                var dir = config1.output + "/" + space.join("/");
                Generator.create_folder(dir);

                var name = dir + "/" + dungeon.name;

                Generator.create_file(name + ".h",
                    Overlord.stroke_to_string(Header_File.generate_header_file(this, dungeon)));

                if (!dungeon.is_enum)
                {
                    Generator.create_file(name + ".cpp",
                        Overlord.stroke_to_string(Source_File.generate_source_file(this, dungeon)));
                }
            }

            //            foreach (var child in dungeon.dungeons.Values)
            //            {
            //                render_full_dungeon(child, config1);
            //            }
        }

        public static void prepare_dungeon(Dungeon dungeon)
        {
            prepare_constructor(dungeon);
            determine_dungeon_ownership(dungeon);
            determine_cpp_types(dungeon);
        }

        public static void determine_dungeon_ownership(Dungeon dungeon)
        {
            if (dungeon.has_minion("constructor"))
            {
                var constructor = dungeon.minions_old["constructor"];
                Crawler.analyze_expressions(constructor.expressions, expression =>
                {
                    if (expression.type == Expression_Type.assignment)
                    {
                        var assignment = (Assignment)expression;
                        var target_end = assignment.target.get_end();
                        if (target_end.type == Expression_Type.portal
                            && assignment.expression.type == Expression_Type.variable
                            && is_pointer_or_shared(assignment.expression.get_profession()))
                        {
                            var portal = ((Portal_Expression)target_end).portal;
                            var variable = (Variable)assignment.expression;
                            var parameter = constructor.parameters.FirstOrDefault(p => p.symbol == variable.symbol);
                            if (parameter != null)
                            {
                                portal.is_owner = false;
                                parameter.symbol.profession = portal.profession =
                                    portal.profession.change_cpp_type(Cpp_Type.pointer);
                            }
                        }
                    }
                });
            }
        }

        public static void determine_cpp_types(Dungeon dungeon)
        {
            Crawler.analyze_minions(dungeon, expression =>
            {
                switch (expression.type)
                {
                    case Expression_Type.declare_variable:
                        var variable_declaration = (Declare_Variable)expression;
                        var profession = variable_declaration.symbol.profession;
                        if (is_shared_pointer(profession) && profession.cpp_type != Cpp_Type.shared_pointer)
                        {
//                            variable_declaration.symbol.profession = profession.change_cpp_type(Cpp_Type.shared_pointer);
                        }
                        break;

                    //                    case Expression_Type.self:
                    //                        var self = (Self) expression;
                    //                        self.
                }
            });
        }

        public static void prepare_constructor(Dungeon dungeon)
        {
            if (dungeon.has_minion("constructor"))
            {
                foreach (var minion in dungeon.minions_more["constructor"])
                {
                    minion.return_type = null;
                }
                return;
            }

            var expressions = new List<Expression>();
            foreach (var portal in dungeon.portals.Where(p => !p.has_enchantment(Enchantments.Static)))
            {
                if (portal.default_expression != null)
                {
                    var assignment = new Assignment(new Portal_Expression(portal), "=", portal.default_expression);
                    expressions.Insert(0, assignment);
                }
            }

            if (expressions.Count == 0)
                return;

            var constructor = dungeon.spawn_minion("constructor", new List<Parameter>());
            constructor.return_type = null;
            constructor.expressions = expressions;
        }

        public List<Stroke> generate_source_strokes()
        {
            var output = new List<Stroke>();
            foreach (var dungeon in overlord.dungeons)
            {
                if (dungeon.is_external || dungeon.is_external
                    || dungeon.name == "")
                    continue;

                output.Add(Source_File.generate_source_file(this, dungeon));
            }

            return output;
        }

        public static List<Profession> get_dungeon_parents(Dungeon dungeon)
        {
            var parents = new List<Profession>();
            if (dungeon.parent != null)
                parents.Add(dungeon.parent);

            return parents.Concat(dungeon.interfaces).ToList();
        }

        public override Stroke render_profession(Profession signature, bool is_parameter = false)
        {
            var context = new Render_Context(current_dungeon.realm, Cpp.static_config,
                Cpp.statement_router, this);

            return render_profession2(signature, context, is_parameter);
        }

        public static bool is_pointer_or_shared(Profession signature)
        {
            return !signature.dungeon.is_value && !signature.is_generic_parameter;
        }

        public static bool is_shared_pointer(Profession signature, bool is_type = false)
        {
            if (signature.cpp_type != Cpp_Type.none && signature.cpp_type != Cpp_Type.shared_pointer)
                return false;

            return !signature.dungeon.is_value && !signature.is_generic_parameter && !is_type;
        }

        public static bool is_shared_pointer(Expression expression, bool is_type = false)
        {
            var end = expression.get_end();
            if (end.type != Expression_Type.portal && end.type != Expression_Type.variable)
                return false;

            if (end.type == Expression_Type.portal)
            {
                var portal = ((Portal_Expression)end).portal;
                if (!portal.is_owner)
                    return false;
            }

            var profession = expression.get_profession();
            return is_shared_pointer(profession, is_type);
        }

        public static Stroke render_profession2(Profession signature, Render_Context context,
            bool is_parameter = false, bool is_type = false)
        {
            if (signature.dungeon == Professions.List)
                return listify2(render_profession2(signature.children[0], context), signature);

            if (signature.dungeon == Professions.Dictionary)
                return render_dictionary_profession(signature, context);

            var lower_name = signature.dungeon.name.ToLower();

            var name = types.ContainsKey(lower_name)
                ? new Stroke_Token(types[lower_name])
                : render_dungeon_path2(signature.dungeon, context);

            if (signature.children != null && signature.children.Count > 0)
            {
                name += new Stroke_Token("<")
                    + Stroke.join(signature.children.Select(p => render_profession2(p, context)), ", ")
                    + new Stroke_Token(">");
            }

            if (signature.cpp_type == Cpp_Type.pointer)
            {
                name = name + new Stroke_Token("*");
            }
            else if (is_shared_pointer(signature, is_type))
            {
                name = name + new Stroke_Token("*");
//                name = shared_pointer(name);
            }
            //            if (is_parameter && !signature.dungeon.is_value)
            //                name += new Stroke_Token("&");

            return name;
        }

        public static Stroke render_dictionary_profession(Profession profession, Render_Context context)
        {
            return new Stroke_Token("std::map<")
                + render_profession2(profession.children[0], context)
                +new Stroke_Token(", ")
                + render_profession2(profession.children[1], context)                
                + new Stroke_Token(">");
        }

        public static Stroke shared_pointer(Stroke stroke)
        {
            return new Stroke_Token("std::shared_ptr<") + stroke + new Stroke_Token(">");
        }

        public static Stroke render_dungeon_path2(IDungeon dungeon, Render_Context context)
        {
            return dungeon.realm != null && dungeon.realm.name != ""
                && (dungeon.realm != context.realm)
                ? render_dungeon_path2(dungeon.realm, context) + new Stroke_Token("::" + dungeon.name)
                : new Stroke_Token(dungeon.name);
        }

        public static Stroke render_definition_parameter2(Parameter parameter, Render_Context context)
        {
            return Cpp.render_profession2(parameter.symbol.profession, context, true) + new Stroke_Token(" " + parameter.symbol.name);
        }

        override protected Stroke render_portal(Portal_Expression portal_expression)
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
                    result = render_this() + new Stroke_Token("->") + result;
                }
            }
            if (portal_expression.index != null)
                result += new Stroke_Token("[") + render_expression(portal_expression.index) + new Stroke_Token("]");

            return result;
        }


        public static Stroke render_includes(IEnumerable<External_Header> headers)
        {
            return new Stroke_List(Stroke_Type.statements, headers
                .Select(render_include).ToList()) { margin_bottom = 1 };
        }

        public static string render_source_path(Dungeon dungeon)
        {
            return Common_Functions.render_realm_path_string(dungeon, "/");
        }

        public static Stroke render_include(External_Header header)
        {
            if (string.IsNullOrEmpty(header.name))
                throw new Exception("Empty header file name");

            return new Stroke_Token(header.is_standard
                ? "#include <" + header.name + ">"
                : "#include \"" + header.name + ".h\""
                );
        }

        public static bool has_header(IEnumerable<External_Header> list, string name)
        {
            return list.Any(header => header.name == name);
        }

        override public Stroke listify(Stroke type, Profession signature)
        {
            return new Stroke_Token("std::vector<") + type + new Stroke_Token(">");
        }

        public static Stroke listify2(Stroke type, Profession signature)
        {
            return new Stroke_Token("std::vector<") + type + new Stroke_Token(">");
        }

        protected override Stroke render_platform_function_call(Platform_Function expression, Expression parent)
        {
            throw new Exception("Not implemented.");
        }

        override protected Stroke render_variable_declaration(Declare_Variable declaration)
        {
            var context = new Render_Context(current_realm, config, statement_router, this);
            Stroke start = null;
            bool convert = false;
            if (declaration.expression != null)
            {
                if (declaration.symbol.profession == declaration.expression.get_profession()
                    || declaration.symbol.profession == Professions.none)
                {
                    start = new Stroke_Token("auto");
                }
                else
                {
                    start = render_profession2(declaration.symbol.profession, context);
                    convert = true;
                }
            }
            else
            {
                start = render_profession2(declaration.symbol.profession, context);
            }

            var result = start + new Stroke_Token(" " + declaration.symbol.name);

            if (declaration.expression != null)
            {
                result += new Stroke_Token(" = ");
                if (convert)
                {
                    result += new Stroke_Token("(")
                              + render_profession2(declaration.symbol.profession, context)
                              + new Stroke_Token(")");
                }

                result += render_expression(declaration.expression);
            }
              
            result    += terminate_statement();

            result.expression = declaration;
            return result;
        }

        override protected Stroke render_instantiation(Instantiate expression)
        {
            if (expression.profession.dungeon == Professions.List)
                return render_list(expression.profession, expression.args);

            var args = expression.args.Count > 0
                ? render_arguments(expression.args, expression.profession.dungeon.minions_old["constructor"].parameters)
                : new Stroke_Token();

            var context = new Render_Context(current_realm, config, statement_router, this);

//            if (is_shared_pointer(expression.profession))
//            {
//                return render_profession(expression.profession)
//                + new Stroke_Token("(new ")
//                + Cpp.render_dungeon_path2(expression.profession.dungeon, context)
//                + new Stroke_Token("(")
//                + args
//                + new Stroke_Token("))");
//            }

            var result = Cpp.render_dungeon_path2(expression.profession.dungeon, context)
                + new Stroke_Token("(")
                + args
                + new Stroke_Token(")");

            if (!expression.profession.dungeon.is_value)
                result = new Stroke_Token("new ") + result;

            return result;
        }

        public Stroke render_realm2(Dungeon realm, Stroke_List_Delegate action)
        {
            if (realm == null || realm.name == "")
                return new Stroke_Token() + action();

            Stroke_Delegate block = () =>
            {
                current_realm = realm;
                var result = new Stroke_Token(config.namespace_keyword + " ")
                         + render_realm_path(realm, config.namespace_separator)
                         + render_block(action(), false);

                current_realm = null;
                return result;
            };

            if (realm.realm != null)
            {
                return render_realm(realm.realm, () => new List<Stroke> { block() });
            }

            return block();
        }

        protected override Stroke render_null()
        {
            return new Stroke_Token("NULL");
        }

        bool is_pointer(Profession profession)
        {
            if (profession.dungeon == Professions.List || profession.dungeon == Professions.Dictionary)
                return false;

            if (profession.cpp_type != Cpp_Type.pointer && profession.dungeon.is_value)
                return false;

            return true;
        }

        override protected Stroke get_connector(Expression expression)
        {
            if (expression.type == Expression_Type.parent_class)
                return new Stroke_Token("::");

            //            if (expression.type == Expression_Type.portal && ((Portal_Expression)expression).index != null)
            //                return new Stroke_Token("->");

            var profession = expression.get_profession();
            return new Stroke_Token(is_pointer(profession) ? "->" : ".");
        }

        override protected string get_connector(Profession profession)
        {
            return is_pointer(profession) ? "->" : ".";
        }

        override protected Stroke render_iterator_block(Iterator statement)
        {
            var parameter = statement.parameter;
            //            var it = parameter.scope.create_symbol(parameter.name, parameter.profession);
            var expression = render_iterator(parameter, statement.expression);

            var result = new Stroke_Token(config.foreach_symbol + " (") + expression + new Stroke_Token(")")
                + render_block(render_statements(statement.body));

            result.expression = statement;
            return result;
        }

        override protected Stroke render_iterator(Symbol parameter, Expression expression)
        {
            var path_string = render_expression(expression);
            return new Stroke_Token("auto &" + parameter.name + " : ") + path_string;
        }

        Stroke wrap_pointer(Dungeon dungeon, Expression expression)
        {
            return shared_pointer(new Stroke_Token(dungeon.name))
                    + new Stroke_Token("(")
                    + render_expression(expression)
                    + new Stroke_Token(")");
        }

        protected static Stroke dereference_shared_pointer()
        {
            return new Stroke_Token("&*");
        }

        protected override Stroke render_argument(Expression expression, Parameter parameter)
        {
            if (expression.get_profession().cpp_type != Cpp_Type.shared_pointer
                && parameter.symbol.profession.cpp_type == Cpp_Type.shared_pointer)
            {
//                return wrap_pointer(current_dungeon, expression);
            }

            if (expression.type == Expression_Type.self && parameter.symbol.profession.is_generic_parameter)
            {
//                return wrap_pointer(current_dungeon, expression);
            }

            if (expression.get_profession().cpp_type == Cpp_Type.shared_pointer
              && parameter.symbol.profession.cpp_type == Cpp_Type.pointer)
            {
//                return dereference_shared_pointer() + base.render_argument(expression, parameter);
            }

            return base.render_argument(expression, parameter);
        }

        override protected Stroke render_operation_part(Expression expression)
        {
            var result = render_expression(expression);
            var end = expression.get_end();
//            if (is_shared_pointer(end))
//                result = dereference_shared_pointer() + result;

            return result;
        }

        override protected Stroke render_assignment(Assignment statement)
        {
            var value = /*statement.expression.get_end().type == Expression_Type.self
                ? wrap_pointer(current_dungeon, statement.expression)
                : */render_expression(statement.expression);

            var result = render_expression(statement.target)
                + new Stroke_Token(" " + statement.op + " ")
                + value
                + terminate_statement();

            result.expression = statement;
            return result;
        }

    }
}
