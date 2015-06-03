using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace imperative.render.artisan
{
 
    public static class Scribe
    {
        public static string render_source_map(List<Passage> passages)
        {
//            var map = new Source_Map(passages);
//            throw new Exception("Not implemented.");

            var result = new StringBuilder();
            foreach (var passage in passages)
            {
                if (passage.text != null)
                    result.Append(passage.text);
            }

            return result.ToString();
        }
    }
}
