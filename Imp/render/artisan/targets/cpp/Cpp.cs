using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.legion;
using imperative.schema;
using metahub.render;
using metahub.render.targets;

namespace imperative.render.artisan.targets.cpp
{
    public class Cpp : Common_Target2
    {
        private static Target_Configuration static_config;

        public Cpp(Overlord overlord = null)
            : base(overlord)
        {
            static_config = config = new Target_Configuration()
             {
                 float_suffix = true,
                 statement_terminator = ";",
                 dependency_keyword = "using",
                 space_tabs = true,
                 indent = 4,
                 block_brace_same_line = false,
                 explicit_public_members = false,
                 type_mode = Type_Mode.required_prefix,
                 namespace_separator = "::",
                 list_start = "{",
                 list_end = "}"
             };

            types["string"] = "std::string";
            minion_names[Professions.List.minions["push"]] = "push_back";
        }

        public override void run(Build_Orders config1, string[] sources)
        {
            if (config1.name != "" && config1 is Project)
                CMake.create_files((Project)config1, overlord);

            foreach (var dungeon in config1.dungeons.Values)
            {
                if (dungeon.is_external || (dungeon.is_abstract && dungeon.is_external))
                    continue;

                render_full_dungeon(dungeon, config1);
                //Console.WriteLine(dungeon.realm.name + "." + dungeon.name);
            }
        }

        public void render_full_dungeon(Dungeon dungeon, Build_Orders config1)
        {
            if (dungeon.portals.Length > 0 || dungeon.minions.Count > 0)
            {
                var space = Generator.get_namespace_path(dungeon.realm);
                var dir = config1.output + "/" + space.join("/");
                Generator.create_folder(dir);

                var name = dir + "/" + dungeon.name;

                Generator.create_file(name + ".h",
                    Overlord.stroke_to_string(Header_File.generate_header_file(this, dungeon)));

                Generator.create_file(name + ".cpp",
                    Overlord.stroke_to_string(Source_File.generate_source_file(this, dungeon)));
            }

            //            foreach (var child in dungeon.dungeons.Values)
            //            {
            //                render_full_dungeon(child, config1);
            //            }
        }

        public List<Stroke> generate_source_strokes()
        {
            var output = new List<Stroke>();
            foreach (var dungeon in overlord.dungeons)
            {
                if (dungeon.is_external || dungeon.is_external
                    || dungeon.name == "")
                    continue;

                output.Add(Source_File.generate_source_file(this, dungeon));
            }

            return output;
        }

        public static List<Dungeon> get_dungeon_parents(Dungeon dungeon)
        {
            var parents = new List<Dungeon>();
            if (dungeon.parent != null)
                parents.Add(dungeon.parent);

            return parents.Concat(dungeon.interfaces).ToList();
        }

        public static Stroke render_profession2(Symbol symbol, bool is_parameter = false)
        {
            if (symbol.profession != null)
                return render_profession2(symbol.profession, is_parameter);

            return render_profession2(symbol.profession, is_parameter);
        }

        public static Stroke render_profession2(Profession signature, bool is_parameter = false)
        {
            if (signature.dungeon == Professions.List)
                return listify(render_profession2(signature.children[0]), signature);
            //            throw new Exception("Not implemented.");
            var lower_name = signature.dungeon.name.ToLower();
            var name = types.ContainsKey(lower_name)
                ? new Stroke_Token(types[lower_name])
                : render_dungeon_path(signature.dungeon);

            if (!signature.dungeon.is_value)
                name = new Stroke_Token("std::shared_ptr<") + name + new Stroke_Token(">");

            return name;
        }

        public static Stroke render_definition_parameter2(Parameter parameter)
        {
            return Cpp.render_profession2(parameter.symbol, true) + new Stroke_Token(" " + parameter.symbol.name);
        }

        public static Stroke render_includes(IEnumerable<External_Header> headers)
        {
            return new Stroke_List(Stroke_Type.statements, headers
                .Select(h => (Stroke)new Stroke_Token(h.is_standard
                    ? "#include <" + h.name + ">"
                    : "#include \"" + h.name + ".h\""
                    )).ToList()) { margin_bottom = 1 };
        }

        public static bool has_header(IEnumerable<External_Header> list, string name)
        {
            return list.Any(header => header.name == name);
        }

        override public Stroke listify(Stroke type, Profession signature)
        {
            return new Stroke_Token("std::vector<") + type + new Stroke_Token(">");
        }

        protected override Stroke render_platform_function_call(Platform_Function expression, Expression parent)
        {
            throw new Exception("Not implemented.");
        }

        override protected Stroke render_variable_declaration(Declare_Variable declaration)
        {
            var result = new Stroke_Token("auto " + declaration.symbol.name)
                + (declaration.expression != null
                    ? new Stroke_Token(" = ") + render_expression(declaration.expression)
                    : null)
                + terminate_statement();

            result.expression = declaration;
            return result;
        }

    }
}
