using imperative.schema;
using runic.parser;


namespace imperative.expressions
{
    public class Declare_Variable : Expression
    {
        public Symbol symbol;
        public Expression expression;

        public Declare_Variable(Symbol symbol, Expression expression, Legend legend = null)
            : base(Expression_Type.declare_variable, legend)
        {
            this.symbol = symbol;
            this.expression = expression;
        }

        public override Expression clone()
        {
            return new Declare_Variable(symbol, expression, legend);
        }

        public override System.Collections.Generic.IEnumerable<Expression> children
        {
            get { return new[] {expression}; }
        }
    }

}