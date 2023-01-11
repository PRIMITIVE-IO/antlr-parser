using antlr_parser.Antlr4Impl.Python;
using FluentAssertions;
using PrimitiveCodebaseElements.Primitive;
using PrimitiveCodebaseElements.Primitive.dto;
using Xunit;

namespace antlr_parser.tests.Python;

public class AntlrParsePython3Test
{
    [Fact]
    public void SmokeTest()
    {
        string source = @"
                class MyClass:
                    """"""A simple example class""""""
                    i = 12345
                
                    def f(self):
                        """"""Method comment""""""
                        return 'hello world'
            ".Unindent();

        FileDto fileDto = AntlrParsePython3.Parse(source, "some/path");

        fileDto.Path.Should().Be("some/path");
        fileDto.Classes.Should().HaveCount(1);
        ClassDto classDto = fileDto.Classes[0];
        classDto.Name.Should().Be("MyClass");
        string expectedClassHeader = "class MyClass:\n    \"\"\"A simple example class\"\"\"";
        classDto.CodeRange.Of(source).Should().Be(expectedClassHeader.PlatformSpecific());

        classDto.Fields.Should().HaveCount(1);
        FieldDto fieldDto = classDto.Fields[0];
        fieldDto.Name.Should().Be("i");
        fieldDto.CodeRange.Of(source).Should().Be("i = 12345");

        classDto.Methods.Should().HaveCount(1);
        MethodDto methodDto = classDto.Methods[0];
        methodDto.Name.Should().Be("f");
        methodDto.CodeRange.Of(source).Should()
            .Be("def f(self):\n        \"\"\"Method comment\"\"\"\n        return 'hello world'".PlatformSpecific());
    }

    [Fact]
    public void ParseMethod()
    {
        string source = @"
                class MyClass:
                    """"""A simple example class""""""
                    
                    i = 12345
                
                    def f(self):
                        """"""Method F comment""""""
                        return 'hello F'
                    
                    def g(self):
                        """"""Method G comment""""""
                        return 'hello G'
            ".Unindent();

        FileDto fileDto = AntlrParsePython3.Parse(source, "some/path");

        ClassDto classDto = fileDto.Classes[0];


        classDto.Methods.Should().HaveCount(2);
        MethodDto fMethodDto = classDto.Methods[0];
        fMethodDto.Name.Should().Be("f");
        fMethodDto.CodeRange.Of(source).Should()
            .Be("def f(self):\n        \"\"\"Method F comment\"\"\"\n        return 'hello F'".PlatformSpecific());

        MethodDto gMethodDto = classDto.Methods[1];
        gMethodDto.Name.Should().Be("g");
        gMethodDto.CodeRange.Of(source).Should()
            .Be("def g(self):\n        \"\"\"Method G comment\"\"\"\n        return 'hello G'".PlatformSpecific());
    }

    [Fact]
    public void ParseMethodWithoutComments()
    {
        string source = @"
                class MyClass:
                    """"""A simple example class""""""
                    
                    i = 12345
                
                    def f(self):
                        return 'hello F'
                    
                    def g(self):
                        return 'hello G'
            ".Unindent();

        FileDto fileDto = AntlrParsePython3.Parse(source, "some/path");

        ClassDto classDto = fileDto.Classes[0];


        classDto.Methods.Should().HaveCount(2);
        MethodDto fMethodDto = classDto.Methods[0];
        fMethodDto.Name.Should().Be("f");
        fMethodDto.CodeRange.Of(source).Should()
            .Be("def f(self):\n        return 'hello F'".PlatformSpecific());

        MethodDto gMethodDto = classDto.Methods[1];
        gMethodDto.Name.Should().Be("g");
        gMethodDto.CodeRange.Of(source).Should()
            .Be("def g(self):\n        return 'hello G'".PlatformSpecific());
    }

    [Fact]
    public void CreateFakeClass()
    {
        string source = @"
            def f():
                """"""Function F comment""""""
                return 'hello F'
            ".Unindent();

        FileDto fileDto = AntlrParsePython3.Parse(source, "some/path");

        ClassDto classDto = fileDto.Classes[0];


        classDto.Methods.Should().HaveCount(1);
        MethodDto fMethodDto = classDto.Methods[0];
        fMethodDto.Name.Should().Be("f");
        fMethodDto.CodeRange.Of(source).Should()
            .Be("def f():\n    \"\"\"Function F comment\"\"\"\n    return 'hello F'".PlatformSpecific());
    }

    [Fact]
    public void TrivialCarrierReturn()
    {
        string source = "def f():\r\n    \"\"\"Function F comment\"\"\"\r\n    return 'hello F'";

        FileDto fileDto = AntlrParsePython3.Parse(source, "some/path");

        MethodDto fMethodDto = fileDto.Classes[0].Methods[0];
        fMethodDto.CodeRange.Of(source).Should()
            .Be("def f():\r\n    \"\"\"Function F comment\"\"\"\r\n    return 'hello F'");
    }

    [Fact]
    public void ComplexCarrierReturn()
    {
        string source =
            "class MyClass:\r\n    \"\"\"A simple example class\"\"\"\r\n    i = 12345\r\n\r\n    def f(self):\r\n        \"\"\"Method comment\"\"\"\r\n        return 'hello world'\r\n";

        FileDto fileDto = AntlrParsePython3.Parse(source, "some/path");

        fileDto.Path.Should().Be("some/path");
        fileDto.Classes.Should().HaveCount(1);
        ClassDto classDto = fileDto.Classes[0];
        classDto.Name.Should().Be("MyClass");
        string expectedClassHeader = "class MyClass:\r\n    \"\"\"A simple example class\"\"\"";
        classDto.CodeRange.Of(source).Should().Be(expectedClassHeader);

        classDto.Fields.Should().HaveCount(1);
        FieldDto fieldDto = classDto.Fields[0];
        fieldDto.Name.Should().Be("i");
        fieldDto.CodeRange.Of(source).Should().Be("i = 12345");

        classDto.Methods.Should().HaveCount(1);
        MethodDto methodDto = classDto.Methods[0];
        methodDto.Name.Should().Be("f");
        methodDto.CodeRange.Of(source).Should()
            .Be("def f(self):\r\n        \"\"\"Method comment\"\"\"\r\n        return 'hello world'");
    }

    [Fact]
    public void ClassHeader()
    {
        string source = @"
                class MyClass:
                    def f(self):
                        """"""Method comment""""""
                        return 'hello world'
            ".TrimIndent2();

        FileDto fileDto = AntlrParsePython3.Parse(source, "some/path");

        fileDto.Classes[0].CodeRange.Of(source).Should().Be("class MyClass:");
    }


    [Fact]
    public void WierdCase2()
    {
        string source = @"
                import inspect
                import itertools as it


                def get_all_descendent_classes(Class):
                    awaiting_review = [Class]
                    result = []
                    while awaiting_review:
                        Child = awaiting_review.pop()
                        awaiting_review += Child.__subclasses__()
                        result.append(Child)
                    return result


                def filtered_locals(caller_locals):
                    result = caller_locals.copy()
                    ignored_local_args = [""self"", ""kwargs""]
                    for arg in ignored_local_args:
                        result.pop(arg, caller_locals)
                    return result


                def digest_config(obj, kwargs, caller_locals={}):
                    """"""
                    Sets init args and CONFIG values as local variables

                    The purpose of this function is to ensure that all
                    configuration of any object is inheritable, able to
                    be easily passed into instantiation, and is attached
                    as an attribute of the object.
                    """"""

                    # Assemble list of CONFIGs from all super classes
                    classes_in_hierarchy = [obj.__class__]
                    static_configs = []
                    while len(classes_in_hierarchy) > 0:
                        Class = classes_in_hierarchy.pop()
                        classes_in_hierarchy += Class.__bases__
                        if hasattr(Class, ""CONFIG""):
                            static_configs.append(Class.CONFIG)

                    # Order matters a lot here, first dicts have higher priority
                    caller_locals = filtered_locals(caller_locals)
                    all_dicts = [kwargs, caller_locals, obj.__dict__]
                    all_dicts += static_configs
                    obj.__dict__ = merge_dicts_recursively(*reversed(all_dicts))


                def merge_dicts_recursively(*dicts):
                    """"""
                    Creates a dict whose keyset is the union of all the
                    input dictionaries.  The value for each key is based
                    on the first dict in the list with that key.

                    dicts later in the list have higher priority

                    When values are dictionaries, it is applied recursively
                    """"""
                    result = dict()
                    all_items = it.chain(*[d.items() for d in dicts])
                    for key, value in all_items:
                        if key in result and isinstance(result[key], dict) and isinstance(value, dict):
                            result[key] = merge_dicts_recursively(result[key], value)
                        else:
                            result[key] = value
                    return result


                def soft_dict_update(d1, d2):
                    """"""
                    Adds key values pairs of d2 to d1 only when d1 doesn't
                    already have that key
                    """"""
                    for key, value in list(d2.items()):
                        if key not in d1:
                            d1[key] = value


                def digest_locals(obj, keys=None):
                    caller_locals = filtered_locals(
                        inspect.currentframe().f_back.f_locals
                    )
                    if keys is None:
                        keys = list(caller_locals.keys())
                    for key in keys:
                        setattr(obj, key, caller_locals[key])

                # Occasionally convenient in order to write dict.x instead of more laborious
                # (and less in keeping with all other attr accesses) dict[""x""]


                class DictAsObject(object):
                    def __init__(self, dict):
                        self.__dict__ = dict
            ".TrimIndent2();

        FileDto fileDto = AntlrParsePython3.Parse(source, "some/path");

        fileDto.Classes.Should().HaveCount(2);
        fileDto.Classes[0].Methods.Should().HaveCount(6);
        fileDto.Classes[0].Methods[5].CodeRange.Of(source).Should().Be(@"
                def digest_locals(obj, keys=None):
                    caller_locals = filtered_locals(
                        inspect.currentframe().f_back.f_locals
                    )
                    if keys is None:
                        keys = list(caller_locals.keys())
                    for key in keys:
                        setattr(obj, key, caller_locals[key])

                # Occasionally convenient in order to write dict.x instead of more laborious
                # (and less in keeping with all other attr accesses) dict[""x""]
            ".TrimIndent2());
    }

    [Fact]
    public void InvalidNewLineSeparators()
    {
        string source = TestUtils.Resource("invalid_new_line_separators.py");
        FileDto fileDto = AntlrParsePython3.Parse(source, "some/path");

        fileDto.Classes.Should().HaveCount(1);
        fileDto.Classes[0].Methods.Should().HaveCount(1);
        fileDto.Classes[0].Methods[0].CodeRange.Should().Be(CodeRange.Of(10, 1, 18, 25));
    }
}