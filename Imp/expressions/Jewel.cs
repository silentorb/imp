using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.schema;

namespace imperative.expressions
{
    public class Jewel : Expression
    {
        public Treasury treasury;
        public int value;

        public Jewel(Treasury treasury, int value)
            :base(Expression_Type.jewel)
        {
            this.treasury = treasury;
            this.value = value;
        }

        public override IEnumerable<Expression> children
        {
            get { return new List<Expression>(); }
        }
    }
}
