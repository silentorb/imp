using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.schema;
using imperative.scholar;

namespace imperative.render.artisan.targets.cpp
{
    public static class Preparation
    {

        public static void prepare_dungeon(Dungeon dungeon)
        {
            prepare_constructor(dungeon);
            Ownership.analyze_dungeon(dungeon);
            determine_cpp_types(dungeon);
            prepare_destructor(dungeon);
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
                        if (Utility.is_shared_pointer(profession) && profession.cpp_type != Cpp_Type.shared_pointer)
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

        public static Minion get_or_create_destructor(Dungeon dungeon)
        {
            var destructor_name = "~" + dungeon.name;
            if (dungeon.has_minion(destructor_name))
            {
                return dungeon.summon_minion(destructor_name);
            }

            var result = dungeon.spawn_minion(destructor_name, new List<Parameter>());
            result.return_type = null;
            return result;
        }

        public static void prepare_destructor(Dungeon dungeon)
        {
            var portals = dungeon.core_portals.Values
                .Where(p => p.is_owner && Cpp.is_pointer(p.profession))
                .ToList();

            if (portals.Count == 0)
                return;

            var destructor = get_or_create_destructor(dungeon);
            foreach (var portal in portals)
            {
                destructor.expressions.Add(new If(new List<Flow_Control>
                {
                    new Flow_Control(Flow_Control_Type.If, new Operation("!=", new Portal_Expression(portal), new Null_Value()),
                        new List<Expression>
                        {
                            new Statement("delete", new Portal_Expression(portal)),
                            new Assignment(new Portal_Expression(portal), "=", new Null_Value())
                        } )
                }));
            }
        }
    }
}
