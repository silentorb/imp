using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace imperative.render.artisan
{
    public class Segment
    {
        public int gen_row;
        public int gen_column;
        public int sources_index;
        public int source_line;
        public int source_column;
        public int source_token;
    }

    public static class Base_64
    {
        public static char[] lookup =
        {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
            'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
            'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            '0','1','2','3','4','5','6','7','8','9','+','/'
        };
    }

    public class Source_Map
    {
        public int version = 3;
        public string file;
        public string sourceRoot = "";
        public string[] sources;
        public string[] names = { };
        public string mappings;

        public Source_Map(string file, string[] sources, List<Segment> segments)
        {
            this.file = file;
            this.sources = sources;
            mappings = generate_map(segments);
        }

        string generate_map(List<Segment> segments)
        {
            if (segments.Count == 0)
                return "";

            var result = new StringBuilder();
            int row = 0;
            var last = segments[0];

            if (last.gen_row > row)
            {
                catch_up(result, last.gen_row - row);
                row = last.gen_row;
            }

            var sequence =
                compress(last.gen_column) +
                compress(last.sources_index) +
                compress(last.source_line) +
                compress(last.source_column);
//                    + compress(last.source_token);

            result.Append(sequence);

            for (var i = 1; i < segments.Count; ++i)
            {
                var segment = segments[i];
                if (segment.gen_row == row + 1)
                {
                    ++row;
                    result.Append(";");
                }
                else if (segment.gen_row > row)
                {
                    catch_up(result, segment.gen_row - row);
                    row = segment.gen_row;
                }
                else
                {
                    result.Append(",");
                }

                sequence =
                    compress(segment.gen_column - last.gen_column) +
                    compress(segment.sources_index) +
                    compress(segment.source_line - last.source_line) +
                    compress(segment.source_column - last.source_column);
//                    compress(segment.source_token);

                result.Append(sequence);
                last = segment;
            }

            return result.ToString();
        }

        public void catch_up(StringBuilder result, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                result.Append("AAAA;");
            }
        }

        public string serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        static string compress(int value)
        {
            var abs = Math.Abs(value);
            var is_negative = value < 0 ? 16 : 0;

            if (abs <= 15)
            {
                return Base_64.lookup[(abs << 1) + is_negative].ToString();
            }

            if (abs <= 495)
            {
                return new string(new[]
                {
                    Base_64.lookup[(abs << 1 & 15) + 32 + is_negative],
                    Base_64.lookup[abs << 1 & 63 >> 4]
                });
            }

            if (abs <= 15872)
            {
                return new string(new[]
                {
                    Base_64.lookup[(abs << 1 & 15) + 32 + is_negative],
                    Base_64.lookup[(abs << 1 & 63 >> 4) + 32],
                    Base_64.lookup[abs << 1 & 495 >> 5]
                });
            }

            throw new Exception("Not yet implemented.  Converting numbers as large as " + value + ".");
        }
    }

}
