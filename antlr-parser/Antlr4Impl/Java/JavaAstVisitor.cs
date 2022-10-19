using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.Antlr4Impl.Java
{
    public class JavaAstVisitor : JavaParserBaseVisitor<AstNode>
    {
        readonly string Path;
        readonly MethodBodyRemovalResult MethodBodyRemovalResult;
        readonly IndexToLocationConverter IndexToLocationConverter;
        readonly CodeRangeCalculator CodeRangeCalculator;

        public JavaAstVisitor(
            MethodBodyRemovalResult methodBodyRemovalResult,
            string path,
            CodeRangeCalculator codeRangeCalculator
        )
        {
            Path = path;
            MethodBodyRemovalResult = methodBodyRemovalResult;
            IndexToLocationConverter = new IndexToLocationConverter(methodBodyRemovalResult.OriginalSource);
            CodeRangeCalculator = codeRangeCalculator;
        }

        #region VISITORS

        public override AstNode VisitCompilationUnit(JavaParser.CompilationUnitContext context)
        {
            List<AstNode> nodes = context.children.Select(it => it.Accept(this))
                .ToList();

            AstNode.PackageNode? package = nodes.OfType<AstNode.PackageNode>().SingleOrDefault();
            List<AstNode.ClassNode> classes = nodes.OfType<AstNode.ClassNode>().ToList();

            CodeRange codeRange = CodeRangeCalculator.Trim(
                new CodeRange(
                    new CodeLocation(1, 1),
                    CodeRangeCalculator.EndPosition()
                )
            );

            return new AstNode.FileNode(
                path: Path,
                packageNode: package,
                classes: classes,
                fields: new List<AstNode.FieldNode>(),
                methods: new List<AstNode.MethodNode>(),
                namespaces: new List<AstNode.Namespace>(),
                language: SourceCodeLanguage.Java,
                isTest: false,
                codeRange: codeRange
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

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(restoredStartIdx, restoredEndIdx)
            );

            return new AstNode.ClassNode(
                name: context.IDENTIFIER().GetText(),
                methods: methodNodes,
                fields: fieldNodes,
                innerClasses: classNodes,
                modifier: AccessFlag(topLevelClassModifier ?? innerClassModifier),
                startIdx: restoredStartIdx,
                codeRange: codeRange
            );
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

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(restoredStartIdx, restoredEndIdx)
            );

            return new AstNode.ClassNode(
                name: context.IDENTIFIER().GetText(),
                methods: methodNodes,
                fields: fieldNodes,
                innerClasses: classNodes,
                modifier: AccessFlag(modifier),
                startIdx: restoredStartIdx,
                codeRange: codeRange
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

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(restoredStartIdx, restoredEndIdx)
            );

            List<AstNode.ArgumentNode> arguments = context.formalParameters()?.formalParameterList()?.formalParameter()
                .Select(parameter => parameter.Accept(this) as AstNode.ArgumentNode)
                .ToList() ?? new List<AstNode.ArgumentNode>();

            string returnType = context.typeTypeOrVoid()?.typeType()?.GetText() ?? "void";

            return new AstNode.MethodNode(
                name: name,
                accFlag: AccessFlag(modifier),
                startIdx: restoredStartIdx,
                codeRange: codeRange,
                arguments: arguments,
                returnType: returnType
            );
        }

        public override AstNode VisitFormalParameter(JavaParser.FormalParameterContext context)
        {
            return new AstNode.ArgumentNode(
                name: context.variableDeclaratorId().GetText(),
                type: context.typeType().GetText());
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

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(restoredStartIdx, restoredEndIdx)
            );

            List<AstNode.ArgumentNode> arguments = context.formalParameters()?.formalParameterList()?.formalParameter()
                .Select(param => param.Accept(this) as AstNode.ArgumentNode)
                .ToList() ?? new List<AstNode.ArgumentNode>();
            
            string returnType = context.typeTypeOrVoid()?.typeType()?.GetText() ?? "void";

            return new AstNode.MethodNode(
                name: name,
                accFlag: AccessFlag(modifier),
                startIdx: restoredStartIdx,
                codeRange: codeRange,
                arguments: arguments,
                returnType: returnType
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

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(restoredStartIdx, restoredEndIdx)
            );

            return new AstNode.FieldNode(
                name: name,
                accFlag: AccessFlag(modifier),
                startIdx: restoredStartIdx,
                codeRange: codeRange
            );
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

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(restoredStartIdx, restoredStopIdx)
            );

            string modifier = TypeDeclarationModifier(context.Parent as JavaParser.TypeDeclarationContext) ??
                              ClassBodyDeclarationModifier(
                                  context.Parent.Parent as JavaParser.ClassBodyDeclarationContext);

            return new AstNode.ClassNode(
                name: context.IDENTIFIER().GetText(),
                methods: methodNodes,
                fields: fieldNodes,
                innerClasses: classNodes,
                modifier: AccessFlag(modifier),
                startIdx: restoredStartIdx,
                codeRange: codeRange
            );
        }

        public override AstNode VisitConstructorDeclaration(JavaParser.ConstructorDeclarationContext context)
        {
            string modifier = (context.Parent.Parent as JavaParser.ClassBodyDeclarationContext)
                .modifier()
                .Select(it => it.GetText())
                .SingleOrDefault(it => it == "private" || it == "protected" || it == "public");

            string? name = ParentName(context);

            int startIdx = (NearestPeerEndIdx(context, context.Start.StartIndex) ?? EnclosingClassHeaderEnd(context))
                .GetValueOrDefault(-1) + 1;

            int restoredStartIdx = MethodBodyRemovalResult.RestoreIdx(startIdx);
            int restoredEndIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(restoredStartIdx, restoredEndIdx)
            );

            List<AstNode.ArgumentNode> arguments = context.formalParameters()?.formalParameterList()?.formalParameter()
                .Select(parameter => parameter.Accept(this) as AstNode.ArgumentNode)
                .ToList() ?? new List<AstNode.ArgumentNode>();

            return new AstNode.MethodNode(
                name: name,
                accFlag: AccessFlag(modifier),
                startIdx: restoredStartIdx,
                codeRange: codeRange,
                arguments: arguments,
                returnType: "void"
            );
        }
        
        #endregion

        #region UTIL

        static string? ClassBodyDeclarationModifier(JavaParser.ClassBodyDeclarationContext? context)
        {
            return context
                ?.modifier()
                ?.Select(it => it.GetText())
                .SingleOrDefault(it => it == "private" || it == "protected" || it == "public");
        }

        static int? NearestPeerEndIdx(RuleContext context, int selfStartIdx)
        {
            return context.Parent switch
            {
                JavaParser.CompilationUnitContext c => null,
                JavaParser.InterfaceBodyContext c => c.interfaceBodyDeclaration()
                    .Select(it => it.Stop.StopIndex as int?)
                    .TakeWhile(it => it < selfStartIdx)
                    .Max(),
                JavaParser.ClassBodyContext c => c.classBodyDeclaration()
                    .Select(it => it.Stop.StopIndex as int?)
                    .TakeWhile(it => it < selfStartIdx)
                    .Max(),
                _ => NearestPeerEndIdx(context.Parent, selfStartIdx)
            };
        }

        static int? EnclosingClassHeaderEnd(RuleContext context)
        {
            return context.Parent switch
            {
                JavaParser.InterfaceDeclarationContext c => c.interfaceBody().LBRACE().Symbol.StartIndex,
                JavaParser.ClassDeclarationContext c => c.classBody().LBRACE().Symbol.StartIndex,
                JavaParser.CompilationUnitContext _ => null,
                _ => EnclosingClassHeaderEnd(context.Parent)
            };
        }

        static AccessFlags AccessFlag(string modifier)
        {
            return modifier switch
            {
                "private" => AccessFlags.AccPrivate,
                "protected" => AccessFlags.AccProtected,
                "public" => AccessFlags.AccPublic,
                _ => AccessFlags.None
            };
        }

        static string? TypeDeclarationModifier(JavaParser.TypeDeclarationContext? context)
        {
            return context
                ?.classOrInterfaceModifier()
                ?.Select(it => it.GetText())
                .SingleOrDefault(it => it == "private" || it == "protected" || it == "public");
        }

        static string? ParentName(ParserRuleContext context)
        {
            return (context.Parent?.Parent?.Parent?.Parent as JavaParser.EnumDeclarationContext)?.IDENTIFIER()
                   .GetText() ??
                   (context.Parent?.Parent?.Parent?.Parent as JavaParser.ClassDeclarationContext)?.IDENTIFIER()
                   .GetText() ??
                   (context.Parent?.Parent?.Parent?.Parent as JavaParser.InterfaceDeclarationContext)?.IDENTIFIER()
                   .GetText();
        }

        protected override AstNode AggregateResult(AstNode aggregate, AstNode nextResult)
        {
            return AstNode.NodeList.Combine(aggregate, nextResult);
        }

        #endregion
    }
}