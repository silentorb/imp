using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace imperative.render.artisan
{
    public static class Painter
    {
        public const string spacer = "    ";

        public static IEnumerable<Passage> render_root(List<Stroke> strokes)
        {
            var result = new List<Passage>();
            render_statements(result, strokes, "");

            return result;
        }

        public static IEnumerable<Passage> render_single_block(List<Stroke> strokes, string indent)
        {
            var result = new List<Passage>();
            result.Add(new Passage("\n"));
            render_statements(result, strokes, indent + spacer);
            return result;
        }

        public static IEnumerable<Passage> render_block(List<Stroke> strokes, string indent)
        {
            var result = new List<Passage>();
            result.Add(new Passage(" {\n"));
            render_statements(result, strokes, indent + spacer);
            result.Add(new Passage("\n" + indent + "}"));
            return result;
        }

        public static IEnumerable<Passage> render_group(List<Stroke> strokes, string indent)
        {
            var result = new List<Passage>();
            render_statements(result, strokes, indent);
            return result;
        }

        public static void render_statements(List<Passage> result, List<Stroke> strokes, string indent)
        {
            for (var i = 0; i < strokes.Count; ++i)
            {
                result.Add(new Passage(indent));

                var addition = render_stroke(strokes[i], indent);
                if (addition == null)
                    continue;

                if (i > 0 && result.Count > 0)
                    result.Add(new Passage("\n"));

                result.AddRange(addition);
            }
        }

        public static IEnumerable<Passage> render_tokens(List<Stroke> strokes, string indent)
        {
            var result = new List<Passage>();

            foreach (Stroke t in strokes)
            {
                var addition = render_stroke(t, indent);
                if (addition != null)
                    result.AddRange(addition);
            }

            return result;
        }

        public static IEnumerable<Passage> render_stroke(Stroke stroke, string indent)
        {
            if (stroke.children != null && stroke.children.Count > 0)
            {
                if (stroke.type == Stroke_Type.group)
                    return render_group(stroke.children, indent);

                if (stroke.type == Stroke_Type.scope || stroke.type == Stroke_Type.block)
                {
                    return stroke.type == Stroke_Type.scope || stroke.children.Count > 1
                        ? render_block(stroke.children, indent)
                        : render_single_block(stroke.children, indent);
                }

                return render_tokens(stroke.children, indent);
            }

            if (string.IsNullOrEmpty(stroke.text))
                return null;

            return new[]
            {
                new Passage(stroke)
            };
        }
    }
}
