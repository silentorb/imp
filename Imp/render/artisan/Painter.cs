using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace imperative.render.artisan
{
    public static class Painter
    {
        public const string spacer = "  ";
        private static int counter = 1;

        public static IEnumerable<Passage> render_root(List<Stroke> strokes)
        {
            var result = new List<Passage>();
            render_statements(result, strokes, "");

            return result;
        }

        public static IEnumerable<Passage> render_block(List<Stroke> strokes, string indent)
        {
            var result = new List<Passage>();
            result.Add(new Passage("\n"));
            render_statements(result, strokes, indent + spacer);
            return result;
        }

        public static void render_statements(List<Passage> result, List<Stroke> strokes, string indent)
        {
            for (var i = 0; i < strokes.Count; ++i)
            {
                var addition = render_stroke(strokes[i], indent);
                if (addition == null)
                    continue;

                if (i > 0 && result.Count > 0)
                {
                    result.Add(new Passage("\n"));
                    result.Add(new Passage(indent));
                }
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
            if (stroke.type == Stroke_Type.token)
            {
                if (string.IsNullOrEmpty(stroke.full_text()))
                    return null;

                return new[]
                {
                    new Passage(stroke)
                };
            }

            var list = (Stroke_List)stroke;
            if (list.children != null && list.children.Count > 0)
            {
                if (stroke.type == Stroke_Type.statements)
                {
                    var result = new List<Passage>();
                    render_statements(result, list.children, indent);
                    return result;
                }

                if (stroke.type == Stroke_Type.block)
                    render_block(list.children, indent);

                return render_tokens(list.children, indent);
            }

            return null;
        }
    }
}
