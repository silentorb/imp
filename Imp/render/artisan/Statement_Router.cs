using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.schema;

namespace imperative.render.artisan
{
    public delegate Stroke Minion_Delegate(Minion expression);
    public delegate Stroke Flow_Control_Delegate(Flow_Control expression, bool is_if);
    public delegate Stroke Iterator_Delegate(Iterator expression);
    public delegate Stroke Assignment_Delegate(Assignment expression);
    public delegate Stroke Comment_Delegate(Comment expression);
    public delegate Stroke Variable_Declaration_Delegate(Declare_Variable expression);

    internal interface Statement_Router
    {
        Minion_Delegate render_function_definition { get; set; }
        Flow_Control_Delegate render_flow_control { get; set; }
        Iterator_Delegate render_iterator { get; set; }
        Assignment_Delegate render_assignment { get; set; }
        Comment_Delegate render_comment { get; set; }
        Variable_Declaration_Delegate render_variable_declaration { get; set; }
    }
}
