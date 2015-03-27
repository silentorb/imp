using imperative.schema;


namespace imperative.expressions
{
    public class Parameter
    {
        public Symbol symbol;
        public Expression default_value;

        public Parameter(Symbol symbol, Expression default_value = null)
        {
            this.symbol = symbol;
            this.default_value = default_value;
        }
    }
}