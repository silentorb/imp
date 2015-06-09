using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;

namespace imperative.render.artisan
{

    public static class Scribe
    {
        const int start = 1;

        public static string render(List<Passage> passages, List<Segment> segments = null)
        {
            int x = 1;
            int y = 1;
            
            var result = new StringBuilder();
            foreach (var passage in passages)
            {
                var text = passage.text;
                if (text != null)
                {
                    result.Append(text);

                    if (text == "\n")
                    {
                        x = start;
                        ++y;
                    }
                    else
                    {
//                        if (text.Contains("\n"))
//                            throw new Exception("Newlines must be separate.");

                        if (segments != null && passage.expression != null)
                            process_segment(passage.expression, x, y, segments);
                        
                        x += text.Length;
                    }
                }
            }

            return result.ToString();
        }

        static void process_segment(Expression expression, int x, int y, List<Segment> segments)
        {
            if (expression.legend == null) 
                return;
            
            var legend = expression.legend;
            var position = legend.position;

            segments.Add(new Segment
            {
                gen_row = y - 1,
                gen_column = x - 1,
                source_file = position.meadow.filename, // Eventually will be indexed to the list of source files
                source_line = position.y - 1,
                source_column = position.x - 1,
                source_token = 0 // May only be needed for when symbols are renamed.
            });
        }
    }
}
