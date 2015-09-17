using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using imp_test.fixtures;
using imperative.schema;
using metahub.render.targets;
using imperative;

namespace imp_test.tests
{
    [TestFixture]
    public class General_Test
    {
        [Test]
        public void class_attribute_test()
        {
            var overlord = new Overlord();
            var code = Utility.load_resource("imp.class_attributes.imp");
            overlord.summon(code, "imp.class_attributes.imp");
            var dungeon = overlord.root.get_dungeon_from_path("magic.lore.Vector3");
            Assert.True(dungeon.is_external);
            Assert.True(dungeon.is_value);
        }

    }
}
