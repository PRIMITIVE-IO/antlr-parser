using System;
using System.Collections.Generic;
using System.Linq;
using antlr_parser.Antlr4Impl;
using antlr_parser.Antlr4Impl.CPP;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
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
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.First();
            classInfo.Name.Should().Be("path");
            MethodDto method = classInfo.Methods.Single();

            method.Name.Should().Be("f");
            method.SourceCode.Should().Be(@"
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
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.First();
            classInfo.Name.Should().Be("path");
            MethodDto method = classInfo.Methods.Single();

            method.Name.Should().Be("f");
            method.SourceCode.Should().Be(@"
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
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.First();
            classInfo.Name.Should().Be("path");
            MethodDto method = classInfo.Methods.Single();

            method.Name.Should().Be("f");
            method.SourceCode.Should().Be(@"
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
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.First();
            classInfo.Name.Should().Be("A");
            MethodDto method = classInfo.Methods.Single();

            method.Name.Should().Be("f");
            method.SourceCode.Should().Be(@"
                int f(int a, int b) {
                   return 10; 
                }
            ".TrimIndent().Trim());

            FieldDto field = classInfo.Fields.Single();
            field.Name.Should().Be("x");
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
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.First();
            classInfo.Name.Should().Be("A");

            ClassDto nestedClass = classInfos.Classes[1];
            nestedClass.Name.Should().Be("B");
            ClassDto nestedClassWithForwardDeclaration = classInfos.Classes[2];
            nestedClassWithForwardDeclaration.Name.Should().Be("C");
        }

        [Fact]
        void ParseClassWithForwardDeclaration()
        {
            string source = @"
                class A;
            ".TrimIndent();
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.First();
            classInfo.Name.Should().Be("A");
        }

        [Fact]
        void ParseClassWithForwardDeclarationField()
        {
            string source = @"
                class A {
                    int x;
                };
            ".TrimIndent();
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.First();
            classInfo.Name.Should().Be("A");
            classInfo.Fields.Single().Name.Should().Be("x");
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
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.First();
            classInfo.Name.Should().Be("A");
            classInfo.Fields.ToArray()[0].Name.Should().Be("x");
            classInfo.Fields.ToArray()[1].Name.Should().Be("y");
            classInfo.Methods.ToArray()[0].Name.Should().Be("f");
            classInfo.Methods.ToArray()[1].Name.Should().Be("h");
        }

        [Fact]
        void ParseClassWithForwardDeclarationMethod()
        {
            string source = @"
                class A {
                    double f();
                };
            ".TrimIndent();
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.First();
            classInfo.Name.Should().Be("A");
            classInfo.Methods.Single().Name.Should().Be("f");
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
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.First();
            classInfo.Name.Should().Be("S");
            classInfo.Fields.Single().Name.Should().Be("x");
            classInfo.Methods.Single().Name.Should().Be("f");
        }

        [Fact]
        void ParseTopLevelFields()
        {
            string source = @"
                    int x;
                    int y = 0;
                    static a b GUARDED_BY(c) = 0;
            ".TrimIndent();
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.First();
            classInfo.Fields.ToArray()[0].Name.Should().Be("x");
            classInfo.Fields.ToArray()[1].Name.Should().Be("y");
            // classInfo.Fields.ToArray()[2].Name.Should().Be("b"); TODO GUARDED BY is a macro :(
        }

        [Fact]
        void FunctionCallWithDoubleColons()
        {
            string source = @"
                    T1::T1(const T2& x) : x(x)
                    {
                       
                    }
            ".TrimIndent();
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.First();
            classInfo.Methods.Single().Name.Should().Be("T1");
        }

        [Fact]
        void FunctionCallWithDoubleColonsAndType()
        {
            string source = @"
                bool T::operator()() const {
                 
                }
            ".TrimIndent();
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.First();
            classInfo.Methods.Single().Name.Should().Be("()");
        }

        [Fact]
        void ParseNamespace()
        {
            string source = @"
                namespace {
                    class A{};
                }
            ".TrimIndent();
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.Single();
            classInfo.Name.Should().Be("A");
        }

        [Fact]
        void ParseTemplate()
        {
            string source = @"
                template<typename T>
                CSHA512& operator<<() {
                    
                }
            ".TrimIndent();
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.Single();
            classInfo.Methods.Single().Name.Should().Be("<<");
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
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.Single();
            classInfo.Name.Should().Be("E");
            classInfo.Header.Should().Be(@"
                enum class E {
            ".TrimIndent().Trim());
        }

        [Fact]
        void ParseWierdFunctionName()
        {
            string source = @"
                Session::~Session(){}
            ".TrimIndent();
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.Single();
            classInfo.Methods.Single().Name.Should().Be("~Session");
        }

        [Fact]
        void ParseConversionOperator()
        {
            string source = @"
                CThreadInterrupt::operator bool() const
                {
                }
            ".TrimIndent();
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.Single();
            classInfo.Methods.Single().Name.Should().Be("bool");
        }     
        [Fact]
        void StaticConst()
        {
            string source = @"
                static const size_t MAX_GETUTXOS_OUTPOINTS = 15;
            ".TrimIndent();
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.Single();
            classInfo.Fields.Single().Name.Should().Be("MAX_GETUTXOS_OUTPOINTS");
        }

        //[Fact]TODO
        void HaveNoIdea()
        {
            string source = @"
                struct {
                    bool (*handler)(const std::any& context, HTTPRequest* req, const std::string& strReq);
                };
            ".TrimIndent();
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            throw new Exception();
        }

        //[Fact] TODO
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
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes[0];
            classInfo.Name.Should().Be("path");
            classInfo.Methods.ToArray()[0].Name.Should().Be("NewBloomFilterPolicy");
            classInfo.Methods.ToArray()[1].Name.Should().Be("BloomHash");
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
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.Single();
            classInfo.Header.Should().Be(@"
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
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes[1];
            classInfo.Header.Should().Be(@"
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
            FileDto classInfos = AntlrParseCpp.Parse(source, "path");

            ClassDto classInfo = classInfos.Classes.ToArray()[1];
            classInfo.Name.Should().Be("B");
            classInfo.Header.Should().Be(@"
                /**comment*/
                class B{
            ".TrimIndent().Trim());
        }
        
    }
}