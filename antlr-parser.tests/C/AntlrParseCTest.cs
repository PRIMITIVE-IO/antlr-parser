using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using antlr_parser.Antlr4Impl;
using antlr_parser.Antlr4Impl.C;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using Xunit;

namespace antlr_parser.tests.C
{
    public class AntlrParseCTest
    {
        public AntlrParseCTest()
        {
        }

        [Fact]
        void ParseFunctions()
        {
            string source = @"
                int addNumbers(int a, int b)  
                {
                    int result;
                    result = a+b;
                    return result;
                }
            ".TrimIndent2();

            FileDto fileDto = AntlrParseC.Parse(source, "file/path");

            MethodDto method = fileDto.Classes[0].Methods[0];
            method.Name.Should().Be("addNumbers");
            method.CodeRange.Of(source).Should().Be(source);
        }

        [Fact]
        void ParseStructsAsClasses()
        {
            string source = @"
                struct structureName 
                {
                    dataType member1;
                };".Unindent();
            FileDto fileDto = AntlrParseC.Parse(source, "file/path");

            fileDto.Classes.Should().HaveCount(1);
            ClassDto classInfo = fileDto.Classes[0];
            classInfo.Name.Should().Be("structureName");
            classInfo.Fields.First().Name.Should().Be("member1");
        }

        [Fact]
        void ParseStructArrayField()
        {
            string source = @"
                struct structureName 
                {
                    dataType member1[10];
                };".Unindent();
            FileDto fileDto = AntlrParseC.Parse(source, "file/path");

            fileDto.Classes.Should().HaveCount(1);
            ClassDto classInfo = fileDto.Classes[0];
            classInfo.Name.Should().Be("structureName");
            classInfo.Fields.First().Name.Should().Be("member1");
        }

        [Fact]
        void ParseNestedStructs()
        {
            string source = @"
                struct Employee  
                {     
                   struct Date  
                    {  
                      int dd;  
                    }doj;  
                };".Unindent();
            FileDto fileDto = AntlrParseC.Parse(source, "file/path");

            fileDto.Classes.Should().HaveCount(2);
            ClassDto employeeClass = fileDto.Classes[0];
            employeeClass.Name.Should().Be("Employee");
            employeeClass.Fields.Single().Name.Should().Be("doj");
            ClassDto dateClass = fileDto.Classes[1];
            dateClass.Name.Should().Be("Date");
        }

        //TODO [Fact]
        public void ClassNameWithoutNamespace()
        {
            string source = @"
                struct Employee  
                {     
                    int x;
                };".Unindent();
            FileDto fileDto = AntlrParseC.Parse(source, "file/path");
            fileDto.Classes[0].FullyQualifiedName.Should().Be("Employee");
        }

        [Fact]
        void ParseEmptyFile()
        {
            string source = @"
                /**
                  64-bit Mach-O library functions layer.

                Copyright (C) 2020, Goldfish64.  All rights reserved.<BR>
                This program and the accompanying materials are licensed and made available
                under the terms and conditions of the BSD License which accompanies this
                distribution.  The full text of the license may be found at
                http://opensource.org/licenses/bsd-license.php.

                THE PROGRAM IS DISTRIBUTED UNDER THE BSD LICENSE ON AN ""AS IS"" BASIS,
                            WITHOUT WARRANTIES OR REPRESENTATIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED.

                            **/

                #include ""CxxSymbolsX.h""
                #include ""HeaderX.h""
                #include ""SymbolsX.h""
            ".Unindent();
            FileDto fileDto = AntlrParseC.Parse(source, "file/path");
            fileDto.Classes.Should().HaveCount(0);
        }

        [Fact]
        void ParseTwoFunctions()
        {
            string source = @"
                /**
                  Decode WAVE audio to PCM audio.

                  @param[in]  This           Audio decode protocol instance.
                **/
                STATIC
                EFI_STATUS
                EFIAPI
                AudioDecodeWave (
                  IN  EFI_AUDIO_DECODE_PROTOCOL      *This,
                  IN  CONST VOID                     *InBuffer
                  )
                {
                  if (EFI_ERROR (Status)) {
                    return Status;
                  }
                }

                /**
                  Decode MP3 audio to PCM audio.

                  @param[in]  This           Audio decode protocol instance.
                **/
                STATIC
                EFI_STATUS
                EFIAPI
                AudioDecodeMp3 (
                  IN  EFI_AUDIO_DECODE_PROTOCOL      *This,
                  OUT UINT8                          *Channels
                  )
                {
                  Status = OcDecodeMp3 (
                    InBuffer,
                    Channels
                  );
                }
            ".Unindent();

            FileDto fileDto = AntlrParseC.Parse(source, "file/path");

            List<MethodDto> methodInfos = fileDto.Classes[0].Methods;

            // methodInfos.Count().Should().Be(2);
            MethodDto vaweMethod = methodInfos.Single(it => it.Name == "AudioDecodeWave");
            methodInfos.Single(it => it.Name == "AudioDecodeMp3");
        }

        [Fact]
        void ParseCodeWithDirectives()
        {
            string source = @"
                int f(int a, int b)
                {
                #if 1
                    for(i = 0; i<0; i++){
                #else
                    for(j = 0; j<0; j++){
                #endif
                    };
                }
                int g(int a, int b)  
                {
     
                }
            ".TrimIndent2();

            FileDto fileDto = AntlrParseC.Parse(source, "file/path");

            List<MethodDto> methodInfos = fileDto.Classes[0].Methods;
            MethodDto fMethod = methodInfos.Single(it => it.Name == "f");
            fMethod.Should().NotBeNull();

            fMethod.CodeRange.Of(source).Should().Be(@"
                |int f(int a, int b)
                |{
                |#if 1
                |    for(i = 0; i<0; i++){
                |#else
                |    for(j = 0; j<0; j++){
                |#endif
                |    };
                |}
                |
            ".TrimMargin());

            methodInfos.SingleOrDefault(it => it.Name == "g").Should().NotBeNull();
        }

        [Fact]
        void ParseReferenceFieldNames()
        {
            string source = @"
                struct A {
                    const struct b *c;
                };
            ".Unindent();

            FileDto fileDto = AntlrParseC.Parse(source, "file/path");

            fileDto.Classes.First().Fields.First().Name.Should().Be("c");
        }

        [Fact]
        void ParseMultiFieldNames()
        {
            string source = @"
                struct A {
                   struct b c,d;
                };
            ".Unindent();

            FileDto fileDto = AntlrParseC.Parse(source, "file/path");

            fileDto.Classes.First().Fields.Single().Name.Should().Be("c,d");
        }

        [Fact]
        void FirstClassHeader()
        {
            string source = @"
                #include ""something""
                /**comment*/
                struct A {
                   int a;
                };
            ".TrimIndent2();

            FileDto fileDto = AntlrParseC.Parse(source, "file/path");

            fileDto.Classes.Single().CodeRange.Of(source).Should().Be(@"
                |#include ""something""
                |/**comment*/
                |struct A {
                |   
            ".TrimMargin());
        }

        [Fact]
        void SecondClassHeader()
        {
            string source = @"
                #include ""something""
                struct A {
                   int a ;
                };

                /**comment2*/
                struct B {
                    int c;
                };
            ".TrimIndent2();

            FileDto fileDto = AntlrParseC.Parse(source, "file/path");

            fileDto.Classes.Single(x => x.Name == "B").CodeRange.Of(source).Should().Be(@"
                |
                |
                |/**comment2*/
                |struct B {
                |    
            ".TrimMargin());
        }
    }
}