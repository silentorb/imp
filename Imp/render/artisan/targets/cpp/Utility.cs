using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.schema;
using metahub.render.targets;

namespace imperative.render.artisan.targets.cpp
{
    public static class Utility
    {

        public static bool is_pointer_or_shared(Profession signature)
        {
            return !signature.dungeon.is_value && !signature.is_generic_parameter;
        }

        public static bool is_shared_pointer(Profession signature, bool is_type = false)
        {
            if (signature.cpp_type != Cpp_Type.none && signature.cpp_type != Cpp_Type.shared_pointer)
                return false;

            return !signature.dungeon.is_value && !signature.is_generic_parameter && !is_type;
        }

        public static bool is_shared_pointer(Expression expression, bool is_type = false)
        {
            var end = expression.get_end();
            if (end.type != Expression_Type.portal && end.type != Expression_Type.variable)
                return false;

            if (end.type == Expression_Type.portal)
            {
                var portal = ((Portal_Expression)end).portal;
                if (!portal.is_owner)
                    return false;
            }

            var profession = expression.get_profession();
            return is_shared_pointer(profession, is_type);
        }

        public static Stroke render_profession2(Profession signature, Render_Context context,
            bool is_parameter = false, bool is_type = false)
        {
            if (signature.dungeon == Professions.List)
                return Cpp.listify2(render_profession2(signature.children[0], context), signature);

            if (signature.dungeon == Professions.Dictionary)
                return render_dictionary_profession(signature, context);

            var lower_name = signature.dungeon.name.ToLower();

            var name = Cpp.types.ContainsKey(lower_name)
                ? new Stroke_Token(Cpp.types[lower_name])
                : render_dungeon_path2(signature.dungeon, context);

            if (signature.children != null && signature.children.Count > 0)
            {
                name += new Stroke_Token("<")
                    + Stroke.join(signature.children.Select(p => render_profession2(p, context)), ", ")
                    + new Stroke_Token(">");
            }

            if (signature.cpp_type == Cpp_Type.pointer)
            {
                name = name + new Stroke_Token("*");
            }
            else if (is_shared_pointer(signature, is_type))
            {
                name = name + new Stroke_Token("*");
                //                name = shared_pointer(name);
            }
            //            if (is_parameter && !signature.dungeon.is_value)
            //                name += new Stroke_Token("&");

            return name;
        }

        public static Stroke render_dictionary_profession(Profession profession, Render_Context context)
        {
            return new Stroke_Token("std::map<")
                + render_profession2(profession.children[0], context)
                + new Stroke_Token(", ")
                + render_profession2(profession.children[1], context)
                + new Stroke_Token(">");
        }

        public static Stroke dereference_shared_pointer()
        {
            return new Stroke_Token("&*");
        }

        public static Stroke shared_pointer(Stroke stroke)
        {
            return new Stroke_Token("std::shared_ptr<") + stroke + new Stroke_Token(">");
        }

        public static Stroke unique_pointer(Stroke type, Stroke expression)
        {
            return new Stroke_Token("&*std::unique_ptr<") + type + new Stroke_Token(">(") + expression + new Stroke_Token(")");
        }

        public static Stroke render_dungeon_path2(IDungeon dungeon, Render_Context context)
        {
            return dungeon.realm != null && dungeon.realm.name != ""
                && (dungeon.realm != context.realm)
                ? render_dungeon_path2(dungeon.realm, context) + new Stroke_Token("::" + dungeon.name)
                : new Stroke_Token(dungeon.name);
        }

        public static Stroke render_definition_parameter2(Parameter parameter, Render_Context context)
        {
            return render_profession2(parameter.symbol.profession, context, true) + new Stroke_Token(" " + parameter.symbol.name);
        }

        public static Stroke render_includes(IEnumerable<External_Header> headers)
        {
            return new Stroke_List(Stroke_Type.statements, headers
                .Select(render_include).ToList()) { margin_bottom = 1 };
        }

        public static string render_source_path(Dungeon dungeon)
        {
            return Common_Functions.render_realm_path_string(dungeon, "/");
        }

        public static Stroke render_include(External_Header header)
        {
            if (string.IsNullOrEmpty(header.name))
                throw new Exception("Empty header file name");

            return new Stroke_Token(header.is_standard
                ? "#include <" + header.name + ">"
                : "#include \"" + header.name + ".h\""
                );
        }

        public static bool has_header(IEnumerable<External_Header> list, string name)
        {
            return list.Any(header => header.name == name);
        }

    }
}
