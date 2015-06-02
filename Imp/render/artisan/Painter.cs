using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace imperative.render.artisan
{
    public static class Painter
    {
        public static IEnumerable<Passage> render_list(List<Stroke> strokes)
        {
            return strokes.SelectMany(s => render_stroke(s));
        }

        public static IEnumerable<Passage> render_stroke(Stroke stroke)
        {
            if (stroke.children != null && stroke.children.Count > 0)
                return render_list(stroke.children);

            if (string.IsNullOrEmpty(stroke.text))
                return new Passage[] { };

            return new []
            {
                new Passage(stroke),
            };
        }
    }
}
