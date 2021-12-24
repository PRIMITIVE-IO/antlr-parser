using System;
using System.Collections.Generic;
using System.Linq;

namespace antlr_parser.Antlr4Impl
{
    public class MethodBodyRemovalResult
    {
        public readonly string ShortenedSource;
        public readonly string OriginalSource;
        readonly List<Tuple<int, int>> BlocksToRemove;

        /// <summary>
        /// Index is a position of last symbol of a function declaration header
        /// It is 'Y' for: fun f(x:X):Y  { 
        /// and ')' for: fun f(x:X){ 
        /// </summary>
        public readonly Dictionary<int, string> IdxToRemovedMethodBody;

        public static MethodBodyRemovalResult From(string source, List<Tuple<int, int>> blocksToRemove)
        {
            List<Tuple<int, int>> topLevelBlocksToRemove = RemoveNested(blocksToRemove);
            Dictionary<int, string> idxToRemovedMethodBody =
                ComputeIdxToRemovedMethodBody(topLevelBlocksToRemove, source);
            string cleanedText = RemoveBlocks(source, topLevelBlocksToRemove);
            return new MethodBodyRemovalResult(cleanedText, source, idxToRemovedMethodBody, topLevelBlocksToRemove);
        }

        static string RemoveBlocks(string source, List<Tuple<int, int>> removeFromTo)
        {
            return removeFromTo.AsEnumerable()
                // since the list contains char indices, in case if we traverse in original order - each removed block
                // will affect the next one.
                // (Assume you have to remove 1-st and 5-th elements. After removing the first one, you have to remove 4-th rather than 5-th, because it was shifted by removal.)
                // Traversing them in reverse order allows us to avoid this problem.
                // (Using previous example, after removing 5-th element you have to remove 1-st one. No shifts anymore.) 
                .Reverse()
                //for each block it removes a substring (tuple 'fromTo') from a source text
                .Aggregate(source,
                    (acc, fromTo) => acc.Remove(fromTo.Item1, fromTo.Item2 - fromTo.Item1 + 1));
        }

        static Dictionary<int, string> ComputeIdxToRemovedMethodBody(List<Tuple<int, int>> blocksToRemove, string text)
        {
            //This map is used to restore removed source code
            Dictionary<int, string> idxToRemovedMethodBody = new Dictionary<int, string>();

            int removedLength = 0;
            foreach (Tuple<int, int> fromTo in blocksToRemove)
            {
                int lengthToRemove = fromTo.Item2 - fromTo.Item1 + 1;
                // 'fromTo.Item1' is an index in original file, where removed source code used to be. Since this index
                // is shifted (because of previous deletions in the same file) it should be corrected for
                // 'removedLength`.
                idxToRemovedMethodBody[fromTo.Item1 - removedLength - 1] = text.Substring(fromTo.Item1, lengthToRemove);
                removedLength += lengthToRemove;
            }

            return idxToRemovedMethodBody;
        }

        /// <summary>
        /// Removes nested blocks for removal. For example:
        /// for input: [1..10, 2..5] this method return: [1..10], since
        /// second block (2..5) is in first block (1..10).  
        /// </summary>
        /// <param name="blocksForRemoval"></param>
        /// <returns></returns>
        static List<Tuple<int, int>> RemoveNested(List<Tuple<int, int>> blocksForRemoval)
        {
            List<Tuple<int, int>> res = new List<Tuple<int, int>>();
            int lastIndexForRemoval = -1;
            foreach (Tuple<int, int> blockForRemoval in blocksForRemoval)
            {
                Tuple<int, int> fromTo = blockForRemoval;
                if (fromTo.Item1 > lastIndexForRemoval)
                {
                    res.Add(blockForRemoval);
                    lastIndexForRemoval = fromTo.Item2;
                }
            }

            return res.ToList();
        }

        public MethodBodyRemovalResult(
            string shortenedSource,
            string originalSource,
            Dictionary<int, string> idxToRemovedMethodBody,
            List<Tuple<int, int>> blocksToRemove
        )
        {
            ShortenedSource = shortenedSource;
            IdxToRemovedMethodBody = idxToRemovedMethodBody;
            BlocksToRemove = blocksToRemove;
            OriginalSource = originalSource;
        }

        public string ExtractOriginalSubstring(int from, int to)
        {
            return OriginalSource.Substring(from, to - from + 1);
        }

        /// <summary>
        /// Restores original indices based on indices from "shortened" (without method bodies) source code 
        /// </summary>
        /// <param name="idx">index in "shortened" source</param>
        /// <returns>index from an original source code, including removed blocks</returns>
        public int RestoreIdx(int idx)
        {
            int acc = idx;
            foreach (Tuple<int, int> tuple in BlocksToRemove)
            {
                if (acc < tuple.Item1) break;

                acc += tuple.Item2 - tuple.Item1 + 1;
            }

            return acc;
        }
    }
}