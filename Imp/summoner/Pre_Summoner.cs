//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Text.RegularExpressions;
//using imperative.Properties;
//using parser;
//using Match = parser.Match;
//
//namespace imperative.summoner
//{
//    public class Pre_Summoner : Parser_Context
//    {
//        public static Regex remove_comments = new Regex("//[^\n]*");
//        public Pattern_Source output;
//
//        public enum Mode
//        {
//            full,
//            snippet
//        }
//
//        public Pre_Summoner()
//            : base(null)
//        {
//            load_parser();
//        }
//
//        public void summon(string code, Mode mode)
//        {
//            load_parser();
//            var without_comments = remove_comments.Replace(code, "");
//            var start = mode == Mode.full
//                ? definition.patterns[0]
//                : definition.patterns[1];
//
//            //trace("without_comments", without_comments);
//            var result = parse(without_comments, start);
//
//            if (!result.success)
//            {
//                Debug_Info.output(result);
//                throw new Exception("Syntax Error at " + result.end.y + ":" + result.end.x);
//            }
//
//            var match = (Match)result;
//            output = match.get_data();
//        }
//
//        public void load_parser()
//        {
//            Definition boot_definition = new Definition();
//            boot_definition.load_parser_schema();
//            Bootstrap context = new Bootstrap(boot_definition);
//
//            var result = context.parse(Resources.imp_grammar, boot_definition.patterns[0], false);
//            //Debug_Info.output(result);
//            if (result.success)
//            {
//                var match = (Match)result;
//
//                definition = new Definition();
//                definition.load(match.get_data().dictionary);
//            }
//            else
//            {
//                throw new Exception("Error loading parser.");
//            }
//        }
//
//
//        public override object perform_action(string name, Pattern_Source data, Match match)
//        {
//            if (data.type == null)
//                data.type = match.pattern.name;
//
//            if (match.pattern.name == null)
//                return data;
//
//            switch (match.pattern.name)
//            {
//                case "start":
//                    return data.patterns[1];
//
//                case "string":
//                    data = data.patterns[1];
//                    data.type = "string";
//                    break;
//
//                case "optional_expression":
//                    return data.patterns[1];
//
//                case "expression":
//                    //data.dividers = ((Repetition_Match) match).dividers.Select(d => d.get_data().patterns[1].text).ToArray();
//                    return process_expression(data, (Repetition_Match)match);
//                //break;
//
//                case "reference_token":
//                    data.text = data.patterns[0].text;
//                    break;
//
//                case "optional_assignment":
//                    return data.patterns[3];
//
//                //case "type_info":
//                //    return data.patterns[2];
//
//                case "optional_arguments":
//                    return data.patterns[1];
//
//                case "closed_expression":
//                    return data.patterns[2];
//
//                case "optional_parent_classes":
//                    return data.patterns[2];
//
//                case "arguments":
//                    data.patterns = data.patterns[2].patterns;
//                    break;
//
//                case "optional_array":
//                    return data.patterns[2];
//
//                case "short_block":
//                    data.type = "block";
//                    data.patterns = new[] { data.patterns[1] };
//                    break;
//
//                case "long_block":
//                    data.type = "block";
//                    data.patterns = data.patterns[2].patterns;
//                    break;
//
//                case "id_with_optional_array":
//                    data.type = "id";
//                    data.text = data.patterns[0].text;
//                    data.patterns = data.patterns[1].patterns;
//                    break;
//
//                case "long_block_any":
//                    return data.patterns[2];
//
//                case "snippet_entry":
//                    data = data.patterns[1];
//                    data.type = "snippets";
//                    break;
//
//                case "optional_long_block":
//                    return data.patterns[1];
//            }
//
//            return data;
//        }
//
//        static Pattern_Source process_expression(Pattern_Source data, Repetition_Match match)
//        {
//            if (data.patterns.Length < 2)
//                return data;
//
//            var rep_match = match;
//            var dividers = rep_match.dividers
//                .Select(d => d.matches.First(m => m.pattern.name != "trim").get_data().text).ToList();
//
//            var patterns = data.patterns.ToList();
//            MetaHub_Context.group_operators(new[] { "/", "*" }, patterns, dividers);
//            MetaHub_Context.group_operators(new[] { "+", "-" }, patterns, dividers);
//            MetaHub_Context.group_operators(new[] { "!=", "==" }, patterns, dividers);
//            MetaHub_Context.group_operators(new[] { "||", "&&" }, patterns, dividers);
//            data.patterns = patterns.ToArray();
//
//            if (dividers.Count > 0)
//                data.text = dividers[0];
//
//            return data;
//        }
//    }
//}
