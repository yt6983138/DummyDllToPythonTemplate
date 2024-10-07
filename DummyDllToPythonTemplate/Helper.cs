#if DEBUG
using Dumpify;

#endif

namespace DummyDllToPythonTemplate;
internal static class Helper
{
	internal static readonly Dictionary<Type, string> CSharpPythonTypeMap = new()
	{
		{ typeof(int), "int" },
		{ typeof(short), "short" },
		{ typeof(string), "str" },
		{ typeof(float), "float" },
		{ typeof(Enum), "int" }
	};

	internal static T DumpInternal<T>(this T data)
	{
#if DEBUG
		data.Dump();
#endif
		return data;
	}
	internal static void WriteClass(this PythonClassWriter writer, FieldOffsetPair pair, int indentionLevel = 0)
	{
		if (pair.SubFields is null) throw new ArgumentNullException(nameof(pair.SubFields));

		string classDecName;
		if (pair.Type.IsAssignableTo(typeof(System.Collections.IList)))
		{
			if (pair.Type.IsArray)
				classDecName = pair.Type.GetElementType()!.Name;
			else classDecName = pair.Type.GetGenericArguments()[0].Name;
		}
		else classDecName = pair.Type.Name;

		writer.WriteClassDeclaration(classDecName, indentionLevel);

		indentionLevel++;
		foreach (FieldOffsetPair item in pair.SubFields)
		{
			if (item.Type.IsAssignableTo(typeof(System.Collections.IList)))
			{
				string typeName;
				if (item.Type.IsArray)
				{
					Type elementType = item.Type.GetElementType()!;
					KeyValuePair<Type, string> def = CSharpPythonTypeMap.FirstOrDefault(x => elementType.IsAssignableTo(x.Key));
					typeName = def.Value is null ? elementType.Name : def.Value;
				}
				else
				{
					Type elementType = item.Type.GetGenericArguments()[0];
					KeyValuePair<Type, string> def = CSharpPythonTypeMap.FirstOrDefault(x => elementType.IsAssignableTo(x.Key));
					typeName = def.Value is null ? elementType.Name : def.Value;
				}
				writer.WriteFieldWithArrayType(item.Name, typeName, indentionLevel);
				continue;
			}
			KeyValuePair<Type, string> def2 = CSharpPythonTypeMap.FirstOrDefault(x => item.Type.IsAssignableTo(x.Key));
			writer.WriteField(item.Name, def2.Value is null ? item.Type.Name : def2.Value, indentionLevel);
		}
	}
	internal static void WriteClasses(this PythonClassWriter writer, IEnumerable<FieldOffsetPair> pairs)
	{
		List<FieldOffsetPair> items = new();
		foreach (FieldOffsetPair item in pairs)
		{
			WriteClasses_Recursion(item, items);
		}
		items.AddRange(pairs);
		foreach (FieldOffsetPair item in items)
		{
			if (item.SubFields is not null)
				writer.WriteClass(item);
		}
	}
	private static void WriteClasses_Recursion(FieldOffsetPair pair, List<FieldOffsetPair> toAdd)
	{
		if (pair.SubFields is null) return;
		foreach (FieldOffsetPair item in pair.SubFields)
		{
			WriteClasses_Recursion(item, toAdd);
			toAdd.Add(item);
		}
	}
}
