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
            render_statements(result, strokes, "", false);

            return result;
        }

        public static IEnumerable<Passage> render_block(List<Stroke> strokes, string indent)
        {
            var result = new List<Passage>();
            result.Add(new Passage("\n" + indent + spacer));
            render_statements(result, strokes, indent + spacer, true);
            return result;
        }

        public static void render_statements(List<Passage> result, List<Stroke> strokes, string indent, bool is_end)
        {
            for (var i = 0; i < strokes.Count; ++i)
            {
                var addition = render_stroke(strokes[i], indent, is_end && i == strokes.Count - 1);
                if (addition == null)
                    continue;

                if (i > 0 && result.Count > 0)
                {
                    result.Add(new Passage("\n"));
                    result.Add(new Passage(indent));
                }
                //                else
                //                {
                //                    result.Add(new Passage("s" + i));
                //                }
                result.AddRange(addition);
            }
        }

        public static IEnumerable<Passage> render_tokens(List<Stroke> strokes, string indent, bool is_end)
        {
            var result = new List<Passage>();

            foreach (Stroke t in strokes)
            {
                var addition = render_stroke(t, indent, is_end);
                if (addition != null)
                    result.AddRange(addition);

                //                result.Add(new Passage("%"));
            }

            return result;
        }

        public static IEnumerable<Passage> render_stroke(Stroke stroke, string indent, bool is_end)
        {
            if (stroke.type == Stroke_Type.token)
            {
                if (string.IsNullOrEmpty(stroke.full_text()))
                    return null;

                if (stroke.full_text() == "var $injector")
                {
                    var x = 0;
                }
                return new[]
                {
                    new Passage(stroke)
                };
            }

            if (stroke.type == Stroke_Type.newline)
            {
                return ((Stroke_Newline) stroke).ignore_on_block_end && is_end
                    ? null
                    : new[] {new Passage("\n" + indent)};
            }

            var list = (Stroke_List)stroke;
            if (list.children != null && list.children.Count > 0)
            {
                if (stroke.type == Stroke_Type.statements)
                {
                    var result = new List<Passage>();
                    render_statements(result, list.children, indent, is_end);
                    return result;
                }

                if (stroke.type == Stroke_Type.block)
                    return render_block(list.children, indent);

                return render_tokens(list.children, indent, is_end);
            }

            return null;
        }
    }
}
