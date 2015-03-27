using imperative;
using imperative.render;
using imperative.schema;
using imperative.expressions;

namespace metahub.render
{
    public class Target
    {
        protected Renderer render = new Renderer();
        protected int line_count = 0;
        public Overlord overlord;
        public Transmuter transmuter;
        public Target_Configuration config;
        public bool needs_indent = false;

        public Target(Overlord overlord)
        {
            this.overlord = overlord;
            if (overlord != null)
                overlord.target = this;
        }

        public virtual void generate_dungeon_code(Dungeon dungeon)
        {

        }

        public virtual void generate_code2(Dungeon dungeon)
        {

        }

        public virtual void run(string output_folder)
        {

        }

        public string line(string text)
        {
            return add(text) + newline();
        }

        public Renderer indent()
        {
            var tab = "\t";
            if (config.space_tabs)
            {
                tab = "";

                for (var i = 0; i < config.indent; ++i)
                {
                    tab += " ";
                }
            }

            return render.indent(tab);
        }

        public Renderer unindent()
        {
            var offset = config.space_tabs
                ? config.indent
                : 1;

            return render.unindent(offset);
        }

        public string newline(int amount = 1)
        {
            ++line_count;
            needs_indent = true;
            return render.newline(amount);
        }

        public string pad(string content)
        {
            return content == ""
            ? content
            : newline() + content;
        }

        public string add(string text)
        {
            if (needs_indent)
            {
                needs_indent = false;
                return render.indentation + text;
            }

            return text;
        }

        public virtual void analyze_expression(Expression expression)
        {
        }
    }
}