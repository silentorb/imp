using System.Collections.Generic;
using imperative.schema;


namespace imperative.expressions
{

    public class Class_Definition : Block
    {
        public Dungeon dungeon;

        public Class_Definition(Dungeon dungeon, List<Expression> statements)

            : base(Expression_Type.class_definition, statements)
        {
            this.dungeon = dungeon;
        }
    }
}