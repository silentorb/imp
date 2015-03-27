using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var target = new Cpp();
            var overlord = Imp_Fixture.create_overlord(target, "imp.pizza.imp");
            var dungeon = overlord.get_dungeon("Pizza");
            var output_h = target.create_header_file(dungeon);
            var output_cpp = target.create_class_file(dungeon);
            var goal_h = Utility.load_resource("cpp.pizza.h"); 
            var goal_cpp = Utility.load_resource("cpp.pizza.cpp");
            Utility.diff(goal_h, output_h);
            Utility.diff(goal_cpp, output_cpp);
        }
    }
}
