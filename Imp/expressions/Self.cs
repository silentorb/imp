using System.Collections.Generic;
using imperative.schema;
using metahub.schema;

namespace imperative.expressions
{
    public sealed class Self : Expression
    {
        public Dungeon dungeon;

        public Self(Dungeon dungeon, Expression child = null)
            : base(Expression_Type.self)
        {
            this.dungeon = dungeon;
            next = child;
        }

        public override Profession get_profession()
        {
            return new Profession(Kind.reference, dungeon);
        }

        public override Expression clone()
        {
            return new Self(dungeon, next != null ? next.clone() : null);
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