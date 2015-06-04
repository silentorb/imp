﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using imperative.expressions;

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
            type = Stroke_Type.literal;
            this.text = text;
        }

        public override string ToString()
        {
            return text ?? "";
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
                return a;
            }

            return new Stroke(Stroke_Type.chain, new List<Stroke>
            {
                a
            }.Concat(b).ToList());
        }

        public Stroke chain(Stroke next)
        {
            if (next == null)
                return this;

            if (type == Stroke_Type.chain)
            {
                children.Add(next);
                return this;
            }

            return new Stroke(Stroke_Type.chain, new List<Stroke>
            {
                this, next
            });
        }
    }
}
