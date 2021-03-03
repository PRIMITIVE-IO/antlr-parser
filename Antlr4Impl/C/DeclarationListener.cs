using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.C
{
    public class ExternalDeclarationListener : CBaseListener
    {
        readonly ClassInfo fileClassInfo;

        public ExternalDeclarationListener(ClassInfo fileClassInfo)
        {
            this.fileClassInfo = fileClassInfo;
        }

        public override void EnterExternalDeclaration(CParser.ExternalDeclarationContext context)
        {
            if (context.functionDefinition() != null)
            {
                AntlrParseC.FunctionDefinitionListener functionDefinitionListener =
                    new AntlrParseC.FunctionDefinitionListener(fileClassInfo);
                context.functionDefinition().EnterRule(functionDefinitionListener);
            }
        }
    }

    public class DeclarationSpecifiersListener : CBaseListener
    {
        public TypeName ReturnType;

        public override void EnterDeclarationSpecifiers(CParser.DeclarationSpecifiersContext context)
        {
            foreach (CParser.DeclarationSpecifierContext declarationSpecifierContext in
                context.declarationSpecifier())
            {
                ReturnType = TypeName.For(declarationSpecifierContext.GetFullText());
            }
        }
    }

    public class DeclaratorListener : CBaseListener
    {
        public string DeclaratorName;

        public override void EnterDeclarator(CParser.DeclaratorContext context)
        {
            DirectDeclaratorListener directDeclaratorListener = new DirectDeclaratorListener();
            context.directDeclarator().EnterRule(directDeclaratorListener);
            DeclaratorName = directDeclaratorListener.DirectDeclarator;
        }
    }

    public class DirectDeclaratorListener : CBaseListener
    {
        public string DirectDeclarator = "";

        public override void EnterDirectDeclarator(CParser.DirectDeclaratorContext context)
        {
            if (context.directDeclarator() != null)
            {
                DirectDeclaratorListener directDeclaratorListener = new DirectDeclaratorListener();
                context.directDeclarator().EnterRule(directDeclaratorListener);
                DirectDeclarator = directDeclaratorListener.DirectDeclarator;
            }
            else
            {
                DirectDeclarator = context.GetFullText();
            }
        }
    }
}