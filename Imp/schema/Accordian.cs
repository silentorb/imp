using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.expressions;

namespace imperative.schema
{
    public class Accordian
    {
        public string name;
        public Scope scope;
        List<Division> divisions = new List<Division>();
        Dictionary<string, Division> division_map = new Dictionary<string, Division>();
        private Dungeon dungeon;
        public List<Expression> output;

        public Accordian(string name, Scope scope, Dungeon dungeon, List<Expression> output = null)
        {
            this.name = name;
            this.scope = scope;
            this.dungeon = dungeon;
            this.output = output ?? new List<Expression>();
        }

        public Division divide(string division_name, IEnumerable<Expression> expressions = null)
        {
            var division = new Division()
                {
                    expressions = expressions != null
                        ? expressions.ToList()
                        : new List<Expression>()
                };
            divisions.Add(division);
            if (division_name != null)
                division_map[division_name] = division;

            return division;
        }

        public void add(Expression expression)
        {
            if (divisions.Count == 0)
                divide(null);

            if (expression.type == Expression_Type.statements)
                add_many(((Block)expression).body);
            else
                divisions.First().add(expression);
        }

        public void add_many(IEnumerable<Expression> expressions)
        {
            if (divisions.Count == 0)
                divide(null);

            divisions.First().add_many(expressions);
        }

        public void add(string division_name, Expression expression)
        {
            if (expression.type == Expression_Type.statements)
                add_many(division_name, ((Block)expression).body);
            else
                division_map[division_name].add(expression);
        }

        public void add_many(string division_name, IEnumerable<Expression> expressions)
        {
            division_map[division_name].add_many(expressions);
        }

        public void flatten()
        {
            foreach (var division in divisions)
            {
                output.AddRange(division.expressions);
            }

            divisions = null;
            division_map = null;
        }
    }
}
