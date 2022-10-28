using System;

namespace antlr_parser
{
    public static class Utils
    {
        [Obsolete("should be replaced with the same function from PrimitiveCodebaseElements")]
        public static R Let<T, R>(this T t, Func<T, R> block)
        {
            return block(t);
        }
    }
}