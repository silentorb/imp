using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using imp_test.fixtures;
using imperative.render.targets;
using imperative.schema;
using metahub.render.targets;

namespace imp_test.tests
{
    [TestFixture]
    public class CSharp_Test
    {
        [Test]
        public void pizza_test()
        {
            var overlord = Imp_Fixture.create_overlord("cs", "imp.pizza.imp");
            var target = (Csharp) overlord.target;
            var dungeon = (Dungeon)overlord.root.get_dungeon_from_path("test.Pizza");
            {
                var output = target.generate_dungeon_file_contents(dungeon);
                var goal = Utility.load_resource("cs.pizza.cs");
                Utility.diff(goal, output);
            }

            {
                var treasury = overlord.root.children["test"].treasuries["Crust"];
                var output = target.generate_enum_file_contents(treasury);
                var goal = Utility.load_resource("cs.crust.cs");
                Utility.diff(goal, output);
            }
        }

        [Test]
        public void namespace_test()
        {
            var overlord = Imp_Fixture.create_overlord("cs", new[] { "imp.namespaces1.imp", "imp.namespaces2.imp" });
            var target = (Csharp)overlord.target;
            var dungeon = (Dungeon)overlord.root.get_dungeon_from_path("light.citadel.Courier");
            {
                var output = target.generate_dungeon_file_contents(dungeon);
                var goal = Utility.load_resource("cs.namespaces2.cs");
                Utility.diff(goal, output);
            }
        }

        [Test]
        public void generic_test()
        {
            var overlord = Imp_Fixture.create_overlord("cs", "imp.generic.imp");
            var target = (Csharp)overlord.target;
            var dungeon = (Dungeon)overlord.root.get_dungeon_from_path("magic.lore.Mage");
            {
                var output = target.generate_dungeon_file_contents(dungeon);
                var goal = Utility.load_resource("cs.generic.cs");
                Utility.diff(goal, output);
            }
        }
    }
}
