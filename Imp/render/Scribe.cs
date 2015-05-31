using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace imperative.render
{
    public class Source_Map
    {
        public int version = 3;
        public string file;
        public string sourceRoot;
        public string[] sources;
        public string[] names;
        public string mappings;

        public Source_Map(string file, string[] sources, List<Passage> passages)
        {
            this.file = file;
            this.sources = sources;
            mappings = generate_mappings(passages);
        }

        public string generate_mappings(List<Passage> passages)
        {
            StringBuilder result = new StringBuilder();

            foreach (var passage in passages)
            {
                
            }
        }
    }

    public static class Scribe
    {

        public static string render_source_map(List<Passage> passages)
        {
            var map = new Source_Map(passages);

        }
    }
}
