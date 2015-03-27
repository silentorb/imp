using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;

using metahub.schema;

namespace imperative.schema
{
    public class Minion : Minion_Base
    {
        public static string[] platform_specific_functions = new string[]
            {
                "count",
                "add",
                "contains",
                "distance",
                "last",
                "pop",
                "remove",
                "rand",
                "setter"
            };

        public string name;
        public Dungeon dungeon;
        public Portal portal;
        public bool is_platform_specific;
        public List<Minion> invokers = new List<Minion>();
        public List<Minion> invokees = new List<Minion>();
        public Accordian accordian;
        public Minion parent;
        public List<Minion> children = new List<Minion>();
        public bool is_abstract = false;
        public bool is_static = false;

#if DEBUG
        public string stack_trace;
#endif

        public Minion(string name, Dungeon dungeon, Portal portal = null)
        {
            this.name = name;
            this.dungeon = dungeon;
            this.portal = portal;
            accordian = dungeon.create_block(name, scope, expressions);

#if DEBUG
            stack_trace = Environment.StackTrace;
#endif
        }

        public Minion spawn_child(Dungeon new_dungeon)
        {
            var child = new_dungeon.spawn_minion(name, parameters, null, return_type, portal);
            child.parent = this;
            children.Add(child);
            return child;
        }

        public static Flow_Control If(Expression expression, List<Expression> children)
        {
            return new Flow_Control(Flow_Control_Type.If, expression, children);
        }

        public static Literal False()
        {
            return new Literal(false);
        }

        public static Literal True()
        {
            return new Literal(true);
        }

        public static Operation operation(string op, Expression first, Expression second)
        {
            return new Operation(op, new List<Expression> { first, second });
        }

        public static Property_Function_Call setter(Portal portal, Expression value, Expression reference, Expression origin)
        {
            if (reference.type == Expression_Type.operation)
                throw new Exception("Cannot call function on operation.");

            return new Property_Function_Call(Property_Function_Type.set, portal, origin != null
                ? new List<Expression> { value, origin }
                : new List<Expression> { value }
             ) { reference = reference };
        }

        public static Expression call_remove(Portal portal, Expression reference, Expression item)
        {
            if (reference.type == Expression_Type.operation)
                throw new Exception("Cannot call function on operation.");

            return portal.type == Kind.reference
                ? setter(portal, new Null_Value(), reference, null)
                : new Property_Function_Call(Property_Function_Type.remove, portal, new List<Expression>
                    {
                     item   
                    }) { reference = reference };
        }

        public static Expression call_initialize(Dungeon caller, Dungeon target, Expression reference)
        {
            var args = new List<Expression>();
            var minion = target.summon_minion("initialize");
            if (minion.parameters.Count > 0)
                args.Add(new Portal_Expression(caller.all_portals["hub"]));

            return new Method_Call(minion, reference, args);
        }

        public Parameter add_parameter(string name, Profession profession, Expression default_value = null)
        {
            var symbol = scope.create_symbol(name, profession);
            var parameter = new Parameter(symbol, default_value);
            parameters.Add(parameter);
            return parameter;
        }

        public void add_to_block(Expression expression)
        {
            accordian.add(expression);
//            if (on_add_expression != null)
//                on_add_expression(this, expression);
        }

        public void add_to_block(IEnumerable<Expression> expressions)
        {
            foreach (var expression in expressions)
            {
                add_to_block(expression);
            }
        }

        public void add_to_block(string division, Expression expression)
        {
            accordian.add(division, expression);
//            if (on_add_expression != null)
//                on_add_expression(this, expression);
        }
    }
}
