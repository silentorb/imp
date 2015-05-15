using System;
using System.Collections.Generic;
using imperative.schema;

using Kind = metahub.schema.Kind;

namespace imperative.expressions {
/**
 * ...
 * @author Christopher W. Johnson
 */
public class Literal : Expression {
	public object value;
    //public Signature signature;
    public Profession profession;

    public Literal(object value, Profession profession)

        : base(Expression_Type.literal)
    {
        //if (value == null)
        //   throw new Exception("Literal value cannot be null.");

        this.value = value;
        this.profession = profession;
    }

    public Literal(int value)

        : base(Expression_Type.literal)
    {
        this.value = value;
        profession = Professions.Int;
    }

    public Literal(string value)

        : base(Expression_Type.literal)
    {
        this.value = value;
        profession = Professions.String;
    }

    public Literal(float value)

        : base(Expression_Type.literal)
    {
        this.value = value;
        profession = Professions.Float;
    }

    public Literal(bool value)

        : base(Expression_Type.literal)
    {
        this.value = value;
        profession = Professions.Bool;
    }

    public override Expression clone()
    {
        return new Literal(value, profession);
    }

    public float get_float()
    {
        return profession == Professions.Float
            ? (int) value 
            : (float) value;
    }

    public override IEnumerable<Expression> children
    {
        get { return new List<Expression>(); }
    }

    public override Profession get_profession()
    {
        return profession;
    }
}}