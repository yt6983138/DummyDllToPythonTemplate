using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DummyDllToPythonTemplate;

public class Program
{
	public static void Main(string[] args) => new Program(args).Run();


	public record class FieldOffsetPair(Type Type, int Offset, List<FieldOffsetPair>? SubFields)
	{
		public static readonly string[] DisallowedFindFieldTypes = ["Int32", "Int16", "Byte", "Boolean", "Single", "String", "Dictionary`2"];

		internal FieldOffsetPair(Program program, Type type, int offset)
			: this(type, offset, new())
		{
			IEnumerable<FieldInfo> subFields = type.GetFields()
				.Where(x => x.IsPublic && !x.IsStatic);
			foreach (FieldInfo item in subFields)
			{
				Type declaringType = item.DeclaringType!;
				if (DisallowedFindFieldTypes.Contains(declaringType.Name))
					continue;
				if (declaringType.IsArray)
				{
					if (!declaringType.IsSZArray)
						continue; // jagged
					Type elementType = declaringType.GetElementType()!;
					if (elementType.IsArray)
						continue; // like type[][]
				}
				if (declaringType.Name == "List`1")
				{
					Type generic = declaringType.GetGenericArguments()[0];
					if (generic.IsArray || generic.Name == "List`1")
						continue;
				}

				int fieldOffset = Convert.ToInt32((string)program.FieldOffsetField.GetValue(item.GetCustomAttribute(program.FieldOffsetAttribute))!, 16);

				this.SubFields!.Add(new(program, declaringType, fieldOffset));
			}
			if (this.SubFields!.Count == 0)
				this.SubFields = null;
		}
	}

	public const string GameInformationTypePath = "GameInformation";

	public List<ArgParseInfo> AllowedArgs { get; } = new()
	{
		new("assembly-csharp", "The location of Assembly-CSharp.dll.", 'a',
			(s, p) => p.AssemblyCSharpLocation = File.Exists(s) ? s : throw new ArgumentException("Assembly-CSharp.dll does not exist."),
			(_, _2) => throw new ArgumentException("assembly-csharp option must be present.")),
		new("il2cpp-dummy-dll", "The location of Il2CppDummyDll.dll.", 'i',
			(s, p) => p.Il2CppDummyDllLocation = File.Exists(s) ? s : throw new ArgumentException("Il2CppDummyDll.dll does not exist."),
			(_, _2) => throw new ArgumentException("assembly-csharp option must be present.")),
		new("output-file", "The output location of generated python file.", 'o',
			(s, p) => p.OutputLocation = new FileInfo(s).Directory is null ? throw new ArgumentException("Output directory does not exist.") : s),
		new("verbose", "Verbose mode. True if present.", 'v',
			(_, p) => p.Verbose = true)
	};
	public bool Verbose { get; set; } = false;
	public string AssemblyCSharpLocation { get; set; } = null!;
	public string Il2CppDummyDllLocation { get; set; } = null!;
	public string? OutputLocation { get; set; }
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

	internal void LogVerbose(string format, params object[] arg)
	{
		if (this.Verbose)
			Console.WriteLine(format, arg);
	}
	[DoesNotReturn]
	internal void LogCritical(string format, params object[] arg)
	{
		Console.WriteLine(format, arg);
		Environment.Exit(1);
	}
	public void Run()
	{
		this.LogVerbose("Loading asm csharp from {0}", this.AssemblyCSharpLocation);
		Assembly assemblyCSharp = Assembly.LoadFrom(this.AssemblyCSharpLocation);
		Assembly il2cppDummy = Assembly.LoadFrom(this.Il2CppDummyDllLocation);
		this.FieldOffsetAttribute = il2cppDummy.GetTypes().First(x => x.Name == "FieldOffsetAttribute");
		this.FieldOffsetField = this.FieldOffsetAttribute.GetField("Offset")!;
		this.LogVerbose("Load success, finding game info");
		Type? gameInformation = assemblyCSharp.GetType(GameInformationTypePath);
		if (gameInformation is null)
			this.LogCritical("GameInformation not found!");
		FieldOffsetPair test = new(this, gameInformation, 0);
	}
}
