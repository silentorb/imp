namespace imperative.expressions
{
    public class Assignment : Expression
    {
        public string op;
        public Expression target;
        public Expression expression;

        public Assignment(Expression target, string op, Expression expression)
            : base(Expression_Type.assignment)
        {
            this.op = op;
            this.target = target;
            this.expression = expression;
        }

        public override System.Collections.Generic.IEnumerable<Expression> children
        {
            get { return new[] {target, expression}; }
        }
    }
}