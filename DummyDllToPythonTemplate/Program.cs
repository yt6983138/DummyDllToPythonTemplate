using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace DummyDllToPythonTemplate;

public class Program
{
	public static void Main(string[] args) => new Program(args).Run();

	public string[] TypesToDump { get; } =
		["AvatarInfo", "CollectionItemIndex", "Key", "SongsItem"]; // used type will be dumped automatically

	public List<ArgParseInfo> AllowedArgs { get; } = new()
	{
		new("assembly-csharp", "The location of Assembly-CSharp.dll.", 'a',
			(s, p) => p.AssemblyCSharpLocation = File.Exists(s) ? s : throw new ArgumentException("Assembly-CSharp.dll does not exist."),
			(_, _2) => throw new ArgumentException("assembly-csharp option must be present.")),
		new("il2cpp-dummy-dll", "The location of Il2CppDummyDll.dll.", 'i',
			(s, p) => p.Il2CppDummyDllLocation = File.Exists(s) ? s : throw new ArgumentException("Il2CppDummyDll.dll does not exist."),
			(_, _2) => throw new ArgumentException("il2cpp-dummy-dll option must be present.")),
		new("output-py", "The output location of generated python file.", 'p',
			(s, p) => p.OutputPythonLocation = new FileInfo(s).Directory is null ? throw new ArgumentException("Output directory does not exist.") : s),
		new("output-json", "The output location of generated python file.", 'j',
			(s, p) => p.OutputJsonLocation = new FileInfo(s).Directory is null ? throw new ArgumentException("Output directory does not exist.") : s),
		new("verbose", "Verbose mode. True if present.", 'v',
			(_, p) => p.Verbose = true)
	};
	public bool Verbose { get; internal set; } = false;
	public string AssemblyCSharpLocation { get; internal set; } = null!;
	public string Il2CppDummyDllLocation { get; internal set; } = null!;
	public string? OutputPythonLocation { get; internal set; }
	public string? OutputJsonLocation { get; internal set; }
	public Type FieldOffsetAttribute { get; private set; } = null!;
	public FieldInfo FieldOffsetField { get; private set; } = null!;

	public Program(string[] args)
	{
		#region Argument parsing
		if (args.Contains("--help"))
		{
			ShowHelp();
			return;
		}
		List<ArgParseInfo> invokeList = new();
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i].StartsWith("--") && args[i].Length > 2)
			{
				ArgParseInfo? info = this.AllowedArgs.FirstOrDefault(x => x.Name == args[i].Replace("-", ""));
				if (info is null)
				{
					Console.WriteLine($"No option associated with argument '{args[i]}'.");
					ShowHelp();
					return;
				}
				if (i < args.Length - 1 && !args[i + 1].StartsWith('-'))
				{
					info.InvokeArg = args[++i];
					invokeList.Add(info);
					continue;
				}
				invokeList.Add(info);
				continue;
			}
			if (args[i].StartsWith('-') && args[i].Length == 2)
			{
				ArgParseInfo? info = this.AllowedArgs.FirstOrDefault(x => x.Shortcut == args[i][1]);
				if (info is null)
				{
					Console.WriteLine($"No option associated with shortcut '{args[i]}'.");
					ShowHelp();
					return;
				}
				if (i < args.Length - 1 && !args[i + 1].StartsWith('-'))
				{
					info.InvokeArg = args[++i];
					invokeList.Add(info);
					continue;
				}
				invokeList.Add(info);
				continue;
			}
			Console.WriteLine($"Invalid option '{args[i]}'.");
			ShowHelp();
			return;
		}
		foreach (ArgParseInfo info in this.AllowedArgs)
		{
			bool debug =
#if DEBUG
				true;
#else
				false;
#endif
			if (invokeList.Contains(info) || (info.ForceExecuteInDebug && debug))
				info.IfArgPresent?.Invoke(info.InvokeArg, this);
			else
				info.IfArgNotPresent?.Invoke("", this);
		}
		void ShowHelp()
		{
			Console.WriteLine("--help: Show this help.");
			foreach (ArgParseInfo info in this.AllowedArgs)
				Console.WriteLine($"--{info.Name}{(info.Shortcut == ' ' ? "" : $", {info.Shortcut}")}: {info.Description}");
		}
		#endregion
	}

	#region Logging
	internal void LogVerbose(string format, params object[] arg)
	{
		if (this.Verbose)
			Console.WriteLine(format, arg);
	}
	internal void LogInfo(string format, params object[] arg)
	{
		Console.WriteLine(format, arg);
	}
	[DoesNotReturn]
	internal void LogCritical(string format, params object[] arg)
	{
		Console.WriteLine(format, arg);
		Environment.Exit(1);
	}
	#endregion

	public void Run()
	{
		this.LogVerbose("Initializing {0}", this.AssemblyCSharpLocation);
		Assembly assemblyCSharp = Assembly.LoadFrom(this.AssemblyCSharpLocation);
		this.LogVerbose("Initializing {0}", this.Il2CppDummyDllLocation);
		Assembly il2cppDummy = Assembly.LoadFrom(this.Il2CppDummyDllLocation);
		this.LogVerbose("Initializing FieldOffsetAttribute");
		this.FieldOffsetAttribute = il2cppDummy.GetTypes().First(x => x.Name == "FieldOffsetAttribute");
		this.FieldOffsetField = this.FieldOffsetAttribute.GetField("Offset")!;
		this.LogInfo("Load success, loading types");
		List<FieldOffsetPair> infos = this.TypesToDump
			.Select(assemblyCSharp.GetType)
			.Select(x => new FieldOffsetPair(this, x!, 0, "class_declaration"))
			.ToList()
			.DumpInternal();
		if (this.OutputJsonLocation is not null)
		{

		}
		MemoryStream stream = new();
		StreamWriter writer = new(stream);
		new PythonClassWriter(writer).WriteClasses(infos);
		writer.Close();
		stream.Close();
		Console.WriteLine(Encoding.UTF8.GetString(stream.GetBuffer()));
		this.LogInfo("Done");
	}
}
