using System;
using System.Collections.Generic;
using PrimitiveCodebaseElements.Primitive;

namespace antlr_parser.Antlr4Impl.Solidity
{
    public class ContractDefinitionListener : SolidityBaseListener
    {
        readonly ClassInfo outerFileClass;

        public ContractDefinitionListener(ClassInfo outerFileClass)
        {
            this.outerFileClass = outerFileClass;
        }

        public override void EnterContractDefinition(SolidityParser.ContractDefinitionContext context)
        {
            string classNameString = $"{outerFileClass.className.ShortName}${context.identifier().GetText()}";

            ClassName className = new ClassName(
                outerFileClass.className.ContainmentFile(),
                outerFileClass.className.ContainmentPackage,
                classNameString);

            ClassInfo classInfo = new ClassInfo(
                className,
                new List<MethodInfo>(),
                new List<FieldInfo>(),
                AccessFlags.AccPublic,
                new List<ClassInfo>(),
                new SourceCodeSnippet(context.GetFullText(), SourceCodeLanguage.Solidity),
                false);

            outerFileClass.Children.Add(classInfo);

            foreach (SolidityParser.ContractPartContext contractPartContext in context.contractPart())
            {
                ContractPartListener contractPartListener = new ContractPartListener(classInfo);
                contractPartContext.EnterRule(contractPartListener);
            }
        }
    }

    public class ContractPartListener : SolidityBaseListener
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
            FunctionDescriptorListener functionDescriptorListener = new FunctionDescriptorListener();
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
            public string FunctionName = "anonymous";
            public override void EnterFunctionDescriptor(SolidityParser.FunctionDescriptorContext context)
            {
                if (context.identifier() != null)
                {
                    FunctionName = context.identifier().GetText();
                }
            }
        }
    } 
    
    public class ModifierListListener : SolidityBaseListener
    {
        public AccessFlags AccessFlags;
        public override void EnterModifierList(SolidityParser.ModifierListContext context)
        {
            foreach (SolidityParser.ModifierInvocationContext modifierInvocationContext in context.modifierInvocation())
            {
                string s = modifierInvocationContext.identifier().GetFullText();
                Console.WriteLine(s);
            }
            
            AccessFlags = AccessFlags.None;
            string modText = context.GetFullText();
            switch (modText)
            {
                case "public":
                case "external":
                case "external view":
                    AccessFlags = AccessFlags.AccPublic;
                    break;
                case "internal":
                case "internal view":
                    AccessFlags = AccessFlags.AccPrivate;
                    break;
                default:
                    Console.WriteLine(modText);
                    break;
            }

            
        }
    }
}