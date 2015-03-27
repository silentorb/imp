using System;
using System.Collections.Generic;
using System.Linq;
using imperative.schema;



namespace imperative.expressions
{
    public enum Property_Function_Type
    {
        get,
        set,
        remove
    }

    public class Property_Function_Call : Method_Call
    {
        public Portal portal;
        public Property_Function_Type function_type;

        public Property_Function_Call(Property_Function_Type function_type, Portal portal, IEnumerable<Expression> args = null)
            : base(null, args)
        {
            type = Expression_Type.property_function_call;
            this.function_type = function_type;
            this.portal = portal;
        }

        public override Expression clone()
        {
            return new Property_Function_Call(function_type, portal, args)
                {
                    reference = reference
                };
        }

        public override IEnumerable<Expression> children
        {
            get
            {
                return reference != null
                    ? new[] { reference }.Concat(args)
                    : args;
            }
        }
    }

}