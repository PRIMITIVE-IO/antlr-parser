using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.JavaScript
{
    public class VariableStatementListener : JavaScriptParserBaseListener
    {
        readonly ClassInfo outerClassInfo;

        public VariableStatementListener(ClassInfo outerClassInfo)
        {
            this.outerClassInfo = outerClassInfo;
        }

        public override void EnterVariableStatement(JavaScriptParser.VariableStatementContext context)
        {
            VariableDeclarationListListener variableDeclarationListListener =
                new VariableDeclarationListListener(outerClassInfo);
            context.variableDeclarationList().EnterRule(variableDeclarationListListener);
        }

        class VariableDeclarationListListener : JavaScriptParserBaseListener
        {
            readonly ClassInfo outerClassInfo;

            public VariableDeclarationListListener(ClassInfo outerClassInfo)
            {
                this.outerClassInfo = outerClassInfo;
            }

            public override void EnterVariableDeclarationList(JavaScriptParser.VariableDeclarationListContext context)
            {
                foreach (JavaScriptParser.VariableDeclarationContext variableDeclarationContext in context
                    .variableDeclaration())
                {
                    VariableDeclarationListener variableDeclarationListener =
                        new VariableDeclarationListener(outerClassInfo);
                    variableDeclarationContext.EnterRule(variableDeclarationListener);
                }
            }

            class VariableDeclarationListener : JavaScriptParserBaseListener
            {
                readonly ClassInfo outerClassInfo;

                public VariableDeclarationListener(ClassInfo outerClassInfo)
                {
                    this.outerClassInfo = outerClassInfo;
                }

                public override void EnterVariableDeclaration(JavaScriptParser.VariableDeclarationContext context)
                {
                    if (context.singleExpression() == null) return;

                    // add single line expressions to the outer fileClass header source
                    string newSourceText =
                        outerClassInfo.SourceCode.Text + "\n" + context.singleExpression().GetFullText();
                    outerClassInfo.SourceCode = new SourceCodeSnippet(newSourceText, SourceCodeLanguage.JavaScript);
                }
            }
        }
    }
}