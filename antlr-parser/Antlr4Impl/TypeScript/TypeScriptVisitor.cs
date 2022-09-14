using System;
using System.Collections.Generic;
using System.Linq;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.Antlr4Impl.TypeScript
{
    public class TypeScriptVisitor : TypeScriptParserBaseVisitor<AstNode>
    {
        readonly string Path;
        readonly MethodBodyRemovalResult MethodBodyRemovalResult;
        readonly IndexToLocationConverter IndexToLocationConverter;
        readonly CodeRangeCalculator CodeRangeCalculator;

        public TypeScriptVisitor(
            string path,
            MethodBodyRemovalResult methodBodyRemovalResult,
            CodeRangeCalculator codeRangeCalculator
        )
        {
            Path = path;
            MethodBodyRemovalResult = methodBodyRemovalResult;
            IndexToLocationConverter = new IndexToLocationConverter(methodBodyRemovalResult.OriginalSource);
            CodeRangeCalculator = codeRangeCalculator;
        }

        #region VISITORS

        public override AstNode VisitProgram(TypeScriptParser.ProgramContext context)
        {
            if (context.Stop == null)
            {
                string[] lines = MethodBodyRemovalResult.OriginalSource.Split('\n');

                return new AstNode.FileNode(
                    path: Path,
                    packageNode: new AstNode.PackageNode(null),
                    classes: new List<AstNode.ClassNode>(),
                    fields: new List<AstNode.FieldNode>(),
                    methods: new List<AstNode.MethodNode>(),
                    language: SourceCodeLanguage.TypeScript,
                    isTest: false,
                    namespaces: new List<AstNode.Namespace>(),
                    codeRange: new CodeRange(
                        new CodeLocation(1, 1),
                        new CodeLocation(lines.Length, lines.Last().Length)
                    )
                );
            }

            List<AstNode> children = AntlrUtil.WalkUntilType(context.children,
                new HashSet<Type>
                {
                    typeof(TypeScriptParser.ClassDeclarationContext),
                    typeof(TypeScriptParser.FunctionDeclarationContext),
                    typeof(TypeScriptParser.NamespaceDeclarationContext),
                    typeof(TypeScriptParser.VariableDeclarationContext)
                },
                this);

            List<AstNode.ClassNode> classes = children.OfType<AstNode.ClassNode>().ToList();
            List<AstNode.FieldNode> fields = children.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.MethodNode> methods = children.OfType<AstNode.MethodNode>().ToList();
            List<AstNode.Namespace> namespaces = children.OfType<AstNode.Namespace>().ToList();

            int headerEndIdxRestored = classes.Select(it => it.StartIdx - 1)
                .Concat(methods.Select(it => it.StartIdx - 1))
                .Concat(fields.Select(it => it.StartIdx - 1))
                .DefaultIfEmpty(MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex))
                .Min();

            CodeLocation headerEndLocationRestored = IndexToLocationConverter.IdxToLocation(headerEndIdxRestored);

            CodeRange codeRange = CodeRangeCalculator.Trim(
                new CodeRange(new CodeLocation(1, 1), headerEndLocationRestored)
            );

            return new AstNode.FileNode(
                path: Path,
                packageNode: new AstNode.PackageNode(null),
                classes: classes,
                fields: fields,
                methods: methods,
                language: SourceCodeLanguage.TypeScript,
                isTest: false,
                namespaces: namespaces,
                codeRange: codeRange
            );
        }

        public override AstNode VisitNamespaceDeclaration(TypeScriptParser.NamespaceDeclarationContext context)
        {
            List<AstNode> children = context.children
                .Select(it => it.Accept(this))
                .ToList();

            return new AstNode.Namespace(
                name: context.namespaceName().GetText(),
                classes: children.OfType<AstNode.ClassNode>().ToList(),
                fields: children.OfType<AstNode.FieldNode>().ToList(),
                methods: children.OfType<AstNode.MethodNode>().ToList(),
                namespaces: children.OfType<AstNode.Namespace>().ToList()
            );
        }

        public override AstNode VisitClassDeclaration(TypeScriptParser.ClassDeclarationContext context)
        {
            List<AstNode> children = AntlrUtil.WalkUntilType(context.children,
                new HashSet<Type>
                {
                    typeof(TypeScriptParser.ConstructorDeclarationContext),
                    typeof(TypeScriptParser.MethodDeclarationExpressionContext),
                    typeof(TypeScriptParser.PropertyDeclarationExpressionContext),
                    typeof(TypeScriptParser.VariableDeclarationContext),
                    typeof(TypeScriptParser.ClassDeclarationContext)
                },
                this);

            List<AstNode.FieldNode> fields = children.OfType<AstNode.FieldNode>().ToList();
            List<AstNode.MethodNode> methods = children.OfType<AstNode.MethodNode>().ToList();
            List<AstNode.ClassNode> innerClasses = children.OfType<AstNode.ClassNode>().ToList();

            int headerEndIdx = innerClasses.Select(it => it.StartIdx - 1)
                .Concat(methods.Select(it => it.StartIdx - 1))
                .Concat(fields.Select(it => it.StartIdx - 1))
                .DefaultIfEmpty(context.Stop.StopIndex)
                .Min();

            int headerStart = context.Start.StartIndex;

            int startIdx = MethodBodyRemovalResult.RestoreIdx(headerStart);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(headerEndIdx);

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx)
            );

            string? classNameString = context.Identifier().ToString();
            if (string.IsNullOrEmpty(classNameString))
            {
                classNameString = "anonymous";
            }

            return new AstNode.ClassNode(
                name: classNameString,
                methods: methods,
                fields: fields,
                innerClasses: innerClasses,
                modifier: AccessFlags.None,
                startIdx: startIdx,
                codeRange: codeRange
            );
        }

        public override AstNode VisitMethodDeclarationExpression(TypeScriptParser.MethodDeclarationExpressionContext context)
        {
            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            string? accFlagString = context.propertyMemberBase().accessibilityModifier()?.GetText();
            AccessFlags accessFlags = AccessFlags.AccPublic;
            if (!string.IsNullOrEmpty(accFlagString))
            {
                accessFlags = AccessFlagsFrom(accFlagString);
            }

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx)
            );

            string? methodNameString = context.propertyName().identifierName().Identifier().ToString();
            if (string.IsNullOrEmpty(methodNameString))
            {
                methodNameString = "anonymous";
            }

            return new AstNode.MethodNode(
                name: methodNameString,
                accFlag: accessFlags,
                startIdx: startIdx,
                codeRange: codeRange,
                arguments: new List<AstNode.ArgumentNode>()
            );
        }

        public override AstNode VisitFunctionDeclaration(TypeScriptParser.FunctionDeclarationContext context)
        {
            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx)
            );

            return new AstNode.MethodNode(
                name: context.Identifier().GetText(),
                accFlag: AccessFlags.None,
                startIdx: startIdx,
                codeRange: codeRange,
                arguments: new List<AstNode.ArgumentNode>()
            );
        }

        public override AstNode VisitPropertyDeclarationExpression(TypeScriptParser.PropertyDeclarationExpressionContext context)
        {
            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            string? accFlagString = context.propertyMemberBase().accessibilityModifier()?.GetText();
            AccessFlags accessFlags = AccessFlags.AccPublic;
            if (!string.IsNullOrEmpty(accFlagString))
            {
                accessFlags = AccessFlagsFrom(accFlagString);
            }

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx)
            );

            string? propertyNameString = context.propertyName().identifierName().Identifier().ToString();
            if (string.IsNullOrEmpty(propertyNameString))
            {
                propertyNameString = "anonymous";
            }
            
            return new AstNode.FieldNode(
                name: propertyNameString,
                accessFlags,
                startIdx: startIdx,
                codeRange: codeRange
            );
        }

        public override AstNode VisitVariableDeclaration(TypeScriptParser.VariableDeclarationContext context)
        {
            string varName = "anonymous";
            if (context.identifierOrKeyWord() != null &&
                context.identifierOrKeyWord().Identifier() != null)
            {
                varName = context.identifierOrKeyWord().Identifier().GetText();
            }

            bool isFunction = false;
            foreach (TypeScriptParser.SingleExpressionContext singleExpressionContext in context.singleExpression())
            {
                string s = singleExpressionContext.GetText();
                if (s.Contains("function"))
                {
                    isFunction = true;
                    break;
                }
            }

            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx)
            );

            AstNode returnNode;
            if (isFunction)
            {
                returnNode = new AstNode.MethodNode(
                    name: varName,
                    accFlag: AccessFlags.None,
                    startIdx: startIdx,
                    codeRange: codeRange,
                    arguments: new List<AstNode.ArgumentNode>());
            }
            else
            {
                returnNode = new AstNode.FieldNode(
                    name: varName,
                    accFlag: AccessFlags.None,
                    startIdx: startIdx,
                    codeRange: codeRange);
            }

            return returnNode;
        }
        
        public override AstNode VisitConstructorDeclaration(TypeScriptParser.ConstructorDeclarationContext context)
        {
            int startIdx = MethodBodyRemovalResult.RestoreIdx(context.Start.StartIndex);
            int endIdx = MethodBodyRemovalResult.RestoreIdx(context.Stop.StopIndex);
            
            string? accFlagString = context.accessibilityModifier()?.GetText();
            AccessFlags accessFlags = AccessFlags.AccPublic;
            if (!string.IsNullOrEmpty(accFlagString))
            {
                accessFlags = AccessFlagsFrom(accFlagString);
            }

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(startIdx, endIdx)
            );

            return new AstNode.MethodNode(
                name: "constructor",
                accFlag: accessFlags,
                startIdx: startIdx,
                codeRange: codeRange,
                arguments: new List<AstNode.ArgumentNode>()
            );
        }
        
        #endregion

        #region UTIL

        protected override AstNode AggregateResult(AstNode aggregate, AstNode nextResult)
        {
            return AstNode.NodeList.Combine(aggregate, nextResult);
        }

        static AccessFlags AccessFlagsFrom(string text)
        {
            return text switch
            {
                "public" => AccessFlags.AccPublic,
                "private" => AccessFlags.AccPrivate,
                _ => AccessFlags.None
            };
        }
        
        #endregion
    }
}