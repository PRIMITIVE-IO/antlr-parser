using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.Python
{
    public static class PythonMethodBodyRemover
    {
        static Regex MethodDeclarationRegex = new(@"def \w*\(");
        static Regex MethodDeclarationEndRegex = new(@":\s*\r?\n");

        public static List<Tuple<int, int>> FindBlocksToRemove(string source)
        {
            string newLineChar = source.Contains("\r\n") ? "\r\n" : "\n";
            return MethodDeclarationRegex.Matches(source).Select(methodDef =>
                {
                    int methodIndentation = source.IndentationAt(methodDef.Index);
                    int closedParenthesisPosition =
                        source.ClosedParenthesisPosition(methodDef.Index + methodDef.Length - 1);
                    int endMethodDefIdx = MethodDeclarationEndIdx(source, closedParenthesisPosition);
                    // jump to next char and preserve line ending
                    int fromIdx = (source.IndexAtLastCommentChar(endMethodDefIdx) ?? endMethodDefIdx) + 1 + newLineChar.Length;
                    int toIdx = source.LastMethodLineIdx(methodIndentation, fromIdx, newLineChar);

                    return Tuple.Create(fromIdx, toIdx);
                })
                .ToList();
        }

        static int MethodDeclarationEndIdx(string source, int startFrom)
        {
            return MethodDeclarationEndRegex.Match(source, startFrom).Index;
        }

        static int? IndexAtLastCommentChar(this string source, int startFrom)
        {
            int tripleQuoteEnd = 0;
            int linesBeforeComment = 0;
            for (int i = startFrom; i < source.Length; i++)
            {
                if (OnTripleQuoteEnd(source, i))
                {
                    tripleQuoteEnd = i;
                    break;
                }

                if (source[i] == '\n')
                {
                    linesBeforeComment++;
                }

                if (linesBeforeComment > 1 || i == source.Length - 1)
                {
                    return null;
                }
            }

            int commentEnd = 0;
            for (int i = tripleQuoteEnd + 1; i < source.Length; i++)
            {
                if (OnTripleQuoteEnd(source, i))
                {
                    return i;
                }
            }

            return null;
        }

        static bool OnTripleQuoteEnd(string source, int i)
        {
            return source[i - 2] == '"' && source[i - 1] == '"' && source[i] == '"';
        }

        static int IndentationAt(this string source, int idx)
        {
            if (idx == 0) return 0;
            int indentation = 0;
            for (int i = idx - 1; i >= 0; i--)
            {
                if (source[i] == ' ' || source[i] == '\t') indentation++;
                else if (source[i] == '\n') break;
            }

            return indentation;
        }

        public static int ClosedParenthesisPosition(this string source, int firstParenthesisPosition)
        {
            if (source[firstParenthesisPosition] != '(')
            {
                throw new Exception(
                    $"Expected to find '(' but {source[firstParenthesisPosition]} found at {firstParenthesisPosition}");
            }

            int num = 0;
            for (int index = firstParenthesisPosition; index < source.Length; ++index)
            {
                switch (source[index])
                {
                    case '(':
                        ++num;
                        break;
                    case ')':
                        --num;
                        break;
                }

                if (num == 0)
                    return index;
            }

            throw new Exception(
                $"Cannot find close parenthesis starting from {firstParenthesisPosition} for: {source}");
        }

        static int LastMethodLineIdx(this string source, int indentation, int startFrom, string newLineChar)
        {
            OpenBraceFinder openBraceFinder = new OpenBraceFinder();
            int excerptLength = source[startFrom..].Split(newLineChar)
                .TakeWhile(line => Indentation(line) > indentation || line.IsBlank())
                .Reverse()
                .SkipWhile(line => line.IsBlank()) // skip empty lines after method
                .SkipWhile(line => !openBraceFinder.CompletedExpression(line))
                .Skip(1) // skip last non-empty line of the method
                .Aggregate(0, (acc, line) => acc + line.Length + newLineChar.Length);

            // keep new line char
            // int extraNewLineCharCount = excerptLength == 0 ? 0 : newLineChar.Length;
            return startFrom + excerptLength - 1;
        }

        static int Indentation(string s)
        {
            return s.TakeWhile(c => Char.IsWhiteSpace(c) && c != '\r').Count();
        }
    }
}