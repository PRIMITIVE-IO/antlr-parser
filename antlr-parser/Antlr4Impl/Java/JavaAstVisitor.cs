using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using JetBrains.Annotations;
using PrimitiveCodebaseElements.Primitive;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.Antlr4Impl.Java
{
    public class JavaAstVisitor : JavaParserBaseVisitor<AstNode>
    {
        private readonly string Path;
        private readonly MethodBodyRemovalResult MethodBodyRemovalResult;
        private readonly IndexToLocationConverter IndexToLocationConverter;

        public JavaAstVisitor(MethodBodyRemovalResult methodBodyRemovalResult, string path)
        {
            Path = path;
            MethodBodyRemovalResult = methodBodyRemovalResult;
            IndexToLocationConverter = new IndexToLocationConverter(methodBodyRemovalResult.OriginalSource);
        }

        public override AstNode VisitCompilationUnit(JavaParser.CompilationUnitContext context)
        {
            List<AstNode> nodes = context.children.Select(it => it.Accept(this))
                .ToList();

            AstNode.PackageNode package = nodes.OfType<AstNode.PackageNode>().SingleOrDefault();
            List<AstNode.ClassNode> classes = nodes.OfType<AstNode.ClassNode>().ToList();

            return new AstNode.FileNode(
                path: Path,
                packageNode: package,
                classes: classes,
                fields: new List<AstNode.FieldNode>(),
                methods: new List<AstNode.MethodNode>(),
                header: "", //This header was used for creating fake classes, Java does not have top-level functions so there is no need in fake classes
                namespaces: new List<AstNode.Namespace>(),
                language: SourceCodeLanguage.Java,
                isTest: false,
                codeRange: null //Used for fake classes
            );
        }

        public override AstNode VisitPackageDeclaration(JavaParser.PackageDeclarationContext context)
        {
            return new AstNode.PackageNode(context.qualifiedName().GetText());
        }

        public override AstNode VisitClassDeclaration(JavaParser.ClassDeclarationContext context)
        {
            List<AstNode> astNodes = context.children
                .SelectMany(it => it.Accept(this)?.AsList() ?? new List<AstNode>())
                .ToList();

            List<AstNode.FieldNode> fieldNodes = astNodes.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.MethodNode> methodNodes = astNodes.OfType<AstNode.MethodNode>().ToList();
            List<AstNode.ClassNode> classNodes = astNodes.OfType<AstNode.ClassNode>().ToList();


            //top-level class
            string topLevelClassModifier = TypeDeclarationModifier(context.Parent as JavaParser.TypeDeclarationContext);

            // inner class
            string innerClassModifier = ((context.Parent as JavaParser.MemberDeclarationContext)
                    ?.Parent as JavaParser.ClassBodyDeclarationContext)
                ?.modifier()
                .Select(it => it.GetText())
                .SingleOrDefault(it => it == "private" || it == "protected" || it == "public");

            int startIdx = (NearestPeerEndIdx(context, context.Start.StartIndex) ?? EnclosingClassHeaderEnd(context))
                .GetValueOrDefault(-1) + 1;

            int restoredStartIdx = MethodBodyRemovalResult.RestoreIdx(startIdx);
            int restoredEndIdx = MethodBodyRemovalResult.RestoreIdx(context.classBody().LBRACE().Symbol.StartIndex);

            string header = MethodBodyRemovalResult.ExtractOriginalSubstring(restoredStartIdx, restoredEndIdx).Trim()
                .TrimIndent();

            return new AstNode.ClassNode(
                name: context.IDENTIFIER().GetText(),
                methods: methodNodes,
                fields: fieldNodes,
                innerClasses: classNodes,
                modifier: AccessFlag(topLevelClassModifier ?? innerClassModifier),
                startIdx: restoredStartIdx,
                endIdx: restoredEndIdx,
                codeRange: IndexToLocationConverter.IdxToCodeRange(restoredStartIdx, restoredEndIdx),
                header: header
            );
        }

        static int? NearestPeerEndIdx(RuleContext context, int selfStartIdx)
        {
            switch (context.Parent)
            {
                case JavaParser.CompilationUnitContext c: return null;
                case JavaParser.InterfaceBodyContext c:
                    return c.interfaceBodyDeclaration()
                        .Select(it => it.Stop.StopIndex as int?)
                        .TakeWhile(it => it < selfStartIdx)
                        .Max();
                case JavaParser.ClassBodyContext c:
                    return c.classBodyDeclaration()
                        .Select(it => it.Stop.StopIndex as int?)
                        .TakeWhile(it => it < selfStartIdx)
                        .Max();
                default: return NearestPeerEndIdx(context.Parent, selfStartIdx);
            }
        }

        static int? EnclosingClassHeaderEnd(RuleContext context)
        {
            switch (context.Parent)
            {
                case JavaParser.InterfaceDeclarationContext c: return c.interfaceBody().LBRACE().Symbol.StartIndex;
                case JavaParser.ClassDeclarationContext c: return c.classBody().LBRACE().Symbol.StartIndex;
                case JavaParser.CompilationUnitContext _: return null;
                default: return EnclosingClassHeaderEnd(context.Parent);
            }
        }

        static AccessFlags AccessFlag(string modifier)
        {
            switch (modifier)
            {
                case "private": return AccessFlags.AccPrivate;
                case "protected": return AccessFlags.AccProtected;
                case "public": return AccessFlags.AccPublic;
                default: return AccessFlags.None;
            }
        }


        public override AstNode VisitInterfaceDeclaration(JavaParser.InterfaceDeclarationContext context)
        {
            List<AstNode> astNodes = context.children
                .SelectMany(it => it.Accept(this)?.AsList() ?? new List<AstNode>())
                .ToList();

            List<AstNode.FieldNode> fieldNodes = astNodes.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.MethodNode> methodNodes = astNodes.OfType<AstNode.MethodNode>().ToList();
            List<AstNode.ClassNode> classNodes = astNodes.OfType<AstNode.ClassNode>().ToList();

            string modifier = TypeDeclarationModifier(context.Parent as JavaParser.TypeDeclarationContext);

            int startIdx = (NearestPeerEndIdx(context, context.Start.StartIndex) ?? EnclosingClassHeaderEnd(context))
                .GetValueOrDefault(-1) + 1;
            int endIdx = context.interfaceBody().LBRACE().Symbol.StartIndex;

            int restoredStartIdx = MethodBodyRemovalResult.RestoreIdx(startIdx);
            int restoredEndIdx = MethodBodyRemovalResult.RestoreIdx(endIdx);

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(restoredStartIdx, restoredEndIdx);

            string header = MethodBodyRemovalResult.ExtractOriginalSubstring(restoredStartIdx, restoredEndIdx)
                .Trim()
                .TrimIndent();

            return new AstNode.ClassNode(
                name: context.IDENTIFIER().GetText(),
                methods: methodNodes,
                fields: fieldNodes,
                innerClasses: classNodes,
                modifier: AccessFlag(modifier),
                startIdx: restoredStartIdx,
                endIdx: restoredEndIdx,
                codeRange: codeRange,
                header: header
            );
        }

        public override AstNode VisitInterfaceMethodDeclaration(JavaParser.InterfaceMethodDeclarationContext context)
        {
            string modifier = (context.Parent.Parent as JavaParser.InterfaceBodyDeclarationContext)
                .modifier()
                .Select(it => it.GetText())
                .SingleOrDefault(it => it == "private" || it == "protected" || it == "public");

            string name = context.IDENTIFIER().GetText();

            int startIdx = (NearestPeerEndIdx(context, context.Start.StartIndex) ?? EnclosingClassHeaderEnd(context))
                .GetValueOrDefault(-1) + 1;

            int restoredStartIdx = MethodBodyRemovalResult.RestoreIdx(startIdx);
            int restoredEndIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            string source = MethodBodyRemovalResult.ExtractOriginalSubstring(restoredStartIdx, restoredEndIdx)
                .Trim()
                .TrimIndent();

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(restoredStartIdx, restoredEndIdx);

            return new AstNode.MethodNode(
                name: name,
                accFlag: AccessFlag(modifier),
                sourceCode: source,
                startIdx: restoredStartIdx,
                endIdx: restoredEndIdx,
                codeRange: codeRange
            );
        }

        public override AstNode VisitMethodDeclaration(JavaParser.MethodDeclarationContext context)
        {
            string modifier =
                ClassBodyDeclarationModifier(context.Parent.Parent as JavaParser.ClassBodyDeclarationContext);

            string name = context.IDENTIFIER().GetText();

            int startIdx = (NearestPeerEndIdx(context, context.Start.StartIndex) ?? EnclosingClassHeaderEnd(context))
                .GetValueOrDefault(-1) + 1;

            int restoredStartIdx = MethodBodyRemovalResult.RestoreIdx(startIdx);
            int restoredEndIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            string source = MethodBodyRemovalResult.ExtractOriginalSubstring(restoredStartIdx, restoredEndIdx)
                .Trim()
                .TrimIndent();

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(restoredStartIdx, restoredEndIdx);

            return new AstNode.MethodNode(
                name: name,
                accFlag: AccessFlag(modifier),
                sourceCode: source,
                startIdx: restoredStartIdx,
                endIdx: restoredEndIdx,
                codeRange: codeRange
            );
        }

        public override AstNode VisitFieldDeclaration(JavaParser.FieldDeclarationContext context)
        {
            string modifier =
                ClassBodyDeclarationModifier(context.Parent.Parent as JavaParser.ClassBodyDeclarationContext);

            string name = context.variableDeclarators().variableDeclarator().First().variableDeclaratorId().GetText();

            int startIdx = (NearestPeerEndIdx(context, context.Start.StartIndex) ?? EnclosingClassHeaderEnd(context))
                .GetValueOrDefault(-1) + 1;

            int restoredStartIdx = MethodBodyRemovalResult.RestoreIdx(startIdx);
            int restoredEndIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(restoredStartIdx, restoredEndIdx);

            string sourceCode = MethodBodyRemovalResult.ExtractOriginalSubstring(restoredStartIdx, restoredEndIdx)
                .Trim()
                .TrimIndent();

            return new AstNode.FieldNode(
                name: name,
                accFlag: AccessFlag(modifier),
                sourceCode: sourceCode,
                startIdx: restoredStartIdx,
                endIdx: restoredEndIdx,
                codeRange: codeRange
            );
        }

        [CanBeNull]
        private static string ClassBodyDeclarationModifier([CanBeNull] JavaParser.ClassBodyDeclarationContext context)
        {
            return context
                ?.modifier()
                ?.Select(it => it.GetText())
                .SingleOrDefault(it => it == "private" || it == "protected" || it == "public");
        }

        public override AstNode VisitEnumDeclaration(JavaParser.EnumDeclarationContext context)
        {
            List<AstNode> astNodes = context.children
                .SelectMany(it => it.Accept(this)?.AsList() ?? new List<AstNode>())
                .ToList();

            List<AstNode.FieldNode> fieldNodes = astNodes.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.MethodNode> methodNodes = astNodes.OfType<AstNode.MethodNode>().ToList();
            List<AstNode.ClassNode> classNodes = astNodes.OfType<AstNode.ClassNode>().ToList();

            int startIdx = (NearestPeerEndIdx(context, context.Start.StartIndex) ?? EnclosingClassHeaderEnd(context))
                .GetValueOrDefault(-1) + 1;

            int stopIdx = context.enumConstants().Stop.StopIndex;

            int restoredStartIdx = MethodBodyRemovalResult.RestoreIdx(startIdx);
            int restoredStopIdx = MethodBodyRemovalResult.RestoreIdx(stopIdx);

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(restoredStartIdx, restoredStopIdx);

            string modifier = TypeDeclarationModifier(context.Parent as JavaParser.TypeDeclarationContext) ??
                              ClassBodyDeclarationModifier(
                                  context.Parent.Parent as JavaParser.ClassBodyDeclarationContext);

            string header = MethodBodyRemovalResult.ExtractOriginalSubstring(restoredStartIdx, restoredStopIdx)
                .Trim()
                .TrimIndent();

            return new AstNode.ClassNode(
                name: context.IDENTIFIER().GetText(),
                methods: methodNodes,
                fields: fieldNodes,
                innerClasses: classNodes,
                modifier: AccessFlag(modifier),
                startIdx: restoredStartIdx,
                endIdx: restoredStopIdx,
                codeRange: codeRange,
                header: header
            );
        }

        [CanBeNull]
        private static string TypeDeclarationModifier([CanBeNull] JavaParser.TypeDeclarationContext context)
        {
            return context
                ?.classOrInterfaceModifier()
                ?.Select(it => it.GetText())
                .SingleOrDefault(it => it == "private" || it == "protected" || it == "public");
        }

        static string ParentName(ParserRuleContext context)
        {
            return (context.Parent?.Parent?.Parent?.Parent as JavaParser.EnumDeclarationContext)?.IDENTIFIER()
                   .GetText() ??
                   (context.Parent?.Parent?.Parent?.Parent as JavaParser.ClassDeclarationContext)?.IDENTIFIER()
                   .GetText() ??
                   (context.Parent?.Parent?.Parent?.Parent as JavaParser.InterfaceDeclarationContext)?.IDENTIFIER()
                   .GetText();
        }

        public override AstNode VisitConstructorDeclaration(JavaParser.ConstructorDeclarationContext context)
        {
            string modifier = (context.Parent.Parent as JavaParser.ClassBodyDeclarationContext)
                .modifier()
                .Select(it => it.GetText())
                .SingleOrDefault(it => it == "private" || it == "protected" || it == "public");

            string name = ParentName(context);

            int startIdx = (NearestPeerEndIdx(context, context.Start.StartIndex) ?? EnclosingClassHeaderEnd(context))
                .GetValueOrDefault(-1) + 1;

            int restoredStartIdx = MethodBodyRemovalResult.RestoreIdx(startIdx);
            int restoredEndIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            string source = MethodBodyRemovalResult.ExtractOriginalSubstring(restoredStartIdx, restoredEndIdx)
                .Trim()
                .TrimIndent();

            CodeRange codeRange = IndexToLocationConverter.IdxToCodeRange(restoredStartIdx, restoredEndIdx);

            return new AstNode.MethodNode(
                name: name,
                accFlag: AccessFlag(modifier),
                sourceCode: source,
                startIdx: restoredStartIdx,
                endIdx: restoredEndIdx,
                codeRange: codeRange
            );
        }

        protected override AstNode AggregateResult(AstNode aggregate, AstNode nextResult)
        {
            return AstNode.NodeList.Combine(aggregate, nextResult);
        }
    }
}