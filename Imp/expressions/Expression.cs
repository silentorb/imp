using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using imperative.schema;
using imperative.summoner;
using runic.parser;

namespace imperative.expressions
{
    public delegate Expression Expression_Generator(Summoner_Context context);
    public delegate bool Expression_Check(Expression expression);

    [DebuggerDisplay("{debug_string}")]
    public abstract class Expression
    {
        public Expression_Type type;
        private Expression _next;
        public Expression next
        {
            get { return _next; }
            set
            {
                if (value == null)
                {
                    if (_next != null)
                        _next.parent = null;
                }
                _next = value;

                if (_next != null)
                    _next.parent = this;
            }
        }

        protected virtual string debug_string
        {
            get { return "Expression(" + type + ")"; }
        }

        public string stack_trace;

        public Expression parent;

        public Expression get_root()
        {
            return parent != null
                ? parent.get_root()
                : this;
        }

        public Legend legend;

        public abstract IEnumerable<Expression> children { get; }

        protected Expression(Expression_Type type, Legend legend = null)
        {
            stack_trace = Environment.StackTrace;
            this.type = type;
            this.legend = legend;
        }

        public virtual Profession get_profession()
        {
            throw new Exception("Not implemented.");
        }

        public Expression get_end()
        {
            var result = this;
            while (result.next != null && (
                result.next.type == Expression_Type.property
                || result.next.type == Expression_Type.portal
                || result.next.type == Expression_Type.function_call))
            {
                result = result.next;
            }

            return result;
        }

        public List<Expression> get_chain()
        {
            var result = new List<Expression>();
            var current = this;
            while (current != null && (current.type == Expression_Type.property || current.type == Expression_Type.portal))
            {
                result.Add(current);
                current = current.next;
            }

            return result;
        }

        public virtual Expression clone()
        {
            throw new Exception("Not implemented.");
        }

        public IEnumerable<Expression> aggregate()
        {
            return new[] { this }.Concat(children.SelectMany(c => c.aggregate()));
        }

        public virtual bool is_empty()
        {
            return false;
        }

        public List<Expression> find(Expression_Type expression_type)
        {
            var result = new List<Expression>();

            foreach (var expression in children)
            {
                if (expression.type == expression_type)
                    result.Add(expression);

                result.AddRange(expression.find(expression_type));
            }

            return result;
        }

        public List<Expression> find(Expression_Check check)
        {
            var result = new List<Expression>();

            foreach (var expression in children)
            {
                if (check(expression))
                    result.Add(expression);

                result.AddRange(expression.find(check));
            }

            return result;
        }

        protected static Expression_Type[] token_types =
        {
            Expression_Type.portal, 
            Expression_Type.function_call,
            Expression_Type.property_function_call,
            Expression_Type.variable
        };

        public bool is_token()
        {
            return token_types.Contains(type);
        }
    }
}