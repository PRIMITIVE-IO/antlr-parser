using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            if (!removeFromTo.Any()) return source;

            StringBuilder sb = new StringBuilder();

            sb.Append(source[..removeFromTo.First().Item1]);
            
            for (int i = 0; i < removeFromTo.Count - 1; i++)
            {
                sb.Append(source[(removeFromTo[i].Item2 + 1)..(removeFromTo[i + 1].Item1)]);
            }

            sb.Append(source[(removeFromTo.Last().Item2 + 1)..]);

            return sb.ToString();
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
                (int fromIndex, int toIndex) = blockForRemoval;

                if (fromIndex <= lastIndexForRemoval) continue;

                res.Add(blockForRemoval);
                lastIndexForRemoval = toIndex;
            }

            return res.ToList();
        }

        private MethodBodyRemovalResult(
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
            foreach ((int item1, int item2) in BlocksToRemove)
            {
                if (acc < item1) break;

                acc += item2 - item1 + 1;
            }

            return acc;
        }
    }
}