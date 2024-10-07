namespace DummyDllToPythonTemplate;
public class PythonClassWriter
{
	public StreamWriter Output { get; set; }

	public PythonClassWriter(StreamWriter writer)
	{
		this.Output = writer;
	}

	public void WriteField(string name, string typeDeclaration, int indentionLevel = 1, string? comment = null)
	{
		for (int i = 0; i < indentionLevel; i++)
			this.Output.Write('\t');

		this.Output.Write(name);
		this.Output.Write(": ");
		this.Output.Write(typeDeclaration);
		if (comment is null) goto Final;

		this.Output.Write(" # ");
		this.Output.Write(comment);
	Final:
		this.Output.Write('\n');
	}
	public void WriteFieldWithArrayType(string name, string arrayElementType, int indentionLevel = 1, string arrayType = "list", string? comment = null)
	{
		this.WriteField(name, $"{arrayType}[{arrayElementType}]", indentionLevel, comment);
	}

	public void WriteClassDeclaration(string name, int indentionLevel = 0, string? comment = null)
	{
		for (int i = 0; i < indentionLevel; i++)
			this.Output.Write('\t');

		this.Output.Write("class ");
		this.Output.Write(name);
		this.Output.Write(":");
		if (comment is null) goto Final;

		this.Output.Write(" # ");
		this.Output.Write(comment);
	Final:
		this.Output.Write('\n');
	}
}
