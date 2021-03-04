using System.Collections.Generic;
using System.IO;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.JavaScript
{
    public class ProgramListener : JavaScriptParserBaseListener
    {
        public ClassInfo FileClassInfo;
        readonly string filePath;

        public ProgramListener(string filePath)
        {
            this.filePath = filePath;
        }

        public override void EnterProgram(JavaScriptParser.ProgramContext context)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            ClassName fileClassName = new ClassName(new FileName(filePath), new PackageName(), fileName);
            FileClassInfo = new ClassInfo(
                fileClassName,
                new List<MethodInfo>(),
                new List<FieldInfo>(),
                AccessFlags.AccPublic,
                new List<ClassInfo>(),
                new SourceCodeSnippet("", SourceCodeLanguage.JavaScript),
                false);

            SourceElementsListener sourceElementsListener = new SourceElementsListener(FileClassInfo);
            context.sourceElements().EnterRule(sourceElementsListener);
        }

        class SourceElementsListener : JavaScriptParserBaseListener
        {
            readonly ClassInfo fileClassInfo;

            public SourceElementsListener(ClassInfo fileClassInfo)
            {
                this.fileClassInfo = fileClassInfo;
            }

            public override void EnterSourceElements(JavaScriptParser.SourceElementsContext context)
            {
                foreach (JavaScriptParser.SourceElementContext sourceElementContext in context.sourceElement())
                {
                    SourceElementListener sourceElementListener = new SourceElementListener(fileClassInfo);
                    sourceElementContext.EnterRule(sourceElementListener);
                }
            }

            class SourceElementListener : JavaScriptParserBaseListener
            {
                readonly ClassInfo fileClassInfo;

                public SourceElementListener(ClassInfo fileClassInfo)
                {
                    this.fileClassInfo = fileClassInfo;
                }

                public override void EnterSourceElement(JavaScriptParser.SourceElementContext context)
                {
                    StatementListener statementListener = new StatementListener(fileClassInfo);
                    context.statement().EnterRule(statementListener);
                }
            }
        }
    }
}