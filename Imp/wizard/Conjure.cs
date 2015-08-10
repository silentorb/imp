using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using imperative.schema;

namespace imperative.wizard
{
    public class Optional_Result
    {
        public bool is_match;
        public object value;

        public Optional_Result()
        {
            is_match = false;
        }

        public Optional_Result(object value)
        {
            this.value = value;
            is_match = true;
        }
    }

    public delegate object Conjure(Profession profession);
    public delegate Optional_Result Optional_Conjure(Profession profession);

    public class Conjurer
    {
        List<Optional_Conjure> specialties = new List<Optional_Conjure>();

        public object conjure(Profession profession)
        {
            foreach (var specialty in specialties)
            {
                var result = specialty(profession);
                if (result.is_match)
                {
                    return result.value;
                }
            }

            if (profession == Professions.Float)
                return 0f;

            if (profession == Professions.Int)
                return 0;

            if (profession == Professions.String)
                return "";

            if (profession == Professions.Bool)
                return false;

            if (profession.dungeon == Professions.List)
            {
                return new List<Entity>();
            }

            return instantiate_dungeon(profession);
        }

        public void add_specialty(Optional_Conjure specialty)
        {
            specialties.Add(specialty);
        }

        public Entity instantiate_dungeon(Profession profession)
        {
            return new Entity((Dungeon)profession.dungeon, Guid.NewGuid(), conjure);
        }
    }
}
