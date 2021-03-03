using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.C
{
    public class PrimaryExpressionListener : CBaseListener
    {
        public ClassInfo FileClassInfo;
        readonly string filePath;

        public PrimaryExpressionListener(string filePath)
        {
            this.filePath = filePath;
        }
        
        public override void EnterPrimaryExpression(CParser.PrimaryExpressionContext context)
        {
            
        }
    }
}