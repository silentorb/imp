using System;
using System.Collections.Generic;
using imperative.schema;
using metahub.schema;

namespace imperative.expressions
{
    public class Comment : Expression
    {
        public string text;
        public bool is_multiline;

        public Comment(string text)
            : base(Expression_Type.comment)
        {
            this.text = text;
            is_multiline = text.Contains("\n");
        }

        public override IEnumerable<Expression> children
        {
            get { return new List<Expression>(); }
        }
    }
}