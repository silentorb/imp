using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using imperative.render.artisan;
using imperative.render.artisan.targets;
using NUnit.Framework;
using imp_test.fixtures;

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
            var output = target.generate_string();
            var goal = Utility.load_resource("js.pizza.js");
            Utility.diff(goal, output);
        }

        [Test]
        public void anonymous_function_test()
        {
            var overlord = Imp_Fixture.create_overlord("js", "imp.anonymous_pizza.imp");
            var target = (JavaScript)overlord.target;
            var output = target.generate_string();
            var goal = Utility.load_resource("js.anonymous_minion.js");
            Utility.diff(goal, output);
        }

        [Test]
        public void browser_test()
        {
            var overlord = Imp_Fixture.create_overlord("js", "imp.browser.imp");
            var target = (JavaScript)overlord.target;
            var output = target.generate_string();
            var goal = Utility.load_resource("js.browser.js");
            Utility.diff(goal, output);
        }

        [Test]
        public void browser_test_new()
        {
            var overlord = Imp_Fixture.create_overlord("js", "imp.browser.imp");
            var target = new imperative.render.artisan.targets.JavaScript(overlord);
            var strokes = target.generate_strokes();
            var passages = Painter.render_root(strokes).ToList();
            var segments = new List<Segment>();
            var output = Scribe.render(passages, segments);
            var goal = Utility.load_resource("js.browser.js");
            Utility.diff(goal, output);

            var source_map = new Source_Map("imp.browser.js", new [] {"browser.imp"}, segments);
            var source_map_content = source_map.serialize();
            Utility.diff(Utility.load_resource("js.browser.js.map"), source_map_content);
        }

        [Test]
        public void multifile_test()
        {
            var overlord = Imp_Fixture.create_overlord("js", new[] { "imp.part1.imp", "imp.part2.imp" });
            var target = (JavaScript)overlord.target;
            var output = target.generate_string();
            var goal = Utility.load_resource("js.part1-2.js");
            Utility.diff(goal, output);
        }
    }
}
