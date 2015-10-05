
namespace imperative.schema
{
    public class Dependency
    {
        public Dungeon dungeon;
        public bool allow_partial = true;

        public Dependency(Dungeon dungeon)
        {
            this.dungeon = dungeon;
            if (dungeon.is_value)
                allow_partial = false;
        }

    }
}