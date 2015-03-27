using System.Collections.Generic;
using System.Linq;
using imperative.schema;


namespace imperative.expressions
{
    public class Function_Definition : Block
    {
        public string name;
        public Dungeon dungeon;
        public Scope scope;
        public Minion minion;
        public Profession return_type { get { return minion.return_type; } }
        public bool is_abstract { get { return minion.is_abstract; } }
        public List<Expression> expressions { get { return minion.expressions; } }
        public List<Parameter> parameters { get { return minion.parameters; } }

        public Function_Definition(Minion minion)
            : base(Expression_Type.function_definition)
        {
            this.minion = minion;
            name = minion.name;
            dungeon = minion.dungeon;
        }

        public override IEnumerable<Expression> children
        {
            get { return expressions; }
        }
    }

}