namespace DummyDllToPythonTemplate;
public sealed class ArgParseInfo
{
	public string Name { get; set; }
	public string Description { get; set; }
	public Action<string, Program>? IfArgPresent { get; set; }
	public Action<string, Program>? IfArgNotPresent { get; set; }

	public bool ForceExecuteInDebug { get; set; }

	public char Shortcut { get; set; } = ' ';

	internal string InvokeArg { get; set; } = "";

	public ArgParseInfo(
		string name,
		string description,
		char shortcut = ' ',
		Action<string, Program>? ifArgPresent = null,
		Action<string, Program>? ifArgNotPresent = null,
		bool forceExecuteInDebug = false)
	{
		this.Name = name;
		this.Description = description;
		this.Shortcut = shortcut;
		this.IfArgPresent = ifArgPresent;
		this.IfArgNotPresent = ifArgNotPresent;
		this.ForceExecuteInDebug = forceExecuteInDebug;
	}
}
