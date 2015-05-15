using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace imperative.render
{
    public class Render_Context
    {
        public bool needs_indent = false;
        public int line_count = 0;
        public int depth = 0;

        public Render_Context(int depth)
        {
            this.depth = depth;
        }

        public string newline()
        {
            ++line_count;
            needs_indent = true;
            return "\n";
        }
    }
}
