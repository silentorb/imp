using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative;
using imperative.render.artisan;
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
        public void test_pizza()
        {
            var overlord = Imp_Fixture.create_overlord("cpp", "imp.pizza.imp");
            var target = new imperative.render.artisan.targets.Cpp(overlord);
            var pizza = overlord.root.dungeons["test"].dungeons["Pizza"];

            // Source file
            var strokes = new List<Stroke>{ target.generate_class_file(pizza) };
            var output = Overlord.strokes_to_string(strokes);
            var goal = Utility.load_resource("cpp.pizza.cpp");
            Utility.diff(goal, output);

            // Header file
            strokes = new List<Stroke> { target.generate_header_file(pizza) };
            output = Overlord.strokes_to_string(strokes);
            goal = Utility.load_resource("cpp.pizza.h");
            Utility.diff(goal, output);
        }
    }
}
