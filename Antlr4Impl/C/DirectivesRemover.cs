using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace antlr_parser.Antlr4Impl.C
{
    /// <summary>
    /// Removes preprocessor directives (#if, #ifdef, #ifndef, #else, #elif, #endif) alongside with their ELSE brances
    /// and keeps only THEN blocks. Works with nested directives as well. 
    /// </summary>
    public class DirectivesRemover
    {
        //matches text from # to end of a line
        static readonly Regex IfRegex = new Regex("^[ \\t]*#if((n)?def)?.*?$", RegexOptions.Multiline);
        static readonly Regex ElseRegex = new Regex("^[ \\t]*#(elif|else).*?$", RegexOptions.Multiline);
        static readonly Regex EndifRegex = new Regex("^[ \\t]*#endif.*?$", RegexOptions.Multiline);

        enum DirectiveType
        {
            If,
            Else,
            Endif
        }

        class DirectiveOccurence
        {
            public readonly Match Match;
            public readonly DirectiveType Type;

            public DirectiveOccurence(Match match, DirectiveType type)
            {
                Type = type;
                Match = match;
            }
        }

        public static List<Tuple<int, int>> FindBlocksToRemove(string source)
        {
            List<Tuple<int, int>> blocksToRemove = new List<Tuple<int, int>>();

            MatchCollection ifMatches = IfRegex.Matches(source);
            MatchCollection endifMatches = EndifRegex.Matches(source);
            MatchCollection elseMatches = ElseRegex.Matches(source);

            List<DirectiveOccurence> directives = ifMatches.Select(it => new DirectiveOccurence(it, DirectiveType.If))
                .Concat(elseMatches.Select(it => new DirectiveOccurence(it, DirectiveType.Else)))
                .Concat(endifMatches.Select(it => new DirectiveOccurence(it, DirectiveType.Endif)))
                .OrderBy(it => it.Match.Index)
                .ToList();

            int startRemoveBlock = -1;
            bool inThenBlock = true;
            Stack<bool> inThenBlockStack = new Stack<bool>();

            foreach (DirectiveOccurence directive in directives)
            {
                switch (directive.Type)
                {
                    case DirectiveType.If:
                    {
                        inThenBlockStack.Push(inThenBlock);
                        if (inThenBlock)
                        {
                            blocksToRemove.Add(Tuple.Create(directive.Match.Index,
                                directive.Match.Index + directive.Match.Length));
                        }

                        break;
                    }
                    case DirectiveType.Else:
                    {
                        // if it is the first ELSE block
                        if (inThenBlock)
                            //remember first ELSE block position (to remove starting from this point later)
                            startRemoveBlock = directive.Match.Index;
                        inThenBlock = false;
                        break;
                    }
                    case DirectiveType.Endif:
                    {
                        // if there was an ELSE block and current IF is in THEN branch
                        if (!inThenBlock && inThenBlockStack.Peek())
                        {
                            //remove block from the first ELSE (in current IF) until first index of #endif
                            blocksToRemove.Add(Tuple.Create(startRemoveBlock, directive.Match.Index - 1));
                            startRemoveBlock = -1;
                        }

                        inThenBlock = inThenBlockStack.Pop();
                        if (inThenBlock)
                        {
                            blocksToRemove.Add(Tuple.Create(directive.Match.Index,
                                directive.Match.Index + directive.Match.Length));
                        }

                        break;
                    }
                }
            }

            return blocksToRemove.ToList();
        }
    }
}