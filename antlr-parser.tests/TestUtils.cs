using System.IO;
using System.Reflection;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.tests;

public static class TestUtils
{
    public static CodeRange CodeRange(int lineStart, int columnStart, int lineEnd, int columnEnd)
    {
        return new CodeRange(new CodeLocation(lineStart, columnStart), new CodeLocation(lineEnd, columnEnd));
    }

    public static string Resource(string name)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string resourceName = $"antlr_parser.tests.Resources.{name}";
        using Stream stream = assembly.GetManifestResourceStream(resourceName);
        using StreamReader reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}