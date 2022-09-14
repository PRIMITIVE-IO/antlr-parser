using System.Linq;
using PrimitiveCodebaseElements.Primitive.dto;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.Antlr4Impl
{
    public class CodeRangeCalculator
    {
        readonly string[] lines;

        public CodeRangeCalculator(string text)
        {
            lines = text.Split("\n");
        }

        CodeLocation? NextNonWhitespace(CodeLocation loc)
        {
            string firstLine = lines[loc.Line - 1];

            for (int colIdx = loc.Column - 1; colIdx < firstLine.Length; colIdx++)
            {
                if (!char.IsWhiteSpace(firstLine[colIdx]))
                {
                    return new CodeLocation(loc.Line, colIdx + 1);
                }
            }

            //Starting from next line idx
            for (int lineIdx = loc.Line; lineIdx < lines.Length; lineIdx++)
            {
                string currentLine = lines[lineIdx];

                for (int colIdx = 0; colIdx < currentLine.Length; colIdx++)
                {
                    if (!char.IsWhiteSpace(currentLine[colIdx]))
                    {
                        return new CodeLocation(lineIdx + 1, colIdx + 1);
                    }
                }
            }

            //if there are only whitespaces
            return null;
        }

        public CodeLocation? PreviousNonWhitespace(CodeLocation loc)
        {
            string firstLine = lines[loc.Line - 1];

            for (int colIdx = loc.Column - 1; colIdx >= 0; colIdx--)
            {
                if (colIdx == firstLine.Length) continue;
                if (!char.IsWhiteSpace(firstLine[colIdx]))
                {
                    return new CodeLocation(loc.Line, colIdx + 1);
                }
            }

            //Starting from previous line
            for (int lineIdx = loc.Line - 2; lineIdx >= 0; lineIdx--)
            {
                string currentLine = lines[lineIdx];

                for (int colIdx = currentLine.Length - 1; colIdx >= 0; colIdx--)
                {
                    if (colIdx == currentLine.Length) continue;
                    if (!char.IsWhiteSpace(currentLine[colIdx]))
                    {
                        return new CodeLocation(lineIdx + 1, colIdx + 1);
                    }
                }
            }

            //if there are only whitespaces
            return null;
        }

        public CodeRange Trim(CodeRange codeRange)
        {
            return new CodeRange(
                NextNonWhitespace(codeRange.Start) ?? codeRange.Start,
                PreviousNonWhitespace(codeRange.End) ?? codeRange.End
            );
        }

        public CodeLocation EndPosition()
        {
            return new CodeLocation(lines.Length, lines.Last().Length);
        }
    }
}