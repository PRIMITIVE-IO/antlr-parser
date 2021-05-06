using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace antlr_parser.Antlr4Impl
{
    public class MethodBodyRemovalResult
    {

        public readonly string Source;
        
        /// <summary>
        /// Index is a position of last symbol of a function declaration header
        /// It is 'Y' for: fun f(x:X):Y  { 
        /// and ')' for: fun f(x:X){ 
        /// </summary>
        public readonly ImmutableDictionary<int, string> IdxToRemovedMethodBody;

        public static MethodBodyRemovalResult From(string source, ImmutableList<Tuple<int, int>> blocksToRemove)
        {
            ImmutableDictionary<int,string> idxToRemovedMethodBody = ComputeIdxToRemovedMethodBody(blocksToRemove, source);
            string cleanedText = RemoveBlocks(source, blocksToRemove);
            return new MethodBodyRemovalResult(cleanedText, idxToRemovedMethodBody);
        } 
        static string RemoveBlocks(string source, ImmutableList<Tuple<int, int>> removeFromTo)
        {
            return removeFromTo.Reverse()
                .Aggregate(source,
                    (acc, fromTo) =>
                    {
                        var (from, to) = fromTo;
                        return acc.Remove(from, to - from + 1);
                    });
        }

        static ImmutableDictionary<int, string> ComputeIdxToRemovedMethodBody(ImmutableList<Tuple<int, int>> blocksToRemove, string text)
        {
            Dictionary<int, string> idxToRemovedMethodBody = new Dictionary<int, string>();

            int removedLength = 0;
            foreach (var (from, to) in blocksToRemove)
            {
                int lengthToRemove = to - from + 1;
                idxToRemovedMethodBody.Add(from - removedLength - 1, text.Substring(from, lengthToRemove));
                removedLength += lengthToRemove;
            }

            return idxToRemovedMethodBody.ToImmutableDictionary();
        }
        
        MethodBodyRemovalResult(string source, ImmutableDictionary<int, string> idxToRemovedMethodBody)
        {
            Source = source;
            IdxToRemovedMethodBody = idxToRemovedMethodBody;
        }
    }
}