

namespace imperative.schema
{
/**
 * ...
 * @author Christopher W. Johnson
 */
public class Dependency
{
	public IDungeon dungeon;
	public bool allow_partial = true;

    public Dependency(IDungeon dungeon)
	{
		this.dungeon = dungeon;
	}
	
}
}