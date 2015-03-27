using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using imperative.schema;

namespace imperative.expressions
{
    public class Portal_Expression : Expression
    {
        public Portal portal;
        public Expression index;

        public Portal_Expression(Portal portal, Expression child = null)

            : base(Expression_Type.portal)
        {
            if (portal == null)
                throw new Exception("portal cannot be null.");

            this.portal = portal;
            portal.expressions.Add(this);
            next = child;
        }

        public override Expression clone()
        {
            return new Portal_Expression(portal, next != null ? next.clone() : null)
                {
                    index = index != null ? index.clone() : null
                };
        }

        public override Profession get_profession()
        {
            return portal.get_profession();
        }

        protected override string debug_string
        {
            get { return "Portal_Expression " + portal.fullname; }
        }

        public override IEnumerable<Expression> children
        {
            get
            {
                return next != null
                           ? new List<Expression> { next }
                           : new List<Expression>();
            }
        }
    }
}
