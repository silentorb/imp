using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.schema;
using NUnit.Framework;
using imp_test.fixtures;
using metahub.render.targets;

namespace imp_test.tests
{
    [TestFixture]
    public class Cpp_Test
    {
        [Test]
        public void test_simple()
        {
            var overlord = Imp_Fixture.create_overlord("cpp", "imp.pizza.imp");
            throw new Exception("Broke it.");
//            var target = (Cpp)overlord.target;
//            var dungeon = (Dungeon)overlord.root.dungeons["test"].dungeons["Pizza"];
//            var output_h = target.create_header_file(dungeon);
//            var output_cpp = target.create_class_file(dungeon);
//            var goal_h = Utility.load_resource("cpp.pizza.h");
//            var goal_cpp = Utility.load_resource("cpp.pizza.cpp");
//            Utility.diff(goal_h, output_h);
//            Utility.diff(goal_cpp, output_cpp);
        }
    }
}
