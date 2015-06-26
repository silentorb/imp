using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using imperative;
using imperative.render;
using imperative.schema;

using imperative.expressions;
using metahub.render;

namespace imperative.render.artisan.targets
{
    public class JavaScript : Common_Target2
    {
        public JavaScript(Overlord overlord = null)
            : base(overlord)
        {
            config = new Target_Configuration
                {
                    implicit_this = false,
                    primary_quote = "'",
                    supports_abstract = false,
                    supports_enums = false,
                    supports_namespaces = false
                };
        }

        override public void run(Overlord_Configuration settings, string[] sources)
        {
            var strokes = generate_strokes();
            var passages = Painter.render_root(strokes).ToList();
            var segments = new List<Segment>();
            var output = Scribe.render(passages, segments);

            var output_path = !string.IsNullOrEmpty(settings.output)
                ? settings.output
                : File.Exists(settings.input)
                ? settings.output + Path.GetFileNameWithoutExtension(settings.input) + ".js"
                : settings.output + "/" + "lib.js";

            // Source map
            var map_file = output_path + ".map";

            var original = new Uri(output_path.Replace(@"\", "/"));
            var source_map = new Source_Map(Path.GetFileName(output_path),
                sources.Select(s => original.MakeRelativeUri(new Uri(s)).ToString()).ToArray(),
                segments, Path.GetDirectoryName(output_path));
            var source_map_content = source_map.serialize();

            output += "\r\n//# sourceMappingURL=" + Path.GetFileName(map_file);

            Generator.create_file(output_path, output);
            Generator.create_file(map_file, source_map_content);
        }

        public string generate_string()
        {
            var strokes = generate_strokes();
            var passages = Painter.render_root(strokes).ToList();
            var segments = new List<Segment>();
            return Scribe.render(passages, segments);
        }

        public List<Stroke> generate_strokes()
        {
            var output = new List<Stroke>();
            foreach (var dungeon in overlord.dungeons)
            {
                if (dungeon.is_external || dungeon.is_external
                    || dungeon.name == "")
                    continue;

                output.Add(create_class_file(dungeon));
            }

            return output;
        }

        Stroke create_class_file(Dungeon dungeon)
        {
            //            render = new Renderer();
            var result = render_dungeon(dungeon);

            return result;
        }

        override protected Stroke render_dungeon(Dungeon dungeon)
        {
            if (dungeon.is_abstract)
                return new Stroke_Token("");

            if (dungeon.get_type() == Dungeon_Types.Namespace)
            {
                var fullname = "window." + render_dungeon_path(dungeon).full_text();
                return new Stroke_Token(fullname + " = " + fullname + " || {}");
            }

            current_dungeon = dungeon;

            var i = 0;
            var portals = dungeon.all_portals.Values.Where(p => !p.has_enchantment("abstract")
                && (dungeon.core_portals.ContainsKey(p.name) || p.default_expression != null)).ToArray();
            var instance_portals = portals.Where(p => !p.has_enchantment("static")).ToArray();
            var static_portals = portals.Except(instance_portals).ToList();

            var minions = dungeon.minions.Values.Where(p =>
                !p.has_enchantment("abstract") && p.name != "constructor").ToArray();
            var instance_minions = minions.Where(p => !p.has_enchantment("static")).ToArray();
            var static_minions = minions.Except(instance_minions);

            var total = instance_portals.Length + instance_minions.Length;
            String_Delegate2 render_line = text => ++i < total
                    ? text + ","
                    : text;

            var dungeon_prefix = render_dungeon_path(dungeon).full_text();
            var result = new List<Stroke>
            {
                render_dungeon_path(dungeon) + new Stroke_Token(" = ") + render_constructor(dungeon)
            }
                .Concat(render_static_properties(dungeon_prefix, static_portals))
                .Concat(render_static_minions(dungeon_prefix, static_minions)).ToList();

            result.Add(total == 0
                ? new Stroke_Token("")
                : new Stroke_Token(dungeon_prefix + ".prototype = ") + (dungeon.parent != null
                    ? new Stroke_Token("Object.create(") + render_dungeon_path(dungeon.parent) + new Stroke_Token(".prototype)")
                    : new Stroke_Token("{}")
                    )
                );

            result.AddRange(instance_portals.Select(portal =>
            {
                var assignment = get_default_value(portal) ?? render_null();
                return render_dungeon_path(dungeon) + new Stroke_Token(".prototype." + portal.name + " = ") + assignment;
            })
            .Concat(instance_minions.Select(minion =>
                render_dungeon_path(dungeon) + new Stroke_Token(".prototype." + minion.name + " = ")
                + render_function_definition(minion)))
            );

            current_dungeon = null;

            return new Stroke_List(Stroke_Type.statements, result);
        }

        Stroke render_constructor(Dungeon dungeon)
        {
            return !dungeon.minions.ContainsKey("constructor")
                ? new Stroke_Token("function() {") + (dungeon.parent != null
                    ? render_dungeon_path(dungeon.parent) + new Stroke_Token(".apply(this)") + new Stroke_Token("}")
                    : new Stroke_Token("}"))
                : render_function_definition(dungeon.minions["constructor"]);
        }

        List<Stroke> render_static_properties(string dungeon_prefix, IEnumerable<Portal> portals)
        {
            return portals.Select(p => new Stroke_Token(dungeon_prefix + "." + p.name + " = ")
                + get_default_value(p)).ToList();
        }

        List<Stroke> render_static_minions(string dungeon_prefix, IEnumerable<Minion> minions)
        {
            return minions.Select(p => new Stroke_Token(dungeon_prefix + "." + p.name + " = ")
                + render_function_definition(p)).ToList();
        }

        override protected List<Stroke> render_properties(Dungeon dungeon)
        {
            return dungeon.core_portals.Values.Select(portal => render_dungeon_path(dungeon)
                + new Stroke_Token(".prototype." + portal.name + " = ")
                + get_default_value(portal)).ToList();
        }

        //        override protected Stroke render_variable_declaration(Declare_Variable declaration)
        //        {
        //            var profession = declaration.symbol.get_profession(overlord);
        //            Stroke first = new Stroke_Token("var " + declaration.symbol.name);
        //            if (declaration.expression != null)
        //                first += new Stroke_Token(" = ") + render_expression(declaration.expression);
        //
        //            return first;
        //        }

        //        protected Stroke render_function_definition(Function_Definition definition)
        //        {
        //            if (definition.is_abstract)
        //                return new Stroke_Token("");
        //
        //            var minion = definition.minion;
        //
        //            // Search for any case of "this" inside an anonymous function.
        //            var minions = definition.find(Expression_Type.anonymous_function);
        //            if (minions.Any(m => m.find(e => e.type == Expression_Type.self || e.type == Expression_Type.property_function_call).Any()))
        //            {
        //                var self = minion.scope.create_symbol("self", current_dungeon.overlord.library.get(current_dungeon));
        //                minion.expressions.Insert(0, new Declare_Variable(self, new Self(minion.dungeon)));
        //            }
        //
        //            return new Stroke_Token("function(" + definition.parameters.Select(p => p.symbol.name).join(", ") + ")")
        //                + render_block(render_statements(minion.expressions), false);
        //        }

        private bool is_instance_start_portal(Portal_Expression portal_expression)
        {
            return is_start_portal(portal_expression)
                   && !portal_expression.portal.has_enchantment(Enchantments.Static);
        }

        override protected Stroke render_function_definition(Minion minion)
        {
            if (minion.is_abstract)
                return new Stroke_Token();

            minion_stack.Push(minion);

            // Search for any case of "this" inside an anonymous function.
            var minions = minion.expression.find(Expression_Type.anonymous_function);
            if (minions.Any(m => m.find(e => e.type == Expression_Type.self
                || (e.type == Expression_Type.portal
                && is_instance_start_portal((Portal_Expression)e)
                )).Any()))
            {
                var self = minion.scope.create_symbol("self", current_dungeon.overlord.library.get(current_dungeon));
                minion.expressions.Insert(0, new Declare_Variable(self, new Self(minion.dungeon)));
            }

            var parameters_with_default_values = minion.parameters.Where(p => p.default_value != null);
            foreach (var parameter in parameters_with_default_values)
            {
                minion.expressions.Insert(0, new If(new List<Flow_Control>
                {
                    new Flow_Control(Flow_Control_Type.If, 
                        new Operation("===", new Variable(parameter.symbol), new Insert("undefined") ), 
                        new Expression[] {
                        new Assignment(new Variable(parameter.symbol), "=",  parameter.default_value) 
                        })
                }));
            }
            var result = new Stroke_Token("function(" + minion.parameters.Select(p => p.symbol.name).join(", ") + ")")
                + render_block(render_statements(minion.expressions), false);

            minion_stack.Pop();
            return result;
        }

        protected override Stroke render_this()
        {
            return new Stroke_Token(current_minion.GetType() == typeof(Ethereal_Minion)
                && current_minion.scope.find_or_null("self") != null
                ? "self"
                : "this");
        }

        //        override protected string render_iterator_block(Iterator statement)
        //        {
        //            var parameter = statement.parameter;
        //            var it = parameter.scope.create_symbol("it", parameter.profession);
        //            var expression = render_iterator(it, statement.expression);
        //
        //            var result = add("for (" + expression + ")") + render_scope(new List<Expression> { 
        //                    new Declare_Variable(parameter, new Insert("*" + it.name))
        //                }.Concat(statement.body).ToList()
        //            );
        //            return result;
        //        }

        //        string render_operation(Operation operation)
        //        {
        //            return operation.children.Select(c =>
        //                c.type == Expression_Type.operation && ((Operation)c).is_condition() == operation.is_condition()
        //                ? "(" + render_expression(c) + ")"
        //                : render_expression(c)
        //            ).join(" " + operation.op + " ");
        //        }

        //        override protected string render_iterator(Symbol parameter, Expression expression)
        //        {
        //            var path_string = render_expression(expression);
        //            return
        //                "::const_iterator " + parameter.name + " = "
        //                + path_string + ".begin(); " + parameter.name + " != "
        //                + path_string + ".end(); " + parameter.name + "++";
        //        }

        //        Stroke protected string render_scope(String_Delegate action)
        //        {
        //            push_scope();
        //            var result = config.block_brace_same_line
        //                ? " {" + newline()
        //                : newline() + line("{");
        //
        //            indent();
        //            result += action();
        //            unindent();
        //            result += add("}");
        //            pop_scope();
        //            return result;
        //        }

        override protected Stroke render_platform_function_call(Platform_Function expression, Expression parent)
        {
            var ref_string = expression.reference != null
          ? render_expression(expression.reference)
          : new Stroke_Token("");

            var ref_full = ref_string.full_text().Length > 0
                ? ref_string + "."
                : "";

            switch (expression.name)
            {
                case "count":
                    return new Stroke_Token(ref_full + "size()");

                case "add":
                    {
                        var first = render_expression(expression.args[0]);
                        //var dereference = is_pointer(expression.args.Last().get_signature()) ? "*" : "";
                        return new Stroke_Token(ref_full + "push(" + first + ")");
                    }

                case "contains":
                    {
                        var first = render_expression(expression.args[0]);
                        return new Stroke_Token("std::find(" + ref_full + "begin(), "
                               + ref_full + "end(), " + first + ") != " + ref_full + "end()");
                    }

                case "distance":
                    {
                        //var signature = expression.args[0].get_signature();
                        var first = render_expression(expression.args[0]);
                        //var dereference = is_pointer(signature) ? "*" : "";
                        return new Stroke_Token(ref_full + "distance(" + first + ")");
                    }

                case "first":
                    return new Stroke_Token("[0]");

                case "last":
                    return new Stroke_Token(ref_full + "back()");

                case "pop":
                    return new Stroke_Token(ref_full + "pop_back()");

                case "remove":
                    {
                        var first = render_expression(expression.args[0]);
                        return new Stroke_Token(ref_full + "erase(std::remove(" + ref_full + "begin(), "
                            + ref_full + "end(), " + first + "), " + ref_full + "end())");
                    }

                case "rand":
                    float min = ((Literal)expression.args[0]).get_float();
                    float max = ((Literal)expression.args[1]).get_float();
                    return new Stroke_Token("rand() % " + (max - min) + (min < 0 ? " - " + -min : " + " + min));

                default:
                    throw new Exception("Unsupported platform-specific function: " + expression.name + ".");
            }
        }

        //        string render_function_call(Class_Function_Call expression, Expression parent)
        //        {
        //            var ref_string = expression.reference != null
        //               ? render_expression(expression.reference)
        //               : "";
        //
        //            var ref_full = ref_string.Length > 0
        //                ? ref_string + "."
        //                : "";
        //
        //            return ref_full + expression.name + "(" +
        //                expression.args.Select(a => render_expression(a))
        //                .join(", ") + ")";
        //        }
        //
        //        string render_assignment(Assignment statement)
        //        {
        //            return line(render_expression(statement.target) + " " + statement.op + " " + render_expression(statement.expression));
        //        }
        //

        //
        //        string render_instantiation(Instantiate expression)
        //        {
        //            var args = expression.args.Select(a => render_expression(a)).join(", ");
        //            return "new " + full_dungeon_name(expression.dungeon) + "(" + args + ")";
        //        }

        //        override protected Stroke render_realm(Dungeon realm, Stroke_Delegate action)
        //        {
        //            if (realm.name == "")
        //                return action();
        //
        //            var fullname = "window." + render_dungeon_path(realm);
        //            var result = add(fullname + " = " + fullname + " || {}") + newline();
        //
        //            current_realm = realm;
        //            var body = action();
        //            current_realm = null;
        //
        //            if (body == "")
        //                return "";
        //
        //            result += body;
        //            return result;
        //        }

    }
}
