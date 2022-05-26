using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using PrimitiveCodebaseElements.Primitive.dto;

namespace antlr_parser.Antlr4Impl
{
    public class ErrorListener : BaseErrorListener
    {
        public void SyntaxError(IRecognizer recognizer, int offendingSymbol, int line, int charPositionInLine,
            string msg, RecognitionException e)
        {
            PrimitiveLogger.Logger.Instance().Error($"{e}: line {line}/column {charPositionInLine} {msg}");
        }
    }

    public static class OriginalText
    {
        /// <summary>
        /// https://stackoverflow.com/questions/26524302/how-to-preserve-whitespace-when-we-use-text-attribute-in-antlr4
        ///
        /// Explanation: Get the first token, get the last token, and get the text from the input stream between the
        /// first char of the first token and the last char of the last token.
        /// </summary>
        /// <param name="context">The context that contains the tokens</param>
        /// <returns>Original new-lined and indented text</returns>
        public static string GetFullText(this ParserRuleContext context)
        {
            if (context == null)
            {
                return "";
            }

            if (context.Start == null ||
                context.Stop == null ||
                context.Start.StartIndex < 0 ||
                context.Stop.StopIndex < 0)
            {
                // Fallback
                return context.GetText();
            }

            return context.Start.InputStream.GetText(Interval.Of(
                context.Start.StartIndex,
                context.Stop.StopIndex));
        }
    }

    public static class AntlrUtil
    {
        public static CodeLocation ToCodeLocation(IToken c)
        {
            return new CodeLocation(c.Line, c.Column);
        }

        public static IEnumerable<ParserRuleContext> FlattenChildren(ParserRuleContext c)
        {
            IEnumerable<ParserRuleContext> flattenedChildren = c.children
                ?.OfType<ParserRuleContext>()
                .SelectMany(FlattenChildren) ?? new List<ParserRuleContext>();

            return new[] { c }.Concat(flattenedChildren);
        }
    }
}