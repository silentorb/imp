using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using imp_test.fixtures;
using imperative;
using imperative.schema;
using imperative.summoner;
using runic.parser;

namespace imp_test.tests
{
    [TestFixture]
    public class Runic_Test
    {
        [Test]
        public void test()
        {
            var code = Utility.load_resource("imp.pizza.imp");
            var runes = Summoner2.read_runes(code, "imp.pizza.imp");
            Assert.Greater(runes.Count, 5);

            var legend = Summoner2.translate_runes(code, runes);
            var overlord = new Overlord();
            var summoner = new Summoner2(overlord);
            summoner.summon((Group_Legend)legend);
        }

        [Test]
        public void test_if()
        {
            var code = Utility.load_resource("imp.if.imp");
            var runes = Summoner2.read_runes(code, "imp.if.imp");
            var legend = Summoner2.translate_runes(code, runes, "if_statement");
            Assert.AreEqual("return_statement", legend.children[1].children[0].rhyme.name);
        }

        [Test]
        public void test_if_else()
        {
            var code = Utility.load_resource("imp.if_else.imp");
            var runes = Summoner2.read_runes(code, "imp.if_else.imp");
            var legend = Summoner2.translate_runes(code, runes, "statements");
//            Assert.AreEqual("else_statement", legend.children[1].type);

            Imp_Fixture.summon_statements(legend.children);
        }

        [Test]
        public void test_empty_array()
        {
            var code = Utility.load_resource("imp.empty_array.imp");
            var runes = Summoner2.read_runes(code, "imp.empty_array.imp");
            var legend = Summoner2.translate_runes(code, runes, "statement");
            Assert.AreEqual("empty_array", legend.children[2].children[0].rhyme.name);

            Imp_Fixture.summon_statement(legend);
        }

        [Test]
        public void test_anonymous_function()
        {
            var code = Utility.load_resource("imp.anonymous_function.imp");
            var runes = Summoner2.read_runes(code, "imp.anonymous_function.imp");
            var legend = Summoner2.translate_runes(code, runes, "statement");
            Assert.AreEqual("lambda", legend.children[2].children[0].rhyme.name);
        }
    }
}
