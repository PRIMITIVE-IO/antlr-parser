using System;
using System.Collections.Generic;
using System.Linq;
using antlr_parser.Antlr4Impl;
using antlr_parser.Antlr4Impl.CPP;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using Xunit;

namespace antlr_parser.tests.CPP
{
    public class AntlrParseCppTest
    {
        [Fact]
        void ParseFunction()
        {
            string source = @"
                int f(int a, int b) {
                   return 10; 
                }
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.First();
            classInfo.Name.ShortName.Should().Be("path");
            MethodInfo method = classInfo.Methods.Single();

            method.Name.ShortName.Should().Be("f");
            method.SourceCode.Text.Should().Be(@"
                int f(int a, int b) {
                   return 10; 
                }
            ".TrimIndent().Trim());
        }

        [Fact]
        void ParseFunctionWithSemicolon()
        {
            string source = @"
                int f(int a, int b) {
                   return 10; 
                };
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.First();
            classInfo.Name.ShortName.Should().Be("path");
            MethodInfo method = classInfo.Methods.Single();

            method.Name.ShortName.Should().Be("f");
            method.SourceCode.Text.Should().Be(@"
                int f(int a, int b) {
                   return 10; 
                };
            ".TrimIndent().Trim());
        }

        [Fact]
        void ParseFunctionForwardDeclaration()
        {
            string source = @"
                int f(int a, int b);
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.First();
            classInfo.Name.ShortName.Should().Be("path");
            MethodInfo method = classInfo.Methods.Single();

            method.Name.ShortName.Should().Be("f");
            method.SourceCode.Text.Should().Be(@"
                int f(int a, int b);
            ".TrimIndent().Trim());
        }

        [Fact]
        void ParseClass()
        {
            string source = @"
                class A {
                    int x = 10;
                    int f(int a, int b) {
                       return 10; 
                    }
                };
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.First();
            classInfo.Name.ShortName.Should().Be("A");
            MethodInfo method = classInfo.Methods.Single();

            method.Name.ShortName.Should().Be("f");
            method.SourceCode.Text.Should().Be(@"
                int f(int a, int b) {
                   return 10; 
                }
            ".TrimIndent().Trim());

            FieldInfo field = classInfo.Fields.Single();
            field.Name.ShortName.Should().Be("x");
        }

        [Fact]
        void ParseNestedClass()
        {
            string source = @"
                class A {
                    class B{};
                    class C;
                };
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.First();
            classInfo.Name.ShortName.Should().Be("A");

            ClassInfo nestedClass = classInfo.InnerClasses.ToArray()[0];
            nestedClass.Name.ShortName.Should().Be("B");
            ClassInfo nestedClassWithForwardDeclaration = classInfo.InnerClasses.ToArray()[1];
            nestedClassWithForwardDeclaration.Name.ShortName.Should().Be("C");
        }

        [Fact]
        void ParseClassWithForwardDeclaration()
        {
            string source = @"
                class A;
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.First();
            classInfo.Name.ShortName.Should().Be("A");
        }

        [Fact]
        void ParseClassWithForwardDeclarationField()
        {
            string source = @"
                class A {
                    int x;
                };
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.First();
            classInfo.Name.ShortName.Should().Be("A");
            classInfo.Fields.Single().Name.ShortName.Should().Be("x");
        }

        [Fact]
        void ParseClassWithAccessModifier()
        {
            string source = @"
                class A {
                    public:
                    int x;
                    int y = 10;
                    int f();
                    int h(){return 0;};
                };
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.First();
            classInfo.Name.ShortName.Should().Be("A");
            classInfo.Fields.ToArray()[0].Name.ShortName.Should().Be("x");
            classInfo.Fields.ToArray()[1].Name.ShortName.Should().Be("y");
            classInfo.Methods.ToArray()[0].Name.ShortName.Should().Be("f");
            classInfo.Methods.ToArray()[1].Name.ShortName.Should().Be("h");
        }

        [Fact]
        void ParseClassWithForwardDeclarationMethod()
        {
            string source = @"
                class A {
                    double f();
                };
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.First();
            classInfo.Name.ShortName.Should().Be("A");
            classInfo.Methods.Single().Name.ShortName.Should().Be("f");
        }

        [Fact]
        void ParseStructAsClass()
        {
            string source = @"
                struct S {
                    static int x;
                    void f(int i);
                };
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.First();
            classInfo.Name.ShortName.Should().Be("S");
            classInfo.Fields.Single().Name.ShortName.Should().Be("x");
            classInfo.Methods.Single().Name.ShortName.Should().Be("f");
        }

        [Fact]
        void ParseTopLevelFields()
        {
            string source = @"
                    int x;
                    int y = 0;
                    static a b GUARDED_BY(c) = 0;
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.First();
            classInfo.Fields.ToArray()[0].Name.ShortName.Should().Be("x");
            classInfo.Fields.ToArray()[1].Name.ShortName.Should().Be("y");
            // classInfo.Fields.ToArray()[2].Name.ShortName.Should().Be("b"); TODO GUARDED BY is a macro :(
        }

        [Fact]
        void FunctionCallWithDoubleColons()
        {
            string source = @"
                    T1::T1(const T2& x) : x(x)
                    {
                       
                    }
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.First();
            classInfo.Methods.Single().Name.ShortName.Should().Be("T1");
        }

        [Fact]
        void FunctionCallWithDoubleColonsAndType()
        {
            string source = @"
                bool T::operator()() const {
                 
                }
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.First();
            classInfo.Methods.Single().Name.ShortName.Should().Be("()");
        }

        [Fact]
        void ParseNamespace()
        {
            string source = @"
                namespace {
                    class A{};
                }
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.Single();
            classInfo.Name.ShortName.Should().Be("A");
        }

        [Fact]
        void ParseTemplate()
        {
            string source = @"
                template<typename T>
                CSHA512& operator<<() {
                    
                }
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.Single();
            classInfo.Methods.Single().Name.ShortName.Should().Be("<<");
        }

        [Fact]
        void ParseEnum()
        {
            string source = @"
                enum class E {
                    A,
                    B,
                };
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.Single();
            classInfo.Name.ShortName.Should().Be("E");
            classInfo.SourceCode.Text.Should().Be(@"
                enum class E {
            ".TrimIndent().Trim());
        }

        [Fact]
        void ParseWierdFunctionName()
        {
            string source = @"
                Session::~Session(){}
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.Single();
            classInfo.Methods.Single().Name.ShortName.Should().Be("~Session");
        }

        [Fact]
        void ParseConversionOperator()
        {
            string source = @"
                CThreadInterrupt::operator bool() const
                {
                }
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.Single();
            classInfo.Methods.Single().Name.ShortName.Should().Be("bool");
        }     
        [Fact]
        void StaticConst()
        {
            string source = @"
                static const size_t MAX_GETUTXOS_OUTPOINTS = 15;
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.Single();
            classInfo.Fields.Single().Name.ShortName.Should().Be("MAX_GETUTXOS_OUTPOINTS");
        }

        //[Fact]TODO
        void HaveNoIdea()
        {
            string source = @"
                struct {
                    bool (*handler)(const std::any& context, HTTPRequest* req, const std::string& strReq);
                };
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            throw new Exception();
        }

        [Fact]
        void NestedNamespaces()
        {
            string source = @"
                namespace leveldb
                {
                    namespace {
                    static uint32_t BloomHash(const Slice & key) {
                        return Hash(key.data(), key.size(), 0xbc9f1d34);
                    }

                } // namespace

                const FilterPolicy* NewBloomFilterPolicy(int bits_per_key) {
                    return new BloomFilterPolicy(bits_per_key);
                }

                } // namespace leveldb
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.Single();
            classInfo.Name.ShortName.Should().Be("path");
            classInfo.InnerClasses.Should().BeEmpty();
            classInfo.Methods.ToArray()[0].Name.ShortName.Should().Be("NewBloomFilterPolicy");
            classInfo.Methods.ToArray()[1].Name.ShortName.Should().Be("BloomHash");
        }
        
        [Fact]
        void ClassHeaderIncludesComment()
        {
            string source = @"
                /**comment*/
                class A{
                    int x;
                    int y=0;
                    int f(){};
                };
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.Single();
            classInfo.SourceCode.Text.Should().Be(@"
                /**comment*/
                class A{
            ".TrimIndent().Trim());
        }
        
        [Fact]
        void ClassHeaderDoesNotIncludePeerFunction()
        {
            string source = @"
                int f(){};
                /**comment*/
                class A{
                    int x;
                    int y=0;
                    int f(){};
                };
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.Single().InnerClasses.Single();
            classInfo.SourceCode.Text.Should().Be(@"
                /**comment*/
                class A{
            ".TrimIndent().Trim());
        }
        
        [Fact]
        void ClassHeaderDoesNotIncludePeerClass()
        {
            string source = @"
                class A{};
                /**comment*/
                class B{
                    int x;
                    int y=0;
                    int f(){};
                };
            ".TrimIndent();
            IEnumerable<ClassInfo> classInfos = AntlrParseCpp.OuterClassInfosFromSource(source, "path");

            ClassInfo classInfo = classInfos.ToArray()[1];
            classInfo.Name.ShortName.Should().Be("B");
            classInfo.SourceCode.Text.Should().Be(@"
                /**comment*/
                class B{
            ".TrimIndent().Trim());
        }
        
    }
}