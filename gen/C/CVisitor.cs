//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ANTLR Version: 4.9.1
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from C:/Users/john/Desktop/PRIMITIVE_Tools/antlr-parser/grammars\C.g4 by ANTLR 4.9.1

// Unreachable code detected
#pragma warning disable 0162
// The variable '...' is assigned but its value is never used
#pragma warning disable 0219
// Missing XML comment for publicly visible type or member '...'
#pragma warning disable 1591
// Ambiguous reference in cref attribute
#pragma warning disable 419

using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using IToken = Antlr4.Runtime.IToken;

/// <summary>
/// This interface defines a complete generic visitor for a parse tree produced
/// by <see cref="CParser"/>.
/// </summary>
/// <typeparam name="Result">The return type of the visit operation.</typeparam>
[System.CodeDom.Compiler.GeneratedCode("ANTLR", "4.9.1")]
[System.CLSCompliant(false)]
public interface ICVisitor<Result> : IParseTreeVisitor<Result> {
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.primaryExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPrimaryExpression([NotNull] CParser.PrimaryExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.genericSelection"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitGenericSelection([NotNull] CParser.GenericSelectionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.genericAssocList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitGenericAssocList([NotNull] CParser.GenericAssocListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.genericAssociation"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitGenericAssociation([NotNull] CParser.GenericAssociationContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.postfixExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPostfixExpression([NotNull] CParser.PostfixExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.argumentExpressionList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitArgumentExpressionList([NotNull] CParser.ArgumentExpressionListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.unaryExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitUnaryExpression([NotNull] CParser.UnaryExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.unaryOperator"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitUnaryOperator([NotNull] CParser.UnaryOperatorContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.castExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCastExpression([NotNull] CParser.CastExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.multiplicativeExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitMultiplicativeExpression([NotNull] CParser.MultiplicativeExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.additiveExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAdditiveExpression([NotNull] CParser.AdditiveExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.shiftExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitShiftExpression([NotNull] CParser.ShiftExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.relationalExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitRelationalExpression([NotNull] CParser.RelationalExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.equalityExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEqualityExpression([NotNull] CParser.EqualityExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.andExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAndExpression([NotNull] CParser.AndExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.exclusiveOrExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExclusiveOrExpression([NotNull] CParser.ExclusiveOrExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.inclusiveOrExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitInclusiveOrExpression([NotNull] CParser.InclusiveOrExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.logicalAndExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLogicalAndExpression([NotNull] CParser.LogicalAndExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.logicalOrExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLogicalOrExpression([NotNull] CParser.LogicalOrExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.conditionalExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitConditionalExpression([NotNull] CParser.ConditionalExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.assignmentExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAssignmentExpression([NotNull] CParser.AssignmentExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.assignmentOperator"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAssignmentOperator([NotNull] CParser.AssignmentOperatorContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.expression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpression([NotNull] CParser.ExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.constantExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitConstantExpression([NotNull] CParser.ConstantExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.declaration"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDeclaration([NotNull] CParser.DeclarationContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.declarationSpecifiers"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDeclarationSpecifiers([NotNull] CParser.DeclarationSpecifiersContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.declarationSpecifiers2"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDeclarationSpecifiers2([NotNull] CParser.DeclarationSpecifiers2Context context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.declarationSpecifier"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDeclarationSpecifier([NotNull] CParser.DeclarationSpecifierContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.initDeclaratorList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitInitDeclaratorList([NotNull] CParser.InitDeclaratorListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.initDeclarator"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitInitDeclarator([NotNull] CParser.InitDeclaratorContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.storageClassSpecifier"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStorageClassSpecifier([NotNull] CParser.StorageClassSpecifierContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.typeSpecifier"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypeSpecifier([NotNull] CParser.TypeSpecifierContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.structOrUnionSpecifier"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStructOrUnionSpecifier([NotNull] CParser.StructOrUnionSpecifierContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.structOrUnion"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStructOrUnion([NotNull] CParser.StructOrUnionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.structDeclarationList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStructDeclarationList([NotNull] CParser.StructDeclarationListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.structDeclaration"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStructDeclaration([NotNull] CParser.StructDeclarationContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.specifierQualifierList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSpecifierQualifierList([NotNull] CParser.SpecifierQualifierListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.structDeclaratorList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStructDeclaratorList([NotNull] CParser.StructDeclaratorListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.structDeclarator"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStructDeclarator([NotNull] CParser.StructDeclaratorContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.enumSpecifier"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEnumSpecifier([NotNull] CParser.EnumSpecifierContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.enumeratorList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEnumeratorList([NotNull] CParser.EnumeratorListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.enumerator"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEnumerator([NotNull] CParser.EnumeratorContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.enumerationConstant"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitEnumerationConstant([NotNull] CParser.EnumerationConstantContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.atomicTypeSpecifier"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAtomicTypeSpecifier([NotNull] CParser.AtomicTypeSpecifierContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.typeQualifier"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypeQualifier([NotNull] CParser.TypeQualifierContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.functionSpecifier"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFunctionSpecifier([NotNull] CParser.FunctionSpecifierContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.alignmentSpecifier"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAlignmentSpecifier([NotNull] CParser.AlignmentSpecifierContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.declarator"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDeclarator([NotNull] CParser.DeclaratorContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.directDeclarator"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDirectDeclarator([NotNull] CParser.DirectDeclaratorContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.gccDeclaratorExtension"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitGccDeclaratorExtension([NotNull] CParser.GccDeclaratorExtensionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.gccAttributeSpecifier"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitGccAttributeSpecifier([NotNull] CParser.GccAttributeSpecifierContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.gccAttributeList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitGccAttributeList([NotNull] CParser.GccAttributeListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.gccAttribute"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitGccAttribute([NotNull] CParser.GccAttributeContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.nestedParenthesesBlock"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitNestedParenthesesBlock([NotNull] CParser.NestedParenthesesBlockContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.pointer"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitPointer([NotNull] CParser.PointerContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.typeQualifierList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypeQualifierList([NotNull] CParser.TypeQualifierListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.parameterTypeList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitParameterTypeList([NotNull] CParser.ParameterTypeListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.parameterList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitParameterList([NotNull] CParser.ParameterListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.parameterDeclaration"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitParameterDeclaration([NotNull] CParser.ParameterDeclarationContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.identifierList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIdentifierList([NotNull] CParser.IdentifierListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.typeName"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypeName([NotNull] CParser.TypeNameContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.abstractDeclarator"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitAbstractDeclarator([NotNull] CParser.AbstractDeclaratorContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.directAbstractDeclarator"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDirectAbstractDeclarator([NotNull] CParser.DirectAbstractDeclaratorContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.typedefName"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTypedefName([NotNull] CParser.TypedefNameContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.initializer"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitInitializer([NotNull] CParser.InitializerContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.initializerList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitInitializerList([NotNull] CParser.InitializerListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.designation"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDesignation([NotNull] CParser.DesignationContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.designatorList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDesignatorList([NotNull] CParser.DesignatorListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.designator"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDesignator([NotNull] CParser.DesignatorContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.staticAssertDeclaration"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStaticAssertDeclaration([NotNull] CParser.StaticAssertDeclarationContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.statement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitStatement([NotNull] CParser.StatementContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.labeledStatement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitLabeledStatement([NotNull] CParser.LabeledStatementContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.compoundStatement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCompoundStatement([NotNull] CParser.CompoundStatementContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.blockItemList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBlockItemList([NotNull] CParser.BlockItemListContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.blockItem"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitBlockItem([NotNull] CParser.BlockItemContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.expressionStatement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExpressionStatement([NotNull] CParser.ExpressionStatementContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.selectionStatement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitSelectionStatement([NotNull] CParser.SelectionStatementContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.iterationStatement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitIterationStatement([NotNull] CParser.IterationStatementContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.forCondition"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitForCondition([NotNull] CParser.ForConditionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.forDeclaration"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitForDeclaration([NotNull] CParser.ForDeclarationContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.forExpression"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitForExpression([NotNull] CParser.ForExpressionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.jumpStatement"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitJumpStatement([NotNull] CParser.JumpStatementContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.compilationUnit"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitCompilationUnit([NotNull] CParser.CompilationUnitContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.translationUnit"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitTranslationUnit([NotNull] CParser.TranslationUnitContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.externalDeclaration"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitExternalDeclaration([NotNull] CParser.ExternalDeclarationContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.functionDefinition"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitFunctionDefinition([NotNull] CParser.FunctionDefinitionContext context);
	/// <summary>
	/// Visit a parse tree produced by <see cref="CParser.declarationList"/>.
	/// </summary>
	/// <param name="context">The parse tree.</param>
	/// <return>The visitor result.</return>
	Result VisitDeclarationList([NotNull] CParser.DeclarationListContext context);
}
