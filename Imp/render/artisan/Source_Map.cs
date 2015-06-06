using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace imperative.render.artisan
{
    public class Segment
    {
        public int gen_column;
        public int sources_index;
        public int source_line;
        public int source_column;
        public int source_token;
    }

    public class Source_Map
    {
        public int version = 3;
        public string file;
        public string sourceRoot;
        public string[] sources;
        public string[] names = {};
        public string mappings;

        public Source_Map(string file, string[] sources, List<Segment> segments)
        {
            this.file = file;
            this.sources = sources;
        }

        public string serialize()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

}
