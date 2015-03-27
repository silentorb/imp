using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using imperative.expressions;
using imperative.schema;

namespace imp_test.tests
{
    [TestFixture]
    class Expression_Test
    {
        [Test]
        public void test_next_parent()
        {
            var symbol = new Symbol("test", null, null);
            var variable = new Variable(symbol);
            var literal = new Literal(100);
            variable.next = literal;
            Assert.AreSame(literal.parent, variable);
        }
    }
}
