using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using imperative.expressions;
using imperative.legion;
using imperative.scholar;
using imperative.scholar.crawling;
using metahub.render;
using metahub.schema;
using Expression = imperative.expressions.Expression;
using Parameter = imperative.expressions.Parameter;
using Variable = imperative.expressions.Variable;

namespace imperative.schema
{

    public delegate void Dungeon_Minion_Event(Dungeon dungeon, Minion minion);

    [DebuggerDisplay("dungeon ({name})")]
    public class Dungeon : IDungeon
    {
        public string name { get; set; }
        public Dungeon realm { get; set; }
        public Dictionary<string, Dungeon> dungeons = new Dictionary<string, Dungeon>();
        public Profession parent;
        public List<Dungeon> children = new List<Dungeon>();
        //        public List<Expression> code;
        Dictionary<string, Accordian> blocks = new Dictionary<string, Accordian>();
        public Overlord overlord;
        public Dictionary<string, Minion> minions = new Dictionary<string, Minion>();
        public Dictionary<string, Portal> all_portals = new Dictionary<string, Portal>();
        public Dictionary<string, Portal> core_portals = new Dictionary<string, Portal>();
        public Dictionary<string, Used_Function> used_functions = new Dictionary<string, Used_Function>();
        public Dictionary<string, Dependency> dependencies = new Dictionary<string, Dependency>();
        public List<Dungeon> needed_realms = new List<Dungeon>();
        public bool is_external = false;
        public bool is_abstract = false;
        public bool is_virtual = false;
        public bool is_dynamic = false;

        public Dictionary<string, object> hooks = new Dictionary<string, object>();
        public List<Profession> interfaces = new List<Profession>();
        public string class_export = "";
        public event Dungeon_Minion_Event on_add_minion;
        public string external_name;
        public Dictionary<string, Profession> generic_parameters = new Dictionary<string, Profession>();
        public Dictionary<string, Dungeon_Additional> trellis_additional = new Dictionary<string, Dungeon_Additional>();
        public Project project;
        public Dungeon_Journal journal;
        public object default_value { get; set; }

        private bool _is_standard = false;
        public bool is_standard
        {
            get { return _is_standard; }
            set
            {
                if (value == _is_standard)
                    return;

                foreach (var child in children)
                {
                    child.is_standard = value;
                }

                _is_standard = value;
            }
        }

        private Portal[] _portals;
        public Portal[] portals
        {
            get
            {
                if (_portals == null)
                {
                    _portals = new Portal[all_portals.Count];
                    var i = 0;
                    foreach (var portal in all_portals.Values)
                    {
                        _portals[i++] = portal;
                    }
                }

                return _portals;
            }
        }

        bool _is_value = false;
        public bool is_value
        {
            get { return _is_value || is_enum; }
            set { _is_value = value; }
        }

        public string fullname
        {
            get
            {
                return realm != null
                    ? realm.fullname + name
                    : name;
            }
        }

#if DEBUG
        private int id;
        private static int next_id = 1;
#endif

        public Dungeon(string name, Overlord overlord, Dungeon realm, Profession parent = null, bool is_value = false)
        {
#if DEBUG
            id = next_id++;
#endif

            this.name = name;
            journal = new Dungeon_Journal(this);
            if (overlord != null)
            {
                this.overlord = overlord;
                overlord.dungeons.Add(this);
            }

            _is_value = is_value;

            if (parent != null)
            {
                this.parent = parent;
                parent.dungeon.children.Add(this);
                foreach (var portal in parent.dungeon.all_portals.Values)
                {
                    all_portals[portal.name] = new Portal(portal, this);
                }
            }

            if (realm != null)
            {
                this.realm = realm;
                realm.dungeons[name] = this;
                is_external = realm.is_external;
                class_export = realm.class_export;
                is_standard = realm.is_standard;
            }
        }


        public Dependency add_dependency(Dungeon dungeon)
        {
            if (dungeon == null || dungeon == this)
                return null;

            if (!dependencies.ContainsKey(dungeon.name))
                dependencies[dungeon.name] = new Dependency(dungeon);

            if (dungeon.realm != null && dungeon.realm != realm && !dungeon.realm.is_virtual && !needed_realms.Contains(dungeon.realm))
                needed_realms.Add(dungeon.realm);

            var result = dependencies[dungeon.name];
            if ((parent != null && dungeon == parent.dungeon) || interfaces.Any(i => i.dungeon == dungeon))
                result.allow_partial = false;

            return result;
        }

        public Accordian create_block(string path, Scope scope, List<Expression> expressions = null)
        {
            var block = new Accordian(path, scope, this, expressions);
            blocks[path] = block;
            return block;
        }

        public bool has_block(string path)
        {
            return blocks.ContainsKey(path);
        }

        public Accordian get_block(string path)
        {
            if (!has_block(path))
                throw new Exception("Dungeon " + name + " does not have a block named " + path + ".");

            return blocks[path];
        }

        public Portal add_portal(Portal portal)
        {
            if (core_portals.ContainsKey(portal.name))
                throw new Exception("Dungeon " + name + " already has a portal named " + portal.name + ".");

            portal.dungeon = this;
            core_portals[portal.name] = portal;
            add_inherited_portal(portal);
            return portal;
        }

        protected void add_inherited_portal(Portal portal)
        {
            all_portals[portal.name] = portal;
            foreach (var child in children)
            {
                child.add_inherited_portal(portal);
            }
        }

        public bool has_portal(string portal_name)
        {
            return all_portals.ContainsKey(portal_name);
        }

        public Portal add_parent_portal(Portal portal)
        {
            if (all_portals.ContainsKey(portal.name))
                throw new Exception("Dungeon " + name + " already has a portal named " + portal.name + ".");

            portal.dungeon = this;
            all_portals[portal.name] = portal;
            return portal;
        }

        public Portal get_portal_or_null(string portal_name)
        {
            if (all_portals.ContainsKey(portal_name))
                return all_portals[portal_name];

            return null;
        }

        public void flatten()
        {
            foreach (var block in blocks.Values)
            {
                block.flatten();
            }
        }

        public void analyze()
        {
            Dungeon_Crawler.crawl(this, d => add_dependency(d), expression =>
            {
                if (expression.type == Expression_Type.function_call)
                {
                    var definition = (Abstract_Function_Call)expression;
                    if (definition.GetType() == typeof(Method_Call) && overlord.target.GetType() == typeof(render.artisan.targets.cpp.Cpp))
                    {
                        var minion = ((Method_Call)definition).minion;
                        if (minion.expressions.Count > 0)
                            return ((Method_Call)definition).minion.expressions;
                    }
                }

                return null;
            });

            //            if (parent != null && !parent.dungeon.is_abstract)
            //                add_dependency(parent.dungeon).allow_partial = false;


            //            foreach (var @interface in interfaces)
            //            {
            //                add_dependency(@interface.dungeon).allow_partial = false;
            //            }
            //
            //            foreach (var portal in all_portals.Values)
            //            {
            //                var other_dungeon = portal.other_dungeon;
            //                if (other_dungeon != null && (other_dungeon.GetType() != typeof(Dungeon) || !other_dungeon.is_abstract))
            //                {
            //                    add_dependency(portal.other_dungeon);
            //                    if (portal.profession.children != null)
            //                    {
            //                        foreach (var profession in portal.profession.children)
            //                        {
            //                            add_dependency(profession.dungeon);
            //                        }
            //                    }
            //                }
            //            }
            //
            //            foreach (var minion in minions.Values)
            //            {
            //                if (minion.return_type != null)
            //                    analyze_profession(minion.return_type);
            //
            //                foreach (var parameter in minion.parameters)
            //                {
            //                    analyze_profession(parameter.symbol.profession);
            //                }
            //                analyze_expressions(minion.expressions);
            //            }
        }

        void transform_expression(Expression expression, Expression parent)
        {
            expression.parent = parent;

            switch (expression.type)
            {
                case Expression_Type.function_definition:
                    transform_expressions(((Function_Definition)expression).expressions, expression);
                    break;

                case Expression_Type.operation:
                    transform_expressions(((Operation)expression).children, expression);
                    break;

                case Expression_Type.flow_control:
                    transform_expression(((Flow_Control)expression).condition, expression);
                    transform_expressions(((Flow_Control)expression).body, expression);
                    break;

                case Expression_Type.function_call:
                    var definition = (Abstract_Function_Call)expression;
                    transform_expressions(definition.args, expression);
                    break;

                case Expression_Type.property_function_call:
                    var property_function = (Property_Function_Call)expression;
                    transform_expressions(property_function.args, expression);
                    break;

                case Expression_Type.assignment:
                    transform_expression(((Assignment)expression).expression, expression);
                    break;

                case Expression_Type.declare_variable:
                    var declare_variable = (Declare_Variable)expression;
                    transform_expression(declare_variable.expression, expression);
                    break;

                case Expression_Type.iterator:
                    var iterator = (Iterator)expression;
                    transform_expression(iterator.expression, expression);
                    transform_expressions(iterator.body, expression);
                    break;
            }

            if (expression.next != null)
                transform_expression(expression.next, expression);
        }

        void transform_expressions(IEnumerable<Expression> expressions, Expression parent_expression)
        {
            foreach (var expression in expressions)
            {
                transform_expression(expression, parent_expression);
            }
        }
        //
        //
        //        void analyze_expression(Expression expression)
        //        {
        //            //            overlord.target.analyze_expression(expression);
        //
        //            switch (expression.type)
        //            {
        //                case Expression_Type.function_definition:
        //                    analyze_expressions(((Function_Definition)expression).expressions);
        //                    break;
        //
        //                case Expression_Type.operation:
        //                    analyze_expressions(((Operation)expression).children);
        //                    break;
        //
        //                case Expression_Type.flow_control:
        //                    analyze_expression(((Flow_Control)expression).condition);
        //                    analyze_expressions(((Flow_Control)expression).body);
        //                    break;
        //
        //                case Expression_Type.function_call:
        //                    {
        //                        var definition = (Abstract_Function_Call)expression;
        //                        analyze_expressions(definition.args);
        //                        if (definition.GetType() == typeof(Method_Call) && overlord.target.GetType() == typeof(render.artisan.targets.cpp.Cpp))
        //                        {
        //                            analyze_expressions(((Method_Call)definition).minion.expressions);
        //                        }
        //                    }
        //                    break;
        //
        //                case Expression_Type.platform_function:
        //                    {
        //                        var definition = (Platform_Function)expression;
        //                        if (!used_functions.ContainsKey(definition.name))
        //                            used_functions[definition.name] = new Used_Function(definition.name,
        //                                                                                true);
        //
        //                        analyze_expressions(definition.args);
        //                    }
        //                    break;
        //
        //                case Expression_Type.property_function_call:
        //                    var property_function = (Property_Function_Call)expression;
        //                    if (property_function.reference != null)
        //                        analyze_expression(property_function.reference);
        //
        //                    analyze_expressions(property_function.args);
        //                    break;
        //
        //                case Expression_Type.assignment:
        //                    {
        //                        var assignment = (Assignment)expression;
        //                        analyze_expression(assignment.target);
        //                        analyze_expression(assignment.expression);
        //                    }
        //                    break;
        //
        //                case Expression_Type.declare_variable:
        //                    var declare_variable = (Declare_Variable)expression;
        //                    analyze_profession(declare_variable.symbol.profession);
        //                    if (declare_variable.expression != null)
        //                        analyze_expression(declare_variable.expression);
        //
        //                    break;
        //
        //                case Expression_Type.portal:
        //                    var portal_expression = (Portal_Expression)expression;
        //                    add_dependency(portal_expression.portal.dungeon);
        //                    analyze_profession(portal_expression.get_profession());
        //                    break;
        //
        //                case Expression_Type.variable:
        //                    var variable_expression = (Variable)expression;
        //                    if (!Professions.is_scalar(variable_expression.symbol.profession))
        //                        analyze_profession(variable_expression.symbol.profession);
        //
        //                    break;
        //
        //                case Expression_Type.iterator:
        //                    var iterator = (Iterator)expression;
        //                    analyze_expression(iterator.expression);
        //                    analyze_expressions(iterator.body);
        //                    break;
        //
        //                case Expression_Type.instantiate:
        //                    var instantiation = (Instantiate)expression;
        //                    analyze_profession(instantiation.profession);
        //                    analyze_expressions(instantiation.args);
        //                    break;
        //            }
        //
        //            if (expression.next != null)
        //                analyze_expression(expression.next);
        //        }
        //
        //        void analyze_profession(Profession profession)
        //        {
        //            if (profession.dungeon != null)
        //                add_dependency(profession.dungeon);
        //
        //            if (profession.children == null)
        //                return;
        //
        //            foreach (var child in profession.children)
        //            {
        //                analyze_profession(child);
        //            }
        //        }
        //
        //        void analyze_expressions(IEnumerable<Expression> expressions)
        //        {
        //            foreach (var expression in expressions)
        //            {
        //                analyze_expression(expression);
        //            }
        //        }

        public Function_Definition add_function(string function_name, List<Parameter> parameters, Profession return_type = null)
        {
            var minion = spawn_minion(function_name, parameters, new List<Expression>(), return_type);
            return new Function_Definition(minion);
        }

        public Minion spawn_simple_minion(string minion_name, List<Parameter> parameters = null,
            List<Expression> expressions = null, Profession return_type = null, Portal portal = null)
        {
            if (minions.ContainsKey(minion_name))
                throw new Exception("Dungeon " + name + " already contains an minion named " + minion_name + ".");

            var minion = new Minion(minion_name, this, portal)
            {
                parameters = parameters ?? new List<Parameter>(),
                return_type = return_type ?? Professions.none
            };

            if (expressions != null)
                minion.add_to_block(expressions);

            minions[minion_name] = minion;
            return minion;
        }

        public Minion spawn_minion(string minion_name, List<Parameter> parameters = null, List<Expression> expressions = null, Profession return_type = null, Portal portal = null)
        {
            var minion = spawn_simple_minion(minion_name, parameters, expressions);

            var definition = new Function_Definition(minion);

            //            var block = get_block("class_definition");
            //            block.add(definition);
            definition.scope = minion.scope = new Scope();
            minion.scope.minion = minion;

            if (on_add_minion != null)
                on_add_minion(this, minion);

            return minion;
        }

        public bool has_minion(string minion_name, bool check_ancestors = false)
        {
            var result = minions.ContainsKey(minion_name);
            if (result || !check_ancestors || parent == null)
                return result;

            return parent.dungeon.has_minion(minion_name, true);
        }

        public Minion summon_minion(string minion_name, bool check_ancestors = false)
        {
            if (minions.ContainsKey(minion_name))
                return minions[minion_name];

            if (!check_ancestors || parent == null)
                return null;

            return parent.dungeon.summon_minion(minion_name, true);
        }

        public string get_available_name(string key, int start = 0)
        {
            var result = "";
            do
            {
                result = key;
                if (start != 0)
                    result += start;

                ++start;
            } while (has_minion(result) || all_portals.ContainsKey(result));

            return result;
        }

        public Dungeon get_dungeon_from_path(string path)
        {
            return get_dungeon(path.Split('.'));
        }

        public Dungeon get_dungeon(string child_name)
        {
            return dungeons.ContainsKey(child_name)
                ? dungeons[child_name]
                : null;
        }

        public Dungeon get_dungeon(IEnumerable<string> original_path, bool throw_error = true)
        {
            var result = this;
            var path = original_path.ToArray();
            var tokens = path.ToArray();
            foreach (var token in tokens)
            {
                result = result.get_child_realm(token, false);
                if (result == null)
                    break;
            }

            if (result != null)
                return result;

            //            if (realm == null && !throw_error)
            //                return null;

            //            if (path.Length == 1)
            //                return realm.get_dungeon_descending(path.Last());

            if (realm != null)
            {
                return realm.get_dungeon(original_path, throw_error);
            }

            throw new Exception("Invalid path: " + original_path.join("."));
        }

        //        public Dungeon get_dungeon_descending(string path)
        //        {
        //            return get_dungeon(path) ?? (realm != null
        //                ? realm.get_dungeon_descending(path)
        //                : null);
        //        }

        public Dungeon get_child_realm(string token, bool throw_error = true)
        {
            if (!dungeons.ContainsKey(token))
            {
                if (name == "")
                {
                    if (token == "imp")
                        return overlord.load_standard_library();

                    if (!throw_error)
                        return null;

                    throw new Exception("Invalid namespace: " + token + ".");
                }
                else
                {
                    if (!throw_error)
                        return null;

                    throw new Exception("Namespace " + name + " does not have a child named " + token + ".");
                }
            }

            return dungeons[token];
        }

        public Dungeon create_dungeon(string dungeon_name)
        {
            var dungeon = new Dungeon(dungeon_name, overlord, this);
            dungeons[dungeon_name] = dungeon;
            return dungeon;
        }

        public Dungeon get_or_create_realm(IEnumerable<string> original_path)
        {
            var realm = this;
            var path = original_path.ToArray();
            foreach (var token in path)
            {
                if (!realm.dungeons.ContainsKey(token))
                {
                    create_dungeon(token);
                }

                realm = realm.dungeons[token];
            }

            return realm;
        }

        public Dungeon get_realm(IEnumerable<string> original_path)
        {
            var realm = this;
            var path = original_path.ToArray();
            foreach (var token in path)
            {
                realm = realm.get_child_realm(token);
            }

            return realm;
        }

        public Dungeon_Types get_type()
        {
            if (all_portals.Count == 0 && minions.Count == 0)
                return Dungeon_Types.Namespace;

            return Dungeon_Types.Class;
        }

        public bool is_enum
        {
            get { return parent != null && parent == Professions.Int; }
        }
    }
}