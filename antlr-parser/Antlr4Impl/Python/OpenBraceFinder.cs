using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace antlr_parser.Antlr4Impl.Python
{
    public class OpenBraceFinder
    {
        private readonly Stack<char> Stack = new Stack<char>();

        public bool CompletedExpression(string line)
        {
            for (int i = line.Length - 1; i >= 0; i--)
            {
                if (line[i] == ')' || line[i] == ']')
                {
                    Stack.Push(line[i]);
                    continue;
                }

                if (line[i] == '(' || line[i] == '[')
                {
                    char expected = Stack.Pop() switch
                    {
                        ')' => '(',
                        ']' => '[',
                        _ => throw new Exception()
                    };

                    if (line[i] != expected)
                        PrimitiveLogger.Logger.Instance()
                            .Warn($"line: {line} at {i} contains {line[i]}, expected: {expected}");
                }
            }

            return !Stack.Any();
        }
    }
}