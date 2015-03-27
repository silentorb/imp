using System.Collections.Generic;
using System.Linq;

namespace imperative.expressions
{
    public enum Flow_Control_Type
    {
        If,
        While
    }

    public class Flow_Control : Block
    {
        public Flow_Control_Type flow_type;
        public Expression condition;

        public Flow_Control(Flow_Control_Type flow_type, Expression condition, IEnumerable<Expression> body)
            : base(Expression_Type.flow_control)
        {
            this.flow_type = flow_type;
            this.condition = condition;
            if (condition != null)
                condition.parent = this;

            this.body.AddRange(body);
            foreach (var expression in this.body)
            {
                expression.parent = this;
            }
        }

        public override bool is_empty()
        {
            return condition.is_empty() || body.Count == 0; // With Imp the body count is usually high.
        }

        public override IEnumerable<Expression> children
        {
            get
            {
                return new[] { condition }.Concat(body);
            }
        }
    }
}