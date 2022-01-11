using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.tests
{
    public static class TestUtils
    {
        public static CodeRange CodeRange(int lineStart, int columnStart, int lineEnd, int columnEnd)
        {
            return new CodeRange(new CodeLocation(lineStart, columnStart), new CodeLocation(lineEnd, columnEnd));
        }
    }
}