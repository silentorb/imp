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
                Preparation.prepare_dungeon(dungeon);
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

            return Utility.render_profession2(signature, context, is_parameter);
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

        override protected Stroke render_assignment(Assignment statement)
        {
            var is_owner = false;
            var target_end = statement.target.get_end();
            if (target_end.type == Expression_Type.variable)
            {
                var variable = (Variable)target_end;
                is_owner = variable.symbol.is_owner;
            }

            var assignment = render_assignment_expression(statement.target.get_end().get_profession(),
                statement.expression, is_owner);

            var result = render_expression(statement.target)
                + new Stroke_Token(" " + statement.op + " ")
                + assignment.expression
                + terminate_statement();

            if (assignment.pre_statement != null)
                result = assignment.pre_statement + result;

            result.expression = statement;
            return result;
        }

        class Assignment_Expression_Result
        {
            public Stroke expression;
            public Stroke pre_statement;
        }

        private static int next_unique_pointer_id = 1;

        Assignment_Expression_Result render_assignment_expression(Profession target_profession, Expression expression, bool target_is_owner)
        {
            var value = render_expression(expression);
            Stroke pre = null;

            var expression_end = expression.get_end();
            var expression_profession = expression_end.get_profession();

            if (target_is_owner
                && expression_end.type == Expression_Type.instantiate
                && Cpp.is_pointer(expression_profession))
            {
                var pointer_name = "_unique_" + next_unique_pointer_id++;

                var context = new Render_Context(current_dungeon.realm, Cpp.static_config, Cpp.statement_router, this);
                var stroke = (Stroke_List)Utility.render_profession2(expression_profession, context);
                stroke.children.RemoveAt(stroke.children.Count - 1);
                pre = new Stroke_Token("std::unique_ptr<") + stroke + new Stroke_Token("> " + pointer_name + "(")
                    + render_expression(expression) + new Stroke_Token(")")
                    + terminate_statement();

                value = new Stroke_Token("&*" + pointer_name);
            }

            if (target_profession != expression_profession && target_profession != Professions.none)
            {
                var context = new Render_Context(current_realm, config, statement_router, this);
                value = new Stroke_Token("(")
                        + Utility.render_profession2(target_profession, context)
                        + new Stroke_Token(")")
                        + value;
            }

            return new Assignment_Expression_Result
            {
                expression = value,
                pre_statement = pre
            };
        }

        override protected Stroke render_variable_declaration(Declare_Variable declaration)
        {
            var context = new Render_Context(current_realm, config, statement_router, this);
            Stroke start = null;

            if (declaration.expression != null)
            {
                if (declaration.symbol.profession == declaration.expression.get_profession()
                    || declaration.symbol.profession == Professions.none)
                {
                    start = new Stroke_Token("auto");
                }
                else
                {
                    start = Utility.render_profession2(declaration.symbol.profession, context);
                }
            }
            else
            {
                start = Utility.render_profession2(declaration.symbol.profession, context);
            }

            var result = start + new Stroke_Token(" " + declaration.symbol.name);

            if (declaration.expression != null)
            {
                var assignment = render_assignment_expression(declaration.symbol.profession, declaration.expression,
                    declaration.symbol.is_owner);

                result += new Stroke_Token(" = ")
                    + assignment.expression;

                if (assignment.pre_statement != null)
                    result = assignment.pre_statement + result;
            }

            result += terminate_statement();

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

            var result = Utility.render_dungeon_path2(expression.profession.dungeon, context)
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

        public static bool is_pointer(Profession profession)
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
            return Utility.shared_pointer(new Stroke_Token(dungeon.name))
                    + new Stroke_Token("(")
                    + render_expression(expression)
                    + new Stroke_Token(")");
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

    }
}
