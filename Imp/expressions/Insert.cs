using System.Collections.Generic;

namespace imperative.expressions
{

    /**
     * ...
     * @author Christopher W. Johnson
     */
    public class Insert : Expression
    {
        public string code;

        public Insert(string code)

            : base(Expression_Type.insert)
        {
            this.code = code;
        }

        public override IEnumerable<Expression> children
        {
            get { return new List<Expression>(); }
        }
    }
}