using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace library
{
    public class Library
    {
        public static string load_resource(string filename)
        {
            var path = "library.resources." + filename;
            var assembly = Assembly.GetExecutingAssembly();

            var stream = assembly.GetManifestResourceStream(path);
            if (stream == null)
                throw new Exception("Could not find file " + path + ".");

            var reader = new StreamReader(stream);
            return reader.ReadToEnd().Replace("\r\n", "\n");
        }
    }
}
