using System;
using System.Collections.Generic;
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
        public readonly Dictionary<int, string> IdxToRemovedMethodBody;

        public static MethodBodyRemovalResult From(string source, List<Tuple<int, int>> blocksToRemove)
        {
            List<Tuple<int, int>> topLevelBlocksToRemove = RemoveNested(blocksToRemove);
            Dictionary<int,string> idxToRemovedMethodBody = ComputeIdxToRemovedMethodBody(topLevelBlocksToRemove, source);
            string cleanedText = RemoveBlocks(source, topLevelBlocksToRemove);
            return new MethodBodyRemovalResult(cleanedText, idxToRemovedMethodBody);
        } 
        static string RemoveBlocks(string source, List<Tuple<int, int>> removeFromTo)
        {
            return removeFromTo.AsEnumerable() 
                .Reverse()
                .Aggregate(source,
                    (acc, fromTo) => acc.Remove(fromTo.Item1, fromTo.Item2 - fromTo.Item1 + 1));
        }

        static Dictionary<int, string> ComputeIdxToRemovedMethodBody(List<Tuple<int, int>> blocksToRemove, string text)
        {
            Dictionary<int, string> idxToRemovedMethodBody = new Dictionary<int, string>();

            int removedLength = 0;
            foreach (Tuple<int, int> fromTo in blocksToRemove)
            {
                int lengthToRemove = fromTo.Item2 - fromTo.Item1 + 1;
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
        
        public MethodBodyRemovalResult(string source, Dictionary<int, string> idxToRemovedMethodBody)
        {
            Source = source;
            IdxToRemovedMethodBody = idxToRemovedMethodBody;
        }
    }
}