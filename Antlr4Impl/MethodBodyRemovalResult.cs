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
            ImmutableList<Tuple<int, int>> topLevelBlocksToRemove = RemoveNested(blocksToRemove);
            ImmutableDictionary<int,string> idxToRemovedMethodBody = ComputeIdxToRemovedMethodBody(topLevelBlocksToRemove, source);
            string cleanedText = RemoveBlocks(source, topLevelBlocksToRemove);
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
                idxToRemovedMethodBody[from - removedLength - 1] = text.Substring(from, lengthToRemove);
                removedLength += lengthToRemove;
            }

            return idxToRemovedMethodBody.ToImmutableDictionary();
        }
        
        /// <summary>
        /// Removes nested blocks for removal. For example:
        /// for input: [1..10, 2..5] this method return: [1..10], since
        /// second block (2..5) is in first block (1..10).  
        /// </summary>
        /// <param name="blocksForRemoval"></param>
        /// <returns></returns>
        static ImmutableList<Tuple<int, int>> RemoveNested(ImmutableList<Tuple<int, int>> blocksForRemoval)
        {
            List<Tuple<int, int>> res = new List<Tuple<int, int>>();
            int lastIndexForRemoval = -1;
            foreach (Tuple<int, int> blockForRemoval in blocksForRemoval)
            {
                var (from, to) = blockForRemoval;
                if (from > lastIndexForRemoval)
                {
                    res.Add(blockForRemoval);
                    lastIndexForRemoval = to;
                }
            }

            return res.ToImmutableList();
        }
        
        public MethodBodyRemovalResult(string source, ImmutableDictionary<int, string> idxToRemovedMethodBody)
        {
            Source = source;
            IdxToRemovedMethodBody = idxToRemovedMethodBody;
        }
    }
}