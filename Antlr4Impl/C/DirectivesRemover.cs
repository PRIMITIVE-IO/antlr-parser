using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace antlr_parser.Antlr4Impl.C
{
    public class DirectivesRemover
    {
        //matches text from # to end of a line
        private readonly static Regex ifRegex = new Regex("^[ \\t]*#if((n)?def)?.*?$", RegexOptions.Multiline);
        private readonly static Regex elseRegex = new Regex("^[ \\t]*#(elif|else).*?$", RegexOptions.Multiline);
        private readonly static Regex endifRegex = new Regex("^[ \\t]*#endif.*?$", RegexOptions.Multiline);

        public static ImmutableList<Tuple<int, int>> FindBlocksToRemove(string source)
        {
            List<Tuple<int, int>> blocksToRemove = new List<Tuple<int, int>>();
            MatchCollection ifMatches = ifRegex.Matches(source);
            MatchCollection endifMatches = endifRegex.Matches(source);
            MatchCollection elseMatches = elseRegex.Matches(source);

            if (ifMatches.Count != endifMatches.Count)
                throw new Exception(
                    $"Invalid count of 'if' ({ifMatches.Count()}) and 'endif' ({endifMatches.Count}) directives. {source}");

            for (int i = 0; i < ifMatches.Count; i++)
            {
                Match ifMatch = ifMatches[i];
                Match endifMatch = endifMatches[i];

                if (endifMatch.Index < ifMatch.Index) throw new Exception("endif before if");

                //remove #if (ifdef, ifndef) directive from '#' to '\n'
                blocksToRemove.Add(Tuple.Create(ifMatch.Index, ifMatch.Index + ifMatch.Length));

                //remove else block from '#' until '#endif'
                Match firstElse =
                    elseMatches.FirstOrDefault(it => ifMatch.Index < it.Index && it.Index < endifMatch.Index);
                if (firstElse != null)
                {
                    blocksToRemove.Add(Tuple.Create(firstElse.Index, endifMatch.Index - 1));
                }

                //remove #endif
                blocksToRemove.Add(Tuple.Create(endifMatch.Index, endifMatch.Index + endifMatch.Length));
            }

            return blocksToRemove.ToImmutableList();
        }
    }
}