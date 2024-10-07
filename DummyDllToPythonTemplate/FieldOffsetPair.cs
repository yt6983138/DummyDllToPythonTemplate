using System.Reflection;

namespace DummyDllToPythonTemplate;
public class FieldOffsetPair
{
	public static readonly Type[] DisallowedFindFieldOffsetTypes =
		[typeof(int), typeof(short), typeof(byte), typeof(bool), typeof(float), typeof(string), typeof(Enum), typeof(System.Collections.IList)];
	public static readonly Type[] DisallowedAddSubFieldTypes = [typeof(System.Collections.IDictionary)];
	public static readonly Type[] GenericDisallowedAddSubFieldTypes = [typeof(System.Collections.IList)];

	public Type Type { get; private set; }
	public int Offset { get; }
	public List<FieldOffsetPair>? SubFields { get; }

	internal FieldOffsetPair(Program program, Type type, int offset, bool findSubFields = true)
	{
		this.Type = type;
		this.Offset = offset;
		this.SubFields = new();

		if (!findSubFields)
		{
			this.SubFields = null;
			return;
		}
		IEnumerable<FieldInfo> subFields = type.GetFields()
			.Where(x => x.IsPublic && !x.IsStatic);
		foreach (FieldInfo item in subFields)
		{
			Type fieldType = item.FieldType!;
			if (DisallowedAddSubFieldTypes.Any(fieldType.IsAssignableTo))
				continue;

			bool shouldFindFields = true;
			if (DisallowedFindFieldOffsetTypes.Any(fieldType.IsAssignableTo))
				shouldFindFields = false;

			Type? elementType = null;
			if (fieldType.IsArray)
			{
				if (!fieldType.IsSZArray)
					continue; // jagged
				elementType = fieldType.GetElementType()!;
				if (elementType.IsArray)
					continue; // like type[][]
				shouldFindFields = true;
			}
			if (!fieldType.IsArray && fieldType.IsAssignableTo(typeof(System.Collections.IList)))
			{
				Type generic = fieldType.GetGenericArguments()[0];
				if (generic.IsArray)
					continue;
				if (generic.IsGenericType && generic.GetGenericTypeDefinition().IsAssignableTo(typeof(System.Collections.IList)))
					continue;
				shouldFindFields = true;
				elementType = generic;
			}

			int fieldOffset = Convert.ToInt32((string)program.FieldOffsetField.GetValue(item.GetCustomAttribute(program.FieldOffsetAttribute))!, 16);

			FieldOffsetPair toAdd = new(program, elementType ?? fieldType, fieldOffset, shouldFindFields);
			if (elementType is not null)
				toAdd.Type = fieldType;
			this.SubFields!.Add(toAdd);
		}
		if (this.SubFields!.Count == 0)
			this.SubFields = null;
	}

	public override string ToString()
	{
		return $"{{\n\tType: {this.Type}\n\tOffset: {this.Offset}\n\tSubFields: {(this.SubFields is null ? "none" : string.Join(", \n\t", this.SubFields) + "}")} \n}}";
	}
}
