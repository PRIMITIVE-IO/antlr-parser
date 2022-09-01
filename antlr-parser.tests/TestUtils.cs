using System;
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

    public static string PlatformSpecific(this string str)
    {
        if (Environment.NewLine == "\n" && str.Contains("\r\n"))
        {
            return str.Replace("\r\n", "\n");
        } 
        if (Environment.NewLine == "\r\n" && str.Contains('\n') && !str.Contains("\r\n"))
        {
            return str.Replace("\n", "\r\n");
        }
        return str;
    }
}