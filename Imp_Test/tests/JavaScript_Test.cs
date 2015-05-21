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
            var overlord = Imp_Fixture.create_overlord("js", "imp.pizza.imp");
            var target = (JavaScript)overlord.target;
            var output = target.generate();
            var goal = Utility.load_resource("js.pizza.js");
            Utility.diff(goal, output);
        }

        [Test]
        public void anonymous_function_test()
        {
            var overlord = Imp_Fixture.create_overlord("js", "imp.anonymous_pizza.imp");
            var target = (JavaScript)overlord.target;
            var output = target.generate();
            var goal = Utility.load_resource("js.anonymous_minion.js");
            Utility.diff(goal, output);
        }

        [Test]
        public void browser_test()
        {
            var overlord = Imp_Fixture.create_overlord("js", "imp.browser.imp");
            var target = (JavaScript)overlord.target;
            var output = target.generate();
            var goal = Utility.load_resource("js.browser.js");
            Utility.diff(goal, output);
        }

        [Test]
        public void multifile_test()
        {
            var overlord = Imp_Fixture.create_overlord("js", "imp.part1.imp");
            var target = (JavaScript)overlord.target;
            var output = target.generate();
            var goal = Utility.load_resource("js.part1-2.js");
            Utility.diff(goal, output);
        }
    }
}
