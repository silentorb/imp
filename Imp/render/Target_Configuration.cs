using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace imperative.render
{
    public enum Type_Mode
    {
        none,
        required_prefix,
        optional_suffix,
    }

    public class Target_Configuration
    {
        public string base_type = "";                   // Haven't decided what Imp's base type will be called yet.
        public bool block_brace_same_line = true;       // Whether a block open-brace starts on the same line or the next.
        public string dependency_keyword = "";          // Sometimes "include", "import", "require", "using".  Not sure what Imp will use.
        public bool explicit_public_members = false;    // Whether members require a "public" prefix to be public.
        public bool implicit_this = true;               // Whether "this" is required to reference local members.
        public int indent = 2;                          // Default indentation.
        public string namespace_keyword = "namespace";  // Usually either namespace or module.
        public string namespace_separator = ".";        // Some languages use ::
        public string path_separator = ".";             // Some languages use ->
        public bool supports_enums = true;              // Whether to resolve enums to integers or leave them as object members.
        public bool supports_namespaces = true;         // True for most of Imp's targets.
        public bool space_tabs = false;                 // Use spaces instead of tabs.
        public string statement_terminator = "";        // In most cases this will be "" or ";"
        public Type_Mode type_mode = Type_Mode.optional_suffix;
        public bool uses_var = true;                    // Whether the language requires/supports "var" for variable declarations.
    }
}
