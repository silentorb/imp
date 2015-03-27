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
    public class JavaScript_Test
    {
        [Test]
        public void pizza_test()
        {
            var target = new JavaScript();
            Imp_Fixture.create_overlord(target, "imp.pizza.imp");
            var output = target.generate();
            var goal = Utility.load_resource("js.pizza.js");
            Utility.diff(goal, output);
        }

        [Test]
        public void anonymous_function_test()
        {
            var target = new JavaScript();
            Imp_Fixture.create_overlord(target, "imp.anonymous_pizza.imp");
            var output = target.generate();
            var goal = Utility.load_resource("js.anonymous_minion.js");
            Utility.diff(goal, output);
        }
    }
}
