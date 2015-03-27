using System.Collections.Generic;
using imperative.schema;

namespace imperative.expressions
{
    public class Namespace : Block
    {
        public Realm realm;

        public Namespace(Realm realm, List<Expression> block)
            : base(Expression_Type.space)
        {
            this.realm = realm;
            this.body = block;
        }
    }
}