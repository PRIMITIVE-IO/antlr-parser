using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.TypeScript
{
    public class VariableStatementListener : TypeScriptParserBaseListener
    {
        readonly ClassInfo outerClassInfo;

        public VariableStatementListener(ClassInfo outerClassInfo)
        {
            this.outerClassInfo = outerClassInfo;
        }

        public override void EnterVariableStatement(TypeScriptParser.VariableStatementContext context)
        {
            VariableDeclarationListListener variableDeclarationListListener =
                new VariableDeclarationListListener(outerClassInfo);
            context.variableDeclarationList().EnterRule(variableDeclarationListListener);
        }
    }

    public class VariableDeclarationListListener : TypeScriptParserBaseListener
    {
        readonly ClassInfo outerClassInfo;

        public VariableDeclarationListListener(ClassInfo outerClassInfo)
        {
            this.outerClassInfo = outerClassInfo;
        }

        public override void EnterVariableDeclarationList(TypeScriptParser.VariableDeclarationListContext context)
        {
            foreach (TypeScriptParser.VariableDeclarationContext variableDeclarationContext in context
                .variableDeclaration())
            {
                VariableDeclarationListener variableDeclarationListener =
                    new VariableDeclarationListener(outerClassInfo);
                variableDeclarationContext.EnterRule(variableDeclarationListener);
            }
        }
    }

    public class VariableDeclarationListener : TypeScriptParserBaseListener
    {
        readonly ClassInfo outerClassInfo;

        public VariableDeclarationListener(ClassInfo outerClassInfo)
        {
            this.outerClassInfo = outerClassInfo;
        }

        public override void EnterVariableDeclaration(TypeScriptParser.VariableDeclarationContext context)
        {
            if (context.singleExpression() == null) return;

            // add single line expressions to the outer fileClass header source
            string newSourceText = outerClassInfo.SourceCode.Text + "\n" + context.singleExpression();
            outerClassInfo.SourceCode = new SourceCodeSnippet(newSourceText, SourceCodeLanguage.TypeScript);
        }
    }
}