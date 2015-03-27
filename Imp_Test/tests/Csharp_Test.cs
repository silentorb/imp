using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using imp_test.fixtures;
using imperative.render.targets;
using metahub.render.targets;

namespace imp_test.tests
{
    [TestFixture]
    public class CSharp_Test
    {
        [Test]
        public void pizza_test()
        {
            var target = new Csharp();
            var overlord = Imp_Fixture.create_overlord(target, "imp.pizza.imp");
            var dungeon = overlord.get_dungeon("Pizza");
            {
                var output = target.generate_dungeon_file_contents(dungeon);
                var goal = Utility.load_resource("cs.pizza.cs");
                Utility.diff(goal, output);
            }

            {
                var treasury = overlord.realms["test"].treasuries["Crust"];
                var output = target.generate_enum_file_contents(treasury);
                var goal = Utility.load_resource("cs.crust.cs");
                Utility.diff(goal, output);
            }
        }
    }
}
