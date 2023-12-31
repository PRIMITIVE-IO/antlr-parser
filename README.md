This is a C# implementation of an Antlr parser that can be used to parse source code into an abstract syntax tree (AST).  
Each AST is held in a FileNode for each file, with other nodes nested inside of each FileNode.  

TO RUN  
-Clone this repository  
-Open with Visual Studio  
-Build and run the project with a single argument that points to a file or folder containing files that are supported by [PrimitiveAntlrParser.cs](antlr-parser/PrimitiveAntlrParser.cs)  
`e.g.: C:/path/to/file.java`  
-The program will print the structure of the source code contained in the .java file  

TO CREATE AN IMPLEMENTATION FOR A LANGUAGE  
-Install the Antlr4 plugin https://www.antlr.org/tools.html  
-Generate a Lexer and a Parser from a Grammar file (.g4)  
  - Find a grammar file for the desired language at https://github.com/antlr/grammars-v4  
  - Right click a `<LanguageName>_Lexer.g4` file and go to Tools > Configure ANTLR...    
  - Set the language for the _source files_ that will be generated (in this case "CSharp" - _not the language that is actually getting parsed_)  
  - Right click the `<LanguageName>_Lexer.g4` again and run Tools > Generate ANTLR Recognizer
  - Repeat for a `<LanguageName>_Parser.g4`  
  
-Manually fix the generated files if necessary    
  - Include a BaseParser.cs file from https://github.com/antlr/grammars-v4 if required. It will be found in a "CSharp" folder inside of the desired language folder.
  - Replace token names in the generated Lexer file with their numeric equivalents (e.g. LPAREN with 73 - found in the .tokens file) if necessary.  
  - Certain function names like "Lt" might use a different runtime version name "LT"
  - The Parser constructor may have too many arguments outside of the InputStream parameter - remove them.    
  - The _input ITokenStream may be protected. The InputStream parameter from the constructor can be saved locally.  
    
-Create visitors for each type of node in the language's abstract syntax tree  
  - The visitor classes instantiate a Lexer and a Parser to parse a string of code.  
  - The first syntax type to visit can be found in the language's parser `<LanguageName>.g4` file. It will be one of the only syntax types that is NOT contained by any other type.  An Example of `compilationUnit` as a top level object for Java:  
 ```antlrv4
parser grammar JavaParser;

options { tokenVocab=JavaLexer; }

compilationUnit
: packageDeclaration? importDeclaration* typeDeclaration* EOF
;
```
  - When parsing this top level syntax type, the node can only be accessed once in code. Otherwise the AST will advance to the next node and all references will be lost. An example:  
```c#
                // a compilation unit is the highest level container -> start there
                // do not call parser.compilationUnit() more than once
                return parser.compilationUnit().Accept(
                    new JavaAstVisitor(methodBodyRemovalResult, filePath, codeRangeCalculator)
                ) as AstNode.FileNode;
```    
  - A full example is provided in Java.  
  
Note: MethodNodes and FieldNodes are the most granular nodes that can be visited. For faster parsing, the bodies of methods are removed before the source code is parsed.  

REFERENCE  
-https://tomassetti.me/antlr-mega-tutorial  
-https://github.com/antlr/antlr4  
-https://www.antlr.org/tools.html  