using System.Collections.Generic;
using System.Linq;
using antlr_parser.Antlr4Impl;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using Xunit;

namespace antlr_parser.tests
{
    public class ParserHandlerTests
    {
        private readonly string testJavaClassSourceCode;

        public ParserHandlerTests()
        {
            testJavaClassSourceCode = System.IO.File.ReadAllText(@"Resources/TestJavaClass.java");
        }


        [Fact]
        public void ParserHandlerShouldReturnCollectionWithOneElement()
        {
            //Act
            IEnumerable<ClassInfo> result = ParserHandler.ClassInfoFromSourceText("test.java", ".java", testJavaClassSourceCode);

            //Verify
            result.Should().NotBeNull();
            result.Count().Should().Be(1);
        }

        [Fact]
        public void ParserHandlerShouldReturnCollectionWithAnyClassInfoWithClassName()
        {
            //Act
            IEnumerable<ClassInfo> result = ParserHandler.ClassInfoFromSourceText("test.java", ".java", testJavaClassSourceCode);

            //Verify
            result.ToList().First().Children.Any(x => x.Name.ShortName == "doWork").Should().BeTrue();
        }   
        
        [Fact]
        public void KotlinClassHasHeader()
        {
            //string source = System.IO.File.ReadAllText(@"Resources/KotlinExample.kt");
            //Act
            string source = @"
                package pkg
                
                /** comment */
                class C {
                  fun method() {
                    println()
                  }
                }
                
                fun outerFunction() { }

            ".TrimIndent();
            
            IEnumerable<ClassInfo> result = ParserHandler.ClassInfoFromSourceText("kotlin.kt", ".kt", source);

            //Verify
            ClassInfo fileInfo = result.ToList().First();
            fileInfo.SourceCode.Text.Should().Be(
                @"package pkg

                  /** comment */".TrimIndent());

            ClassInfo classInfo = fileInfo.InnerClasses.Single();
            classInfo.Name.ShortName.Should().Be("C");
            classInfo.SourceCode.Text.Should().Be(@"
                package pkg
                
                /** comment */
                class C {
            ".TrimIndent().Trim());

            classInfo.Methods.Single().SourceCode.Text.Should().Be("fun method() {\n  println()\n}");
        }
    }
}
