﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.render.artisan;
using NUnit.Framework;
using imp_test.fixtures;
using imperative.render.artisan.targets;
using imperative.schema;

namespace imp_test.tests
{
    [TestFixture]
    public class CSharp_Test
    {
        [Test]
        public void pizza_test()
        {
            var overlord = Imp_Fixture.create_overlord("cs", "imp.pizza.imp");
//            throw new Exception("Broke it.");

            var target = (Csharp) overlord.target;
            var dungeon = (Dungeon)overlord.root.get_dungeon_from_path("test.Pizza");
            {
                var strokes = target.generate_dungeon_file_contents(dungeon);
                var output = Common_Target2.render_strokes(strokes);
                var goal = Utility.load_resource("cs.pizza.cs");
                Utility.diff(goal, output);
            }

        }

        [Test]
        public void namespace_test()
        {
            var overlord = Imp_Fixture.create_overlord("cs", new[] { "imp.namespaces1.imp", "imp.namespaces2.imp" });
            throw new Exception("Broke it.");

//            var target = (Csharp)overlord.target;
//            var dungeon = (Dungeon)overlord.root.get_dungeon_from_path("light.citadel.Courier");
//            {
//                var output = target.generate_dungeon_file_contents(dungeon);
//                var goal = Utility.load_resource("cs.namespaces2.cs");
//                Utility.diff(goal, output);
//            }
        }

        [Test]
        public void generic_test()
        {
            var overlord = Imp_Fixture.create_overlord("cs", "imp.generic.imp");
            throw new Exception("Broke it.");

//            var target = (Csharp)overlord.target;
//            var dungeon = (Dungeon)overlord.root.get_dungeon_from_path("magic.lore.Mage");
//            {
//                var output = target.generate_dungeon_file_contents(dungeon);
//                var goal = Utility.load_resource("cs.generic.cs");
//                Utility.diff(goal, output);
//            }
        }
    }
}
