using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.schema;

namespace imperative.expressions
{
    public class Treasury_Definition : Expression
    {
        public Treasury treasury;

        public Treasury_Definition(Treasury treasury)

            : base(Expression_Type.treasury_definition)
        {
            this.treasury = treasury;
        }

        public override IEnumerable<Expression> children
        {
            get { return new List<Expression>(); }
        }
    }
}
