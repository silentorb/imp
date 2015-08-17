using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using imperative.expressions;
using metahub.render;

namespace imperative.render.artisan
{
    public abstract class Stroke
    {
        public Expression expression;
        public Stroke_Type type;
        public int margin_top;
        public int margin_bottom;

        public abstract Stroke copy();

        public override string ToString()
        {
            throw new Exception();
        }

        public abstract string full_text();

        public static Stroke operator +(Stroke a, Stroke b)
        {
            return Stroke.chain(a, b);
        }

        public static Stroke operator +(Stroke a, List<Stroke> b)
        {
            if (b.Count == 0)
                return a;

            if (a.type == Stroke_Type.chain)
            {
                var a2 = (Stroke_List)a;
                a2.children = a2.children.Concat(b).ToList();
                return a.copy();
            }

            return new Stroke_List(Stroke_Type.chain, new List<Stroke>
            {
                a.copy()
            }.Concat(b).ToList());
        }

        public static Stroke chain(Stroke a, Stroke b)
        {
            if (a == null)
                return b;

            if (b == null)
                return a;

            if (a.type == Stroke_Type.chain)
            {
                var result = (Stroke_List)a.copy();
                result.children.Add(b);
                return result;
            }

            return new Stroke_List(Stroke_Type.chain, new List<Stroke>
            {
                a.copy(), b.copy()
            });
        }

        public static List<Stroke> join(IEnumerable<Stroke> list, string separator)
        {
            return Stroke.join(list.ToList(), separator);
        }

        public static List<Stroke> join(List<Stroke> list, string separator)
        {
            if (list.Count < 2)
                return list;

            var result = new List<Stroke>(list.Count + list.Count - 1);
            result.Add(list[0]);

            for (int i = 1; i < list.Count; i++)
            {
                result.Add(new Stroke_Token(separator));
                result.Add(list[i]);
            }

            return result;
        }

        public static bool contains_block(Stroke stroke)
        {
            return stroke.type == Stroke_Type.block
                || stroke.type == Stroke_Type.chain
                && ((Stroke_List)stroke).children.Any(contains_block);
        }
    }

    [DebuggerDisplay("Token {text}")]
    public class Stroke_Token : Stroke
    {
        public string text;

        public Stroke_Token(string text = "", Expression expression = null)
        {
            this.text = text;
            type = Stroke_Type.token;
            this.expression = expression;
        }

        public override Stroke copy()
        {
            return this;
        }

        public override string full_text()
        {
            return text;
        }
    }

    public class Stroke_Newline : Stroke
    {
        public bool ignore_on_block_end = false;

        public Stroke_Newline()
        {
            type = Stroke_Type.newline;
        }

        public override Stroke copy()
        {
            return this;
        }

        public override string full_text()
        {
            return "\n";
        }
    }

    public class Stroke_List : Stroke
    {
        public List<Stroke> children = new List<Stroke>();

        public Stroke_List(Stroke_Type type, List<Stroke> children, Expression expression = null)
        {
            this.type = type;
            this.expression = expression;
            this.children = children;
        }

        public override Stroke copy()
        {
            return new Stroke_List(type, children, expression);
        }

        public override string full_text()
        {
            return children.Select(c => c.full_text()).join("");
        }
    }

}
