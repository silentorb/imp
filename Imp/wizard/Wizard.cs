using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;
using imperative.schema;
using imperative.summoner;
using metahub.schema;

namespace imperative.wizard
{
    class Wizard
    {
        public static object resolve_expression(Expression expression, Summoner_Context context)
        {
            object result = null;

            switch (expression.type)
            {
                case Expression_Type.literal:
                    var literal = (Literal)expression;
                    result = literal.value;
//                    result = resolve_literal(literal.value, literal.profession);
                    break;

                case Expression_Type.operation:
                    return resolve_operation((Operation)expression, context);

                case Expression_Type.portal:
                    throw new Exception("Not implemented.");
//                    result = render_portal((Portal_Expression)expression);
//                    break;

                case Expression_Type.function_call:
                    throw new Exception("Not implemented.");
//                    result = render_function_call((Abstract_Function_Call)expression, parent);
//                    break;

                case Expression_Type.property_function_call:
                    throw new Exception("Not implemented.");
//                    result = render_property_function_call((Property_Function_Call)expression, parent);
//                    break;

                case Expression_Type.platform_function:
                    throw new Exception("Not implemented.");
//                    return render_platform_function_call((Platform_Function)expression, null);

                case Expression_Type.instantiate:
                    throw new Exception("Not implemented.");
//                    result = render_instantiation((Instantiate)expression);
//                    break;

                case Expression_Type.self:
                    throw new Exception("Not implemented.");
//                    result = render_this();
//                    break;

                case Expression_Type.null_value:
                    return null;

                case Expression_Type.profession:
                    throw new Exception("Not implemented.");
//                    result = expression.get_profession().dungeon.name;
//                    break;

                case Expression_Type.create_array:
                    throw new Exception("Not implemented.");
//                    result = "FOOO";
//                    break;

                case Expression_Type.anonymous_function:
                    throw new Exception("Not implemented.");
//                    return render_anonymous_function((Anonymous_Function)expression);

                case Expression_Type.comment:
                    throw new Exception("Not implemented.");
//                    return render_comment((Comment)expression);

                case Expression_Type.variable:
                    throw new Exception("Not implemented.");
//                    var variable_expression = (Variable)expression;
//                    //if (find_variable(variable_expression.symbol.name) == null)
//                    //    throw new Exception("Could not find variable: " + variable_expression.symbol.name + ".");
//
//                    result = variable_expression.symbol.name;
//                    if (variable_expression.index != null)
//                        result += "[" + render_expression(variable_expression.index) + "]";
//
//                    break;

                case Expression_Type.parent_class:
                    throw new Exception("Not implemented.");
//                    result = current_dungeon.parent.name;
//                    break;

                case Expression_Type.insert:
                    throw new Exception("Not implemented.");
//                    result = ((Insert)expression).code;
//                    break;

                case Expression_Type.jewel:
                    throw new Exception("Not implemented.");
//                    var jewel = (Jewel)expression;
//                    return render_enum_value(jewel.treasury, jewel.value);

                default:
                    throw new Exception("Unsupported Expression type: " + expression.type + ".");
            }

            return result;
        }

        private static object resolve_operation(Operation operation, Summoner_Context context)
        {
            var expressions = operation.children.Select(c => resolve_expression(c, context)).ToArray();

            switch (operation.op)
            {
                case "!=":
                    return expressions[0] != expressions[1];
            }

            throw new Exception("Not implemented.");
        }

//        public static object resolve_literal(Object value, Profession profession)
//        {
//            if (profession == null)
//                return value.ToString();
//
//            switch (profession.type)
//            {
//                case Kind.unknown:
//                    return value.ToString();
//
//                case Kind.Float:
//                case Kind.Int:
//                case Kind.String:
//                case Kind.Bool:
//                    return value;
//
//                case Kind.reference:
//                    if (value == null)
//                        return null;
//
//                    if (!profession.dungeon.is_value)
//                        throw new Exception("Literal expressions must be scalar values.");
//
//                    if (profession.dungeon.GetType() == typeof(Treasury))
//                        throw new Exception("Not implemented.");
////                        return render_enum_value((Treasury)profession.dungeon, (int)value);
//
////                    if (value != null)
////                        return value.ToString();
//
//                    return profession.dungeon;
////                    return render_dungeon_name(profession.dungeon) + "()";
//
//                default:
//                    throw new Exception("Invalid literal " + value + " type " + profession.type + ".");
//            }
//        }
    }
}
