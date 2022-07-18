using System;
using System.Collections.Generic;
using Antlr4.Runtime;

namespace antlr_parser.Antlr4Impl.CSharp
{
    /// <summary>
    /// copied from here: https://gist.github.com/KvanTTT/d95579de257531a3cc15
    /// based on this readme: https://github.com/antlr/grammars-v4/tree/master/csharp
    /// </summary>
    public class CSharpPreprocessorAwareParser
    {
        public static ListTokenSource TokenSource(Lexer preprocessorLexer)
        {
            List<IToken> codeTokens = new List<IToken>();
            List<IToken> commentTokens = new List<IToken>();

            // Collect all tokens with lexer (CSharpLexer.g4).
            IList<IToken> tokens = preprocessorLexer.GetAllTokens();
            List<IToken> directiveTokens = new List<IToken>();
            ListTokenSource directiveTokenSource = new ListTokenSource(directiveTokens);
            CommonTokenStream directiveTokenStream = new CommonTokenStream(directiveTokenSource, CSharpLexer.DIRECTIVE);
            CSharpPreprocessorParser preprocessorParser = new CSharpPreprocessorParser(directiveTokenStream);

            int index = 0;
            bool compiliedTokens = true;
            while (index < tokens.Count)
            {
                var token = tokens[index];
                if (token.Type == CSharpLexer.SHARP)
                {
                    directiveTokens.Clear();
                    int directiveTokenIndex = index + 1;
                    // Collect all preprocessor directive tokens.
                    while (directiveTokenIndex < tokens.Count &&
                           tokens[directiveTokenIndex].Type != CSharpLexer.Eof &&
                           tokens[directiveTokenIndex].Type != CSharpLexer.DIRECTIVE_NEW_LINE &&
                           tokens[directiveTokenIndex].Type != CSharpLexer.SHARP)
                    {
                        if (tokens[directiveTokenIndex].Channel == CSharpLexer.COMMENTS_CHANNEL)
                        {
                            commentTokens.Add(tokens[directiveTokenIndex]);
                        }
                        else if (tokens[directiveTokenIndex].Channel != Lexer.Hidden)
                        {
                            directiveTokens.Add(tokens[directiveTokenIndex]);
                        }

                        directiveTokenIndex++;
                    }

                    directiveTokenSource = new ListTokenSource(directiveTokens);
                    directiveTokenStream = new CommonTokenStream(directiveTokenSource, CSharpLexer.DIRECTIVE);
                    preprocessorParser.TokenStream = directiveTokenStream;
                    preprocessorParser.Reset();
                    // Parse condition in preprocessor directive (based on CSharpPreprocessorParser.g4 grammar).
                    CSharpPreprocessorParser.Preprocessor_directiveContext directive =
                        preprocessorParser.preprocessor_directive();
                    // if true than next code is valid and not ignored.
                    compiliedTokens = directive.value;
                    String directiveStr = tokens[index + 1].Text.Trim();
                    if ("line" == directiveStr || "error" == directiveStr || "warning" == directiveStr ||
                        "define" == directiveStr || "endregion" == directiveStr || "endif" == directiveStr ||
                        "pragma" == directiveStr)
                    {
                        compiliedTokens = true;
                    }

                    String conditionalSymbol = null;
                    if ("define" == tokens[index + 1].Text)
                    {
                        // add to the conditional symbols 
                        conditionalSymbol = tokens[index + 2].Text;
                        preprocessorParser.ConditionalSymbols.Add(conditionalSymbol);
                    }

                    if ("undef" == tokens[index + 1].Text)
                    {
                        conditionalSymbol = tokens[index + 2].Text;
                        preprocessorParser.ConditionalSymbols.Remove(conditionalSymbol);
                    }

                    index = directiveTokenIndex - 1;
                }
                else if (token.Channel == CSharpLexer.COMMENTS_CHANNEL)
                {
                    commentTokens.Add(token); // Colect comment tokens (if required).
                }
                else if (token.Channel != Lexer.Hidden && token.Type != CSharpLexer.DIRECTIVE_NEW_LINE &&
                         compiliedTokens)
                {
                    codeTokens.Add(token); // Collect code tokens.
                }

                index++;
            }

            // At second stage tokens parsed in usual way.
            return new ListTokenSource(tokens);
        }
    }
}