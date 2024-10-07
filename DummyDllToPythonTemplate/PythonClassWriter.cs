using System.Text;

namespace DummyDllToPythonTemplate;
public class PythonClassWriter
{
	public StringBuilder Output { get; set; }

	public PythonClassWriter(StringBuilder writer)
	{
		this.Output = writer;
	}

	public void WriteField(string name, string typeDeclaration, int indentionLevel = 1, string? comment = null)
	{
		this.Output.Append('\t', indentionLevel);

		this.Output.Append(name);
		this.Output.Append(": ");
		this.Output.Append(typeDeclaration);
		if (comment is null) goto Final;

		this.Output.Append(" # ");
		this.Output.Append(comment);
	Final:
		this.Output.Append('\n');
	}
	public void WriteFieldWithArrayType(string name, string arrayElementType, int indentionLevel = 1, string arrayType = "list", string? comment = null)
	{
		this.WriteField(name, $"{arrayType}[{arrayElementType}]", indentionLevel, comment);
	}

	public void WriteClassDeclaration(string name, int indentionLevel = 0, string? comment = null)
	{
		this.Output.Append('\t', indentionLevel);

		this.Output.Append("class ");
		this.Output.Append(name);
		this.Output.Append(':');
		if (comment is null) goto Final;

		this.Output.Append(" # ");
		this.Output.Append(comment);
	Final:
		this.Output.Append('\n');
	}
}
