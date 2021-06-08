using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using antlr_parser.Antlr4Impl;
using antlr_parser.Antlr4Impl.C;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using Xunit;

namespace antlr_parser.tests.C
{
    public class AntlrParseCTest
    {
        [Fact]
        void ParseFunctions()
        {
            string source = @"
                int addNumbers(int a, int b)  
                {
                    int result;
                    result = a+b;
                    return result;
                }"
                .TrimIndent();

            IEnumerable<ClassInfo> classInfos = AntlrParseC.OuterClassInfosFromSource(source, "file/path");

            MethodInfo method = classInfos.Single().Methods.Single();
            method.Name.ShortName.Should().Be("addNumbers");
            method.SourceCode.Text.Should().Be(source.TrimStart('\n'));
        }

        [Fact]
        void ParseStructsAsClasses()
        {
            string source = @"
                struct structureName 
                {
                    dataType member1;
                };".TrimIndent();
            IEnumerable<ClassInfo> classInfos =
                AntlrParseC.OuterClassInfosFromSource(source, "file/path").ToImmutableList();

            classInfos.Single().InnerClasses.Count().Should().Be(1);
            ClassInfo classInfo = classInfos.Single().InnerClasses.Single();
            classInfo.className.ShortName.Should().Be("structureName");
            classInfo.Fields.First().FieldName.ShortName.Should().Be("member1");
        }
        [Fact]
        void ParseStructArrayField()
        {
            string source = @"
                struct structureName 
                {
                    dataType member1[10];
                };".TrimIndent();
            IEnumerable<ClassInfo> classInfos =
                AntlrParseC.OuterClassInfosFromSource(source, "file/path").ToImmutableList();

            classInfos.Single().InnerClasses.Count().Should().Be(1);
            ClassInfo classInfo = classInfos.Single().InnerClasses.Single();
            classInfo.className.ShortName.Should().Be("structureName");
            classInfo.Fields.First().FieldName.ShortName.Should().Be("member1");
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
                };".TrimIndent();
            IEnumerable<ClassInfo> classInfos =
                AntlrParseC.OuterClassInfosFromSource(source, "file/path").ToImmutableList();

            classInfos.Single().InnerClasses.Count().Should().Be(1);
            ClassInfo employeeClass = classInfos.Single().InnerClasses.Single();
            employeeClass.className.ShortName.Should().Be("Employee");
            employeeClass.Fields.Single().FieldName.ShortName.Should().Be("doj");
            employeeClass.InnerClasses.Count().Should().Be(1);
            ClassInfo dateClass = employeeClass.InnerClasses.First();
            dateClass.className.ShortName.Should().Be("Date");
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
            ".TrimIndent(); 
            IEnumerable<ClassInfo> classInfos = AntlrParseC.OuterClassInfosFromSource(source, "file/path").ToImmutableList();
            classInfos.Count().Should().Be(1);
        }

        [Fact]
        void parseTwoFunctions()
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
            ".TrimIndent();
            
            IEnumerable<ClassInfo> classInfos = AntlrParseC.OuterClassInfosFromSource(source, "file/path").ToImmutableList();

            IEnumerable<MethodInfo> methodInfos = classInfos.First().Methods;

           // methodInfos.Count().Should().Be(2);
            MethodInfo vaweMethod = methodInfos.Single(it=> it.Name.ShortName == "AudioDecodeWave");
            methodInfos.Single(it => it.Name.ShortName == "AudioDecodeMp3");
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
            ".TrimIndent();
            
            IEnumerable<ClassInfo> classInfos = AntlrParseC.OuterClassInfosFromSource(source, "file/path").ToImmutableList();

            IEnumerable<MethodInfo> methodInfos = classInfos.First().Methods;
            MethodInfo fMethod = methodInfos.SingleOrDefault(it => it.Name.ShortName == "f");
            fMethod.Should().NotBeNull();
            fMethod.SourceCode.Text.Trim().Should().Be(@"
                int f(int a, int b)
                {
                    for(i = 0; i<0; i++){
                    };
                }".TrimIndent().Trim());
            methodInfos.SingleOrDefault(it => it.Name.ShortName == "g").Should().NotBeNull();
        }
        
        [Fact]
        void ParseReferenceFieldNames()
        {
            string source = @"
                struct A {
                    const struct b *c;
                };
            ".TrimIndent();
            
            IEnumerable<ClassInfo> classInfos = AntlrParseC.OuterClassInfosFromSource(source, "file/path").ToImmutableList();

            classInfos.First().InnerClasses.First().Fields.First().Name.ShortName.Should().Be("c");
        }
    }
}
