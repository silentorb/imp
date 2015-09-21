using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.schema;

namespace imperative.render.artisan
{
    public delegate Stroke Minion_Delegate(Minion expression, Render_Context context, string name);
    public delegate Stroke Flow_Control_Delegate(Flow_Control expression, bool is_if);
    public delegate Stroke Iterator_Delegate(Iterator expression);
    public delegate Stroke Assignment_Delegate(Assignment expression);
    public delegate Stroke Comment_Delegate(Comment expression);
    public delegate Stroke Variable_Declaration_Delegate(Declare_Variable expression);

    public class Statement_Router
    {
        public Minion_Delegate render_function_definition { get; set; }
        public Flow_Control_Delegate render_flow_control { get; set; }
        public Iterator_Delegate render_iterator { get; set; }
        public Assignment_Delegate render_assignment { get; set; }
        public Comment_Delegate render_comment { get; set; }
        public Variable_Declaration_Delegate render_variable_declaration { get; set; }
    }
}
