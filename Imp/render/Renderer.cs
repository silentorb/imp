using System.Collections.Generic;

namespace metahub.render
{
    
    public class Renderer
    {

        int depth = 0;
        //string content = "";
        public string indentation = "";

        public Renderer()
        {

        }

        public string line(string text)
        {
            return indentation + text + "\n";
        }

        public string get_indentation()
        {
            return indentation;
        }

        public Renderer indent(string text)
        {
            ++depth;
            indentation += text;
            return this;
        }

        public Renderer unindent(int offset)
        {
            --depth;
            indentation = indentation.Length > 1
                ? indentation.Substring(0, indentation.Length - offset)
                : "";

            return this;
        }

        //public void add (string text) {
        //content += text;
        //return this;
        //}

        public string newline(int amount = 1)
        {
            int i = 0;
            var result = "";
            while (i++ < amount)
            {
                result += "\n";
            }
            return result;
        }

        public void finish()
        {
            //content = "";
            depth = 0;
            indentation = "";
        }

        public string pad(string content)
        {
            return content == ""
            ? content
            : newline() + content;
        }

    }
}