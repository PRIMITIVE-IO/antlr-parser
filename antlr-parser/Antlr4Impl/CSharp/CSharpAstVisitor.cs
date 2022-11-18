using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using static antlr_parser.Antlr4Impl.AntlrUtil;
using CodeRange = PrimitiveCodebaseElements.Primitive.dto.CodeRange;

namespace antlr_parser.Antlr4Impl.CSharp
{
    public class CSharpAstVisitor : CSharpParserBaseVisitor<AstNode>
    {
        readonly string Path;
        readonly IndexToLocationConverter IndexToLocationConverter;
        readonly MethodBodyRemovalResult MethodBodyRemovalResult;
        readonly CodeRangeCalculator CodeRangeCalculator;

        public CSharpAstVisitor(
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

        public override AstNode VisitCompilation_unit(CSharpParser.Compilation_unitContext context)
        {
            List<AstNode> parsedChildren = context.children
                .Select(it => it.Accept(this))
                .ToList();

            CodeRange codeRange = CodeRangeCalculator.Trim(
                new CodeRange(new CodeLocation(1, 1), CodeRangeCalculator.EndPosition()));

            return new AstNode.FileNode(
                path: Path,
                packageNode: null,
                classes: parsedChildren.OfType<AstNode.ClassNode>().ToList(),
                methods: new List<AstNode.MethodNode>(),
                fields: new List<AstNode.FieldNode>(),
                namespaces: parsedChildren.OfType<AstNode.Namespace>().ToList(),
                language: SourceCodeLanguage.CSharp,
                isTest: false,
                codeRange: codeRange
            );
        }

        public override AstNode VisitNamespace_declaration(CSharpParser.Namespace_declarationContext context)
        {
            string name = context.qualified_identifier().identifier().First().GetText();
            List<AstNode> astNodes = context.namespace_body().children
                .Select(it => it.Accept(this))
                .Where(it => it != null)
                .SelectMany(it => it.AsList())
                .ToList();

            List<AstNode.ClassNode> classes = astNodes.OfType<AstNode.ClassNode>().ToList();
            List<AstNode.Namespace> namespaces = astNodes.OfType<AstNode.Namespace>().ToList();

            return new AstNode.Namespace(
                name: name,
                classes: classes,
                fields: new List<AstNode.FieldNode>(),
                methods: new List<AstNode.MethodNode>(),
                namespaces: namespaces
            );
        }

        public override AstNode VisitClass_definition(CSharpParser.Class_definitionContext context)
        {
            List<AstNode> children = context.children
                .Select(it => it.Accept(this))
                .Where(it => it != null)
                .SelectMany(it => it.AsList())
                .ToList();

            int startIdx = context.Start.StartIndex;
            int endIdx = context.class_body().OPEN_BRACE().Symbol.StartIndex - 1;

            CSharpParser.Class_bodyContext? parentClassBodyContext =
                FindParent<CSharpParser.Class_bodyContext>(context);

            int? nearestPeerEndIdx = NearestPeerEndIdx(parentClassBodyContext, context.Start.StartIndex);


            int? prevEndPosition = nearestPeerEndIdx ??
                                   parentClassBodyContext?.OPEN_BRACE().Symbol.StartIndex ??
                                   PreviousPeerClassEndPosition(context, startIdx);

            int headerStartIdx = (prevEndPosition ?? -1) + 1;


            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(
                    MethodBodyRemovalResult.RestoreIdx(headerStartIdx),
                    MethodBodyRemovalResult.RestoreIdx(endIdx)
                )
            );

            return new AstNode.ClassNode(
                name: context.identifier().GetText(),
                methods: children.OfType<AstNode.MethodNode>().ToList(),
                fields: children.OfType<AstNode.FieldNode>().ToList(),
                innerClasses: children.OfType<AstNode.ClassNode>().ToList(),
                modifier: AccessFlags.None, //TODO
                startIdx: headerStartIdx,
                codeRange: codeRange
            );
        }

        public override AstNode VisitStruct_definition(CSharpParser.Struct_definitionContext context)
        {
            List<AstNode> children = context.children
                .Select(it => it.Accept(this))
                .Where(it => it != null)
                .SelectMany(it => it.AsList())
                .ToList();

            int startIdx = context.Start.StartIndex;
            int endIdx = context.struct_body().OPEN_BRACE().Symbol.StartIndex - 1;

            CSharpParser.Class_bodyContext parentClassBodyContext = FindParent<CSharpParser.Class_bodyContext>(context);
            int? nearestPeerEndIdx = NearestPeerEndIdx(parentClassBodyContext, context.Start.StartIndex);


            int? prevEndPosition = nearestPeerEndIdx ??
                                   parentClassBodyContext?.OPEN_BRACE().Symbol.StartIndex ??
                                   PreviousPeerClassEndPosition(context, startIdx);

            int headerStartIdx = (prevEndPosition ?? -1) + 1;

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(
                    MethodBodyRemovalResult.RestoreIdx(headerStartIdx),
                    MethodBodyRemovalResult.RestoreIdx(endIdx)
                )
            );

            return new AstNode.ClassNode(
                name: context.identifier().GetText(),
                methods: children.OfType<AstNode.MethodNode>().ToList(),
                fields: children.OfType<AstNode.FieldNode>().ToList(),
                innerClasses: children.OfType<AstNode.ClassNode>().ToList(),
                modifier: AccessFlags.None, //TODO
                startIdx: headerStartIdx,
                codeRange: codeRange
            );
        }

        public override AstNode VisitInterface_definition(CSharpParser.Interface_definitionContext context)
        {
            List<AstNode> children = context.children
                .Select(it => it.Accept(this))
                .Where(it => it != null)
                .SelectMany(it => it.AsList())
                .ToList();

            int startIdx = context.Start.StartIndex;
            int endIdx = context.class_body().OPEN_BRACE().Symbol.StartIndex - 1;

            CSharpParser.Class_bodyContext parentClassBodyContext = FindParent<CSharpParser.Class_bodyContext>(context);
            int? nearestPeerEndIdx = NearestPeerEndIdx(parentClassBodyContext, context.Start.StartIndex);


            int? prevEndPosition = nearestPeerEndIdx ??
                                   parentClassBodyContext?.OPEN_BRACE().Symbol.StartIndex ??
                                   PreviousPeerClassEndPosition(context, startIdx);

            int headerStartIdx = (prevEndPosition ?? -1) + 1;

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(
                    MethodBodyRemovalResult.RestoreIdx(headerStartIdx),
                    MethodBodyRemovalResult.RestoreIdx(endIdx)
                )
            );

            return new AstNode.ClassNode(
                name: context.identifier().GetText(),
                methods: children.OfType<AstNode.MethodNode>().ToList(),
                fields: children.OfType<AstNode.FieldNode>().ToList(),
                innerClasses: children.OfType<AstNode.ClassNode>().ToList(),
                modifier: AccessFlags.None, //TODO
                startIdx: headerStartIdx,
                codeRange: codeRange
            );
        }

        public override AstNode VisitEnum_definition(CSharpParser.Enum_definitionContext context)
        {
            int startIdx = context.Start.StartIndex;
            int endIdx = context.enum_body().CLOSE_BRACE().Symbol.StartIndex;

            CSharpParser.Class_bodyContext parentClassBodyContext = FindParent<CSharpParser.Class_bodyContext>(context);

            int? prevEndPosition = NearestPeerEndIdx(parentClassBodyContext, context.Start.StartIndex) ??
                                   parentClassBodyContext?.OPEN_BRACE().Symbol.StartIndex ??
                                   PreviousPeerClassEndPosition(context, startIdx);

            int headerStartIdx = (prevEndPosition ?? -1) + 1;

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(
                    MethodBodyRemovalResult.RestoreIdx(headerStartIdx),
                    MethodBodyRemovalResult.RestoreIdx(endIdx)
                )
            );

            return new AstNode.ClassNode(
                name: context.identifier().GetText(),
                methods: new List<AstNode.MethodNode>(),
                fields: new List<AstNode.FieldNode>(),
                innerClasses: new List<AstNode.ClassNode>(),
                modifier: AccessFlags.None, //TODO
                startIdx: headerStartIdx,
                codeRange: codeRange
            );
        }

        static List<CSharpParser.Class_definitionContext> FindClassDefinitionContexts(IParseTree parseTree)
        {
            List<CSharpParser.Class_definitionContext> result = new List<CSharpParser.Class_definitionContext>();
            for (int i = 0; i < parseTree.ChildCount; i++)
            {
                IParseTree child = parseTree.GetChild(i);
                if (child is CSharpParser.Class_definitionContext)
                {
                    result.Add(child as CSharpParser.Class_definitionContext);
                }
                else
                {
                    result.AddRange(FindClassDefinitionContexts(child));
                }
            }

            return result;
        }

        public override AstNode VisitField_declaration(CSharpParser.Field_declarationContext context)
        {
            AccessFlags accFlag = FieldAccessFlag(context);
            int startIdx = CalculateFieldStartIdx(context);
            int endIdx = context.Stop.StopIndex;
            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(
                    MethodBodyRemovalResult.RestoreIdx(startIdx),
                    MethodBodyRemovalResult.RestoreIdx(endIdx)
                )
            );

            string name = context.variable_declarators().variable_declarator()
                .First() //TODO multideclaration
                .identifier().GetText();

            return new AstNode.FieldNode(
                name: name,
                accFlag: accFlag,
                startIdx: startIdx,
                codeRange: codeRange
            );
        }

        public override AstNode VisitProperty_declaration(CSharpParser.Property_declarationContext context)
        {
            AccessFlags accFlag = FieldAccessFlag(context);
            int startIdx = CalculateFieldStartIdx(context);
            int endIdx = context.Stop.StopIndex;

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(
                    MethodBodyRemovalResult.RestoreIdx(startIdx),
                    MethodBodyRemovalResult.RestoreIdx(endIdx)
                )
            );

            string name = context.member_name().namespace_or_type_name().identifier().First().GetText();

            return new AstNode.FieldNode(
                name: name,
                accFlag: accFlag,
                startIdx: startIdx,
                codeRange: codeRange
            );
        }

        public override AstNode VisitEvent_declaration(CSharpParser.Event_declarationContext context)
        {
            AccessFlags accFlag = FieldAccessFlag(context);
            int startIdx = CalculateFieldStartIdx(context);
            int endIdx = context.Stop.StopIndex;

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(
                    MethodBodyRemovalResult.RestoreIdx(startIdx),
                    MethodBodyRemovalResult.RestoreIdx(endIdx)
                )
            );

            string name = context.member_name()?.namespace_or_type_name()?.identifier()?.First()?.GetText()
                          ?? context.variable_declarators().variable_declarator().First().identifier().GetText();

            return new AstNode.FieldNode(
                name: name,
                accFlag: accFlag,
                startIdx: -1,
                codeRange: codeRange
            );
        }
        
        public override AstNode VisitMethod_declaration(CSharpParser.Method_declarationContext context)
        {
            AccessFlags accFlag = MethodAccessFlag(context);

            int startIdx = MethodStartIdx(context);

            int endIdx = context.Stop.StopIndex;

            CodeRange codeRange = CodeRangeCalculator.Trim(
                IndexToLocationConverter.IdxToCodeRange(
                    MethodBodyRemovalResult.RestoreIdx(startIdx),
                    MethodBodyRemovalResult.RestoreIdx(endIdx)
                )
            );
            
            List<AstNode.ArgumentNode> arguments = context.formal_parameter_list()?.fixed_parameters()?.fixed_parameter()?
                .Select(param => param.Accept(this) as AstNode.ArgumentNode)
                .ToList() ?? new List<AstNode.ArgumentNode>();

            List<string> returnTypes = context.type_parameter_list()?.type_parameter()?
                .Select(param => param.identifier()?.GetText() ?? "void")
                .ToList() ?? new List<string> { "void" };

            string commaSeparatedTypes = string.Join(",", returnTypes);
            
            return new AstNode.MethodNode(
                name: context.method_member_name().identifier().First().GetText(),
                accFlag: accFlag,
                startIdx: startIdx,
                codeRange: codeRange,
                arguments: arguments ,
                returnType: commaSeparatedTypes
            );
        }
public override AstNode VisitFixed_parameter(CSharpParser.Fixed_parameterContext context)
        {
            return new AstNode.ArgumentNode(
                name: context.arg_declaration().identifier().GetText(),
                type: context.arg_declaration().type_().GetText());
        }
        
        #endregion

        #region UTIL

        static int CalculateFieldStartIdx(ParserRuleContext context)
        {
            CSharpParser.Class_bodyContext? parentClassBodyContext =
                FindParent<CSharpParser.Class_bodyContext>(context);
            if (parentClassBodyContext != null)
            {
                return (NearestPeerEndIdx(parentClassBodyContext, context.Start.StartIndex) ??
                        parentClassBodyContext.OPEN_BRACE().Symbol.StartIndex) + 1;
            }

            CSharpParser.Struct_bodyContext? parentStructBodyContext =
                FindParent<CSharpParser.Struct_bodyContext>(context);

            return (NearestPeerEndIdx(parentStructBodyContext, context.Start.StartIndex) ??
                    parentStructBodyContext.OPEN_BRACE().Symbol.StartIndex) + 1;
        }
        
        static int? NearestPeerEndIdx(CSharpParser.Class_bodyContext? ctx, int startIdx)
        {
            return ctx?.class_member_declarations().class_member_declaration()
                .Select(it => it.Stop.StopIndex as int?)
                .TakeWhile(it => it < startIdx)
                .Max();
        }

        static int? NearestPeerEndIdx(CSharpParser.Struct_bodyContext? ctx, int startIdx)
        {
            return ctx?.struct_member_declaration()
                .Select(it => it.Stop.StopIndex as int?)
                .TakeWhile(it => it < startIdx)
                .Max();
        }

        static int? PreviousPeerClassEndPosition(ParserRuleContext context, int startIdx)
        {
            CSharpParser.Namespace_member_declarationsContext ns =
                FindParent<CSharpParser.Namespace_member_declarationsContext>(context);
            List<CSharpParser.Class_definitionContext> classes = FindClassDefinitionContexts(ns);

            return classes
                .Select(it => it.Stop.StopIndex as int?)
                .TakeWhile(it => it < startIdx)
                .Max();
        }

        static AccessFlags FieldAccessFlag(ParserRuleContext context)
        {
            CSharpParser.Class_member_declarationContext? classMemberDeclarationContext =
                FindParent<CSharpParser.Class_member_declarationContext>(context);

            if (classMemberDeclarationContext != null)
            {
                return classMemberDeclarationContext
                    .all_member_modifiers()
                    ?.all_member_modifier()
                    .Select(it => Flags(it.GetText()))
                    .FirstOrDefault(it => it != null) ?? AccessFlags.None;
            }

            return FindParent<CSharpParser.Struct_member_declarationContext>(context)
                ?.all_member_modifiers()
                ?.all_member_modifier()
                .Select(it => Flags(it.GetText()))
                .FirstOrDefault(it => it != null) ?? AccessFlags.None;
        }

        static AccessFlags? Flags(string flag)
        {
            switch (flag)
            {
                case "private": return AccessFlags.AccPrivate;
                case "protected": return AccessFlags.AccProtected;
                case "public": return AccessFlags.AccPublic;
                default: return null;
            }
        }

        static int MethodStartIdx(CSharpParser.Method_declarationContext context)
        {
            CSharpParser.Class_bodyContext? parentClassBodyContext =
                FindParent<CSharpParser.Class_bodyContext>(context);
            if (parentClassBodyContext != null)
            {
                return (NearestPeerEndIdx(parentClassBodyContext, context.Start.StartIndex) ??
                        parentClassBodyContext.OPEN_BRACE().Symbol.StartIndex) + 1;
            }

            CSharpParser.Struct_bodyContext? parentStructBodyContext =
                FindParent<CSharpParser.Struct_bodyContext>(context);
            return (NearestPeerEndIdx(parentStructBodyContext, context.Start.StartIndex) ??
                    parentStructBodyContext.OPEN_BRACE().Symbol.StartIndex) + 1;
        }

        static AccessFlags MethodAccessFlag(CSharpParser.Method_declarationContext context)
        {
            CSharpParser.Class_member_declarationContext? classMemberDeclarationContext =
                FindParent<CSharpParser.Class_member_declarationContext>(context);

            CSharpParser.All_member_modifierContext[]? allMemberModifierContexts;
            if (classMemberDeclarationContext != null)
            {
                allMemberModifierContexts = classMemberDeclarationContext
                    .all_member_modifiers()
                    ?.all_member_modifier();
            }
            else
            {
                allMemberModifierContexts = FindParent<CSharpParser.Struct_member_declarationContext>(context)
                    ?.all_member_modifiers()
                    ?.all_member_modifier();
            }

            return allMemberModifierContexts
                ?.Select(it => Flags(it.GetText()))
                .FirstOrDefault(it => it != null) ?? AccessFlags.None;
        }

        protected override AstNode AggregateResult(AstNode aggregate, AstNode nextResult)
        {
            return AstNode.NodeList.Combine(aggregate, nextResult);
        }
        
        #endregion
    }
}