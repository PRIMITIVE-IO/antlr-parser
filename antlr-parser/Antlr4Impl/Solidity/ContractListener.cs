using System;
using System.Collections.Generic;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.Solidity
{
    public class ContractDefinitionListener : SolidityBaseListener
    {
        public ClassInfo ContractClassInfo;
        readonly string filePath;

        public ContractDefinitionListener(string filePath)
        {
            this.filePath = filePath;
        }

        public override void EnterContractDefinition(SolidityParser.ContractDefinitionContext context)
        {
            string classNameString = context.identifier().GetText();

            ClassName className = new ClassName(
                new FileName(filePath),
                new PackageName(),
                classNameString);

            ContractClassInfo = new ClassInfo(
                className,
                new List<MethodInfo>(),
                new List<FieldInfo>(),
                AccessFlags.AccPublic,
                new List<ClassInfo>(),
                new SourceCodeSnippet(context.GetFullText(), SourceCodeLanguage.Solidity),
                false);

            foreach (SolidityParser.ContractPartContext contractPartContext in context.contractPart())
            {
                ContractPartListener contractPartListener = new ContractPartListener(ContractClassInfo);
                contractPartContext.EnterRule(contractPartListener);
            }
        }

        class ContractPartListener : SolidityBaseListener
        {
            readonly ClassInfo classInfo;

            public ContractPartListener(ClassInfo classInfo)
            {
                this.classInfo = classInfo;
            }

            public override void EnterContractPart(SolidityParser.ContractPartContext context)
            {
                if (context.functionDefinition() != null)
                {
                    FunctionDefinitionListener functionDefinitionListener = new FunctionDefinitionListener(classInfo);
                    context.functionDefinition().EnterRule(functionDefinitionListener);
                }
            }

            class FunctionDefinitionListener : SolidityBaseListener
            {
                readonly ClassInfo classInfo;

                public FunctionDefinitionListener(ClassInfo classInfo)
                {
                    this.classInfo = classInfo;
                }

                public override void EnterFunctionDefinition(SolidityParser.FunctionDefinitionContext context)
                {
                    FunctionDescriptorListener functionDescriptorListener =
                        new FunctionDescriptorListener(classInfo.className.ShortName);
                    context.functionDescriptor().EnterRule(functionDescriptorListener);

                    ModifierListListener modifierListListener = new ModifierListListener();
                    context.modifierList().EnterRule(modifierListListener);

                    AccessFlags accessFlags = modifierListListener.AccessFlags;

                    // TODO
                    List<Argument> arguments = new List<Argument>();
                    TypeName returnType = TypeName.For("void");

                    MethodName methodName = new MethodName(
                        classInfo.className,
                        functionDescriptorListener.FunctionName,
                        returnType.Signature,
                        arguments);

                    MethodInfo methodInfo = new MethodInfo(
                        methodName,
                        accessFlags,
                        classInfo.className,
                        arguments,
                        returnType,
                        new SourceCodeSnippet(context.GetFullText(), SourceCodeLanguage.Solidity));

                    classInfo.Children.Add(methodInfo);
                }

                class FunctionDescriptorListener : SolidityBaseListener
                {
                    public string FunctionName;

                    public FunctionDescriptorListener(string classNameString)
                    {
                        // default to constructor name
                        FunctionName = classNameString;
                    }

                    public override void EnterFunctionDescriptor(SolidityParser.FunctionDescriptorContext context)
                    {
                        if (context.identifier() != null)
                        {
                            FunctionName = context.identifier().GetText();
                        }
                    }
                }

                class ModifierListListener : SolidityBaseListener
                {
                    public AccessFlags AccessFlags;
                    public TypeName ReturnType = TypeName.For("void");

                    public override void EnterModifierList(SolidityParser.ModifierListContext context)
                    {
                        string typeString = "";
                        foreach (SolidityParser.ModifierInvocationContext modifierInvocationContext in context
                            .modifierInvocation())
                        {
                            // TODO not sure if this is the return type
                            typeString += modifierInvocationContext.identifier().GetFullText();
                        }

                        if (!string.IsNullOrEmpty(typeString))
                        {
                            ReturnType = TypeName.For(typeString);
                        }

                        AccessFlags = AccessFlags.None;
                        string modText = context.GetFullText();
                        if (modText.StartsWith("public") || modText.StartsWith("external"))
                        {
                            AccessFlags = AccessFlags.AccPublic;
                        }
                        else if (modText.StartsWith("private") || modText.StartsWith("internal"))
                        {
                            AccessFlags = AccessFlags.AccPrivate;
                        }
                        else if (modText.Contains("public"))
                        {
                            AccessFlags = AccessFlags.AccPublic;
                        }
                        else
                        {
                            PrimitiveLogger.Logger.Instance().Warn($"unknown modifier {modText}");
                        }
                    }
                }
            }
        }
    }
}