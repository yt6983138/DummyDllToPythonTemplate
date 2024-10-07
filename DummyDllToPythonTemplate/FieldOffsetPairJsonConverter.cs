using System.Text.Json;
using System.Text.Json.Serialization;

namespace DummyDllToPythonTemplate;
internal class FieldOffsetPairJsonConverter : JsonConverter<FieldOffsetPair>
{
	public override FieldOffsetPair? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		throw new NotImplementedException();
	}

	public override void Write(Utf8JsonWriter writer, FieldOffsetPair value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString("Name", value.Name);
		writer.WriteNumber("Offset", value.Offset);

		writer.WriteStartObject("Type");
		writer.WriteBoolean("IsListLike", value.Type.IsArray || value.Type.IsAssignableTo(typeof(System.Collections.IList)));
		string typeName;
		if (value.Type.IsArray)
		{
			typeName = value.Type.GetElementType()!.Name;
		}
		else if (value.Type.IsAssignableTo(typeof(System.Collections.IList)))
		{
			typeName = value.Type.GetGenericArguments()[0].Name;
		}
		else typeName = value.Type.Name;
		writer.WriteString("TypeName", typeName);
		writer.WriteEndObject();

		if (value.SubFields is null)
		{
			writer.WriteNull("SubFields");
			writer.WriteEndObject();
			return;
		}
		writer.WriteStartArray("SubFields");
		foreach (FieldOffsetPair item in value.SubFields)
		{
			this.Write(writer, item, options);
		}
		writer.WriteEndArray();
		writer.WriteEndObject();
	}
}
