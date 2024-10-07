#if DEBUG
using Dumpify;
#endif

namespace DummyDllToPythonTemplate;
internal static class Helper
{
	internal static T DumpInternal<T>(this T data)
	{
#if DEBUG
		data.Dump();
#endif
		return data;
	}
}
