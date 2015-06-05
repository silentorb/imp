using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using imperative.expressions;
using metahub.render;

namespace imperative.render.artisan
{
    public class Stroke
    {
        public Expression expression;
        public string text;
        public Stroke_Type type;
        public List<Stroke> children = new List<Stroke>();

        public Stroke()
        {
            type = Stroke_Type.empty;
        }

        public Stroke(Stroke_Type type)
        {
            this.type = type;
        }

        public Stroke(Expression expression, string text)
        {
            type = Stroke_Type.token;
            this.expression = expression;
            this.text = text;
        }

        public Stroke(Stroke_Type type, Expression expression)
        {
            this.type = type;
            this.expression = expression;
        }

        public Stroke(Stroke_Type type, List<Stroke> children)
        {
            this.type = type;
            this.children = children;
        }

        public Stroke(string text)
        {
            type = Stroke_Type.token;
            this.text = text;
        }

        public Stroke copy()
        {
            return new Stroke(type, expression)
            {
                children = children,
                text = text
            };
        }

//        public override string ToString()
//        {
//            throw new Exception();
////            return text ?? "";
//        }

        public string full_text()
        {
            if (children.Count > 0)
            {
                return children.Select(c => c.text).join("");
            }

            return text;
        }

        public static Stroke operator +(Stroke a, Stroke b)
        {
            return a.chain(b);
        }

        public static Stroke operator +(Stroke a, List<Stroke> b)
        {
            if (b.Count == 0)
                return a;

            if (a.type == Stroke_Type.chain)
            {
                a.children = a.children.Concat(b).ToList();
                return a.copy();
            }

            return new Stroke(Stroke_Type.chain, new List<Stroke>
            {
                a.copy()
            }.Concat(b).ToList());
        }

        public Stroke chain(Stroke next)
        {
            if (next == null)
                return this;

            if (type == Stroke_Type.chain)
            {
                var result = copy();
                result.children.Add(next);
                return result;
            }

            return new Stroke(Stroke_Type.chain, new List<Stroke>
            {
                copy(), next.copy()
            });
        }

        public static List<Stroke> join(List<Stroke> list, string separator)
        {
            if (list.Count < 2)
                return list;

            var result = new List<Stroke>(list.Count + list.Count - 1);
            result.Add(list[0]);
            
            for (int i = 1; i < list.Count; i++)
            {
                result.Add(new Stroke(separator));
                result.Add(list[i]);
            }

            return result;
        } 
    }
}
