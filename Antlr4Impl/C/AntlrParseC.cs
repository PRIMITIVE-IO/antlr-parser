using System;
using System.Collections.Generic;
using System.IO;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.C
{
    public static class AntlrParseC
    {
        public static IEnumerable<ClassInfo> OuterClassInfosFromJavaSource(string source, string filePath)
        {
            try
            {
                char[] codeArray = source.ToCharArray();
                AntlrInputStream inputStream = new AntlrInputStream(codeArray, codeArray.Length);

                CLexer lexer = new CLexer(inputStream);
                CommonTokenStream commonTokenStream = new CommonTokenStream(lexer);
                CParser parser = new CParser(commonTokenStream);

                parser.RemoveErrorListeners();
                parser.AddErrorListener(new ErrorListener()); // add ours

                // a primary expression is the highest level container -> start there
                CompilationUnitListener compilationUnitListener = new CompilationUnitListener(filePath);
                parser.compilationUnit().EnterRule(compilationUnitListener);

                return new List<ClassInfo> {compilationUnitListener.FileClassInfo};
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return new List<ClassInfo>();
        }

        class CompilationUnitListener : CBaseListener
        {
            public ClassInfo FileClassInfo;
            readonly string filePath;

            public CompilationUnitListener(string filePath)
            {
                this.filePath = filePath;
            }

            public override void EnterCompilationUnit(CParser.CompilationUnitContext context)
            {
                ClassName fileClassName = new ClassName(
                    new FileName(filePath),
                    new PackageName(""),
                    Path.GetFileNameWithoutExtension(filePath));

                FileClassInfo = new ClassInfo(
                    fileClassName,
                    new List<MethodInfo>(),
                    new List<FieldInfo>(),
                    AccessFlags.AccPublic,
                    new List<ClassInfo>(),
                    new SourceCodeSnippet("", SourceCodeLanguage.Kotlin),
                    false);

                TranslationUnitListener translationUnitListener = new TranslationUnitListener(FileClassInfo);
                context.translationUnit().EnterRule(translationUnitListener);
            }
        }

        class TranslationUnitListener : CBaseListener
        {
            readonly ClassInfo fileClassInfo;

            public TranslationUnitListener(ClassInfo fileClassInfo)
            {
                this.fileClassInfo = fileClassInfo;
            }

            public override void EnterTranslationUnit(CParser.TranslationUnitContext context)
            {
                ExternalDeclarationListener externalDeclarationListener =
                    new ExternalDeclarationListener(fileClassInfo);
                context.externalDeclaration().EnterRule(externalDeclarationListener);

                if (context.translationUnit() != null)
                {
                    TranslationUnitListener translationUnitListener = new TranslationUnitListener(fileClassInfo);
                    context.translationUnit().EnterRule(translationUnitListener);
                }
            }
        }

        public class FunctionDefinitionListener : CBaseListener
        {
            readonly ClassInfo classInfo;

            public FunctionDefinitionListener(ClassInfo classInfo)
            {
                this.classInfo = classInfo;
            }

            public override void EnterFunctionDefinition(CParser.FunctionDefinitionContext context)
            {
                AccessFlags accessFlags = AccessFlags.AccPublic;

                DeclarationSpecifiersListener declarationSpecifiersListener = new DeclarationSpecifiersListener();
                context.declarationSpecifiers().EnterRule(declarationSpecifiersListener);
                
                TypeName returnType = declarationSpecifiersListener.ReturnType;

                DeclaratorListener declaratorListener = new DeclaratorListener();
                context.declarator().EnterRule(declaratorListener);
                string methodNameString = declaratorListener.DeclaratorName;

                // TODO
                List<Argument> arguments = new List<Argument>();

                MethodName methodName = new MethodName(
                    classInfo.className,
                    methodNameString,
                    returnType.Signature,
                    arguments);

                MethodInfo methodInfo = new MethodInfo(
                    methodName,
                    accessFlags,
                    classInfo.className,
                    arguments,
                    returnType,
                    new SourceCodeSnippet(context.GetFullText(), SourceCodeLanguage.C));

                classInfo.Children.Add(methodInfo);
            }
        }
    }
}