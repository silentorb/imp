using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using imperative.schema;
using imperative.summoner;



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

        //private Expression _parent = null;
        //public Expression parent
        //{
        //    get { return _parent; }
        //    set
        //    {
        //        if (value != null && !value.children.Contains(this))
        //            value.children.Add(this);

        //        if (value == _parent)
        //            return;

        //        if (_parent != null && _parent.children.Contains(this))
        //            _parent.children.Remove(this);

        //        _parent = value;

        //    }
        //}
        public abstract IEnumerable<Expression> children { get; }

        protected Expression(Expression_Type type)
        {
            stack_trace = Environment.StackTrace;
            this.type = type;
        }

        //public void add(Expression expression)
        //{
        //    expression.parent = this;
        //}

        //public void add(IEnumerable<Expression> expressions)
        //{
        //    foreach (var child in expressions)
        //    {
        //        child.parent = this;
        //    }
        //}

        public virtual Profession get_profession()
        {
            throw new Exception("Not implemented.");
        }

        public Expression get_end()
        {
            var result = this;
            while (result.next != null && (result.next.type == Expression_Type.property || result.next.type == Expression_Type.portal))
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

        //public virtual Expression next
        //{
        //    get { return null; }
        //    set
        //    {
        //        throw new Exception("Set next not implemented.");
        //    }
        //}

        public List<Expression> find(Expression_Type expression_type)
        {
            var result = new List<Expression>();

            foreach (var expression in children)
            {
                if (expression.type== expression_type)
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
    }
}