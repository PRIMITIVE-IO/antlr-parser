using System.Collections.Generic;
using System.IO;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.TypeScript
{
    public class ProgramListener : TypeScriptParserBaseListener
    {
        public ClassInfo FileClassInfo;
        readonly string filePath;

        public ProgramListener(string filePath)
        {
            this.filePath = filePath;
        }
        
        public override void EnterProgram(TypeScriptParser.ProgramContext context)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            
            ClassName fileClassName = new ClassName(new FileName(filePath), new PackageName(), fileName);
            FileClassInfo = new ClassInfo(
                fileClassName, 
                new List<MethodInfo>(), 
                new List<FieldInfo>(),
                AccessFlags.AccPublic,
                new List<ClassInfo>(), 
                new SourceCodeSnippet(context.GetFullText(), SourceCodeLanguage.TypeScript),
                false);

            if (context.sourceElements() != null)
            {
                SourceElementsListener sourceElementsListener = new SourceElementsListener(FileClassInfo);
                context.sourceElements().EnterRule(sourceElementsListener);
            }
        }
    }

    public class SourceElementsListener : TypeScriptParserBaseListener
    {
        readonly ClassInfo fileClassInfo;
        public SourceElementsListener(ClassInfo fileClassInfo)
        {
            this.fileClassInfo = fileClassInfo;
        }
        
        public override void EnterSourceElements(TypeScriptParser.SourceElementsContext context)
        {
            foreach (TypeScriptParser.SourceElementContext sourceElementContext in context.sourceElement())
            {
                SourceElementListener sourceElementListener = new SourceElementListener(fileClassInfo);
                sourceElementContext.EnterRule(sourceElementListener);
            }
        }
    }

    public class SourceElementListener : TypeScriptParserBaseListener
    {
        readonly ClassInfo fileClassInfo;
        public SourceElementListener(ClassInfo fileClassInfo)
        {
            this.fileClassInfo = fileClassInfo;
        }

        public override void EnterSourceElement(TypeScriptParser.SourceElementContext context)
        {
            if (context.statement() != null)
            {
                StatementListener statementListener = new StatementListener(fileClassInfo);
                context.statement().EnterRule(statementListener);
            }
        }
    }
}