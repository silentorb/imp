using imperative;
using imperative.render;
using imperative.schema;
using imperative.expressions;

namespace metahub.render
{
    public class Target
    {
        protected Renderer render = new Renderer();
        protected int line_count;
        protected int column = 1;
        public Overlord overlord;
        public Transmuter transmuter;
        public Target_Configuration config;
        public bool needs_indent = false;

        public Target(Overlord overlord)
        {
            this.overlord = overlord;
            overlord.target = this;
        }

        public virtual void generate_dungeon_code(Dungeon dungeon)
        {

        }

        public virtual void generate_code2(Dungeon dungeon)
        {

        }

        public virtual void run(Overlord_Configuration config1)
        {

        }

        public string line(string text)
        {
            return add(text) + newline();
        }

        public string indent()
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

            render.indent(tab);
            return "";
        }

        public string unindent()
        {
            var offset = config.space_tabs
                ? config.indent
                : 1;

            render.unindent(offset);
            return "";
        }

        public string newline(int amount = 1)
        {
            ++line_count;
            column = 1;
            needs_indent = true;
            return render.newline(amount);
        }

        public string add(string text = "")
        {
            if (needs_indent)
            {
                needs_indent = false;
                var result = render.indentation + text;
                column += result.Length;
                return result;
            }

            column += text.Length;
            return text;
        }

        public virtual void analyze_expression(Expression expression)
        {
        }
    }
}