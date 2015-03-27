using System;
using System.Collections.Generic;
using imperative.schema;


using metahub.schema;

namespace imperative.expressions
{
    public sealed class Variable : Expression
    {
        public Symbol symbol;
        public Expression index;

        public Variable(Symbol symbol, Expression child = null)
            : base(Expression_Type.variable)
        {
            if (symbol == null)
               throw new Exception("Variable symbol cannot be null.");

            this.symbol = symbol;
            next = child;
        }

        public override Profession get_profession()
        {
            return symbol.profession;
        }

        public override Expression clone()
        {
            return new Variable(symbol, next != null ? next.clone() : null)
            {
                index = index != null ? index.clone() : null
            };
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