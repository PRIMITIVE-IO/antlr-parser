using System.Collections.Generic;

namespace antlr_parser.Antlr4Impl.Python
{
    public static class LastNonWhitespaceIndexer
    {
        public static Dictionary<int, int> IdxToLastNonWhiteSpace(string source)
        {

            Dictionary<int, int> idx = new Dictionary<int, int>();
            int? lastNonWhiteSpace = null;
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i]; 
                
                if (char.IsWhiteSpace(c))
                {
                    if (lastNonWhiteSpace != null)
                    {
                        idx.Add(i, lastNonWhiteSpace.Value);
                    }
                }
                else
                {
                    lastNonWhiteSpace = i;
                }
            }

            return idx;
        }
    }
}