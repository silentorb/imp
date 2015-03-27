namespace imperative.schema
{

public class Used_Function {
	public string name;
	public bool is_platform_specific;

    public Used_Function(string name, bool is_platform_specific = false)
    {
		this.name = name;
		this.is_platform_specific = is_platform_specific;
	}
}
}