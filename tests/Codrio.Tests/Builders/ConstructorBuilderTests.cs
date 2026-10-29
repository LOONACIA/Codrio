using System;
using Xunit;

namespace Codrio.Tests;

public class ConstructorBuilderTests
{
    [Fact]
    public void ToString_OmitsUnknownTypeNameAndAlwaysRendersBlock()
    {
        var builder = CSharp.Constructor();

        OutputAssert.Equal("()\n{\n}\n", builder.ToString());
        OutputAssert.Equal("()\n{\n}\n", builder.ToString());
        Assert.NotSame(builder, CSharp.Constructor());
        OutputAssert.Equal("public (int value)\n{\n}\n",
            CSharp.Constructor(k => k.Public().Parameter("int", "value")).ToString());
    }

    [Fact]
    public void Constructor_InvokesConfigureImmediatelyOnceOnReturnedBuilder()
    {
        ConstructorBuilder? configured = null;
        var calls = 0;
        var builder = CSharp.Constructor(k =>
        {
            configured = k;
            calls++;
            k.Public();
        });

        Assert.Same(builder, configured);
        Assert.Equal(1, calls);
        OutputAssert.Equal("public ()\n{\n}\n", builder.ToString());
        OutputAssert.Equal("public ()\n{\n}\n", builder.ToString());
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void TypeConstructors_UseContainingNameKeepReferencesAndReturnParent(string keyword)
    {
        switch (keyword)
        {
            case "class": AssertTypeConstructors(CSharp.Class("Outer"), keyword); break;
            case "struct": AssertTypeConstructors(CSharp.Struct("Outer"), keyword); break;
            case "record": AssertTypeConstructors(CSharp.Record("Outer"), keyword); break;
            case "record struct": AssertTypeConstructors(CSharp.RecordStruct("Outer"), keyword); break;
        }
    }

    private static void AssertTypeConstructors<TSelf>(ClassOrStructBuilder<TSelf> builder, string keyword)
        where TSelf : ClassOrStructBuilder<TSelf>
    {
        ConstructorBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Constructor().Parameter("string", "name");

        Assert.Same(builder, builder.Constructor());
        Assert.Same(builder, builder.Constructor(k =>
        {
            configured = k;
            calls++;
            k.Public().Parameter("int", "value");
        }));
        Assert.Same(builder, builder.Add(added));
        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        OutputAssert.Equal(keyword + " Outer\n{\n    Outer()\n    {\n    }\n\n" +
            "    public Outer(int value)\n    {\n    }\n\n    Outer(string name)\n    {\n    }\n}\n", builder.ToString());
        OutputAssert.Equal("public (int value)\n{\n}\n", configured.ToString());

        configured.Private();
        added.Public().Body(b => b.Line("Use(name);"));
        var expected = keyword + " Outer\n{\n    Outer()\n    {\n    }\n\n" +
            "    private Outer(int value)\n    {\n    }\n\n    public Outer(string name)\n    {\n        Use(name);\n    }\n}\n";
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("Box", "Box")]
    [InlineData("class", "@class")]
    [InlineData("@class", "@class")]
    public void GenericAndKeywordTypes_UseEscapedNameWithoutTypeParameters(string name, string escapedName)
    {
        var builder = CSharp.Class(name).TypeParameter("T")
            .Constructor(k => k.Public().Parameter("T", "value"))
            .Add(CSharp.Constructor().Private());

        OutputAssert.Equal("class " + escapedName + "<T>\n{\n    public " + escapedName +
            "(T value)\n    {\n    }\n\n    private " + escapedName + "()\n    {\n    }\n}\n", builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Fact]
    public void SharedConstructor_UsesEachContainingTypeNameWithoutChangingStandaloneOutput()
    {
        var constructor = CSharp.Constructor().Public();
        var first = CSharp.Class("First").Add(constructor);
        var second = CSharp.Record("Second").Add(constructor);

        constructor.Parameter("int", "value");
        OutputAssert.Equal("class First\n{\n    public First(int value)\n    {\n    }\n}\n", first.ToString());
        OutputAssert.Equal("record Second\n{\n    public Second(int value)\n    {\n    }\n}\n", second.ToString());
        OutputAssert.Equal("public (int value)\n{\n}\n", constructor.ToString());
        OutputAssert.Equal("class First\n{\n    public First(int value)\n    {\n    }\n}\n", first.ToString());
    }

    [Fact]
    public void CacheExample_RendersStaticAndInstanceConstructorsAndCompiles()
    {
        var builder = CSharp.Class("Cache", c => c.Public()
            .Field("int", "size", f => f.Private().ReadOnly())
            .Constructor(k => k.Static())
            .Constructor(k => k.Public().Parameter("int", "size").Body(b => b.Line("this.size = size;"))));

        OutputAssert.Equal("public class Cache\n{\n    private readonly int size;\n\n" +
            "    static Cache()\n    {\n    }\n\n    public Cache(int size)\n    {\n        this.size = size;\n    }\n}\n",
            builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Fact]
    public void Modifiers_RenderInFixedOrderWithoutValidatingCombinations()
    {
        var builder = CSharp.Constructor().ReadOnly().Override().Sealed().Abstract()
            .Virtual().New().Static().Private().Public().Static();

        OutputAssert.Equal("public static new virtual abstract sealed override readonly ()\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void Parameters_PreserveOrderModifiersDefaultsAndEscaping()
    {
        ParameterBuilder? configured = null;
        var calls = 0;
        var builder = CSharp.Constructor().Parameter("int", "first", p => p.Ref())
            .Parameter("string", "class", p => p.Out())
            .Parameter("int", "third", p => p.In())
            .Parameter("int", "optional", p =>
            {
                configured = p;
                calls++;
                p.Default("1");
            })
            .Parameter("int[]", "rest", p => p.Params())
            .Parameter("unvalidated type", "last", p => p.This());

        Assert.Equal(1, calls);
        Assert.NotNull(configured);
        configured.Default("2");
        const string expected = "(ref int first, out string @class, in int third, int optional = 2, params int[] rest, this unvalidated type last)\n{\n}\n";
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Body_InvokesConfigureImmediatelyOnceKeepsReferenceAndLastCallWins()
    {
        BodyBuilder? configured = null;
        var calls = 0;
        var builder = CSharp.Constructor().Body(b => b.Line("First();"));
        Assert.Same(builder, builder.Body(b =>
        {
            configured = b;
            calls++;
            b.Line("Second();");
        }));

        Assert.Equal(1, calls);
        Assert.NotNull(configured);
        configured.Line("Third();");
        OutputAssert.Equal("()\n{\n    Second();\n    Third();\n}\n", builder.ToString());
        OutputAssert.Equal("()\n{\n    Second();\n    Third();\n}\n", builder.ToString());
        Assert.Equal(1, calls);
        Assert.Same(builder, builder.Body());
        OutputAssert.Equal("()\n{\n}\n", builder.ToString());
        builder.Body(b => b.Line("Last();"));
        OutputAssert.Equal("()\n{\n    Last();\n}\n", builder.ToString());
    }

    [Fact]
    public void MixedItems_PreserveOrderBlankLinesAndNestedConstructorIndentation()
    {
        var builder = CSharp.Class("Outer").Field("int", "first")
            .Constructor().Method("Run", m => m.Body()).Raw("// marker")
            .Class("Nested", c => c.Add(CSharp.Constructor().Body(b => b.Line("Run();"))))
            .Add(CSharp.Constructor().Parameter("int", "last"));

        OutputAssert.Equal("class Outer\n{\n    int first;\n\n    Outer()\n    {\n    }\n\n" +
            "    void Run()\n    {\n    }\n\n    // marker\n\n    class Nested\n    {\n" +
            "        Nested()\n        {\n            Run();\n        }\n    }\n\n" +
            "    Outer(int last)\n    {\n    }\n}\n", builder.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("1value")]
    [InlineData("value.Name")]
    [InlineData("value name")]
    [InlineData("value-name")]
    [InlineData("@@value")]
    public void InvalidParameterNames_ThrowAtCallTimeWithoutConfiguringOrAdding(string name)
    {
        var builder = CSharp.Constructor();
        var configured = false;

        Assert.Throws<ArgumentException>("name", () => builder.Parameter("int", name));
        Assert.Throws<ArgumentException>("name", () => builder.Parameter("int", name, _ => configured = true));
        Assert.False(configured);
        OutputAssert.Equal("()\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void NullArguments_ThrowWithParameterNameWithoutChangingState()
    {
        var builder = CSharp.Constructor().Body(b => b.Line("Keep();"));
        var configured = false;

        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Constructor(null!));
        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter(null!, "value"));
        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter(null!, "value", _ => configured = true));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter("int", null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter("int", null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Parameter("int", "value", null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Parameter(null!, null!, null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Body(null!));
        Assert.False(configured);
        OutputAssert.Equal("()\n{\n    Keep();\n}\n", builder.ToString());

        AssertNullArguments(CSharp.Class("Outer"));
        AssertNullArguments(CSharp.Struct("Outer"));
        AssertNullArguments(CSharp.Record("Outer"));
        AssertNullArguments(CSharp.RecordStruct("Outer"));
    }

    private static void AssertNullArguments<TSelf>(ClassOrStructBuilder<TSelf> builder)
        where TSelf : ClassOrStructBuilder<TSelf>
    {
        var before = builder.ToString();
        Assert.Throws<ArgumentNullException>("configure", () => builder.Constructor(null!));
        Assert.Throws<ArgumentNullException>("constructorBuilder", () => builder.Add((ConstructorBuilder)null!));
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void FluentMethods_ReturnSameConstructorBuilder()
    {
        var builder = CSharp.Constructor();

        Assert.Same(builder, builder.Public());
        Assert.Same(builder, builder.Internal());
        Assert.Same(builder, builder.Protected());
        Assert.Same(builder, builder.Private());
        Assert.Same(builder, builder.ProtectedInternal());
        Assert.Same(builder, builder.PrivateProtected());
        Assert.Same(builder, builder.Static());
        Assert.Same(builder, builder.New());
        Assert.Same(builder, builder.Virtual());
        Assert.Same(builder, builder.Abstract());
        Assert.Same(builder, builder.Sealed());
        Assert.Same(builder, builder.Override());
        Assert.Same(builder, builder.ReadOnly());
        Assert.Same(builder, builder.Parameter("int", "first"));
        Assert.Same(builder, builder.Parameter("int", "second", p => p.Default("0")));
        Assert.Same(builder, builder.Body());
        Assert.Same(builder, builder.Body(b => b.Line("Run();")));
    }

    [Theory]
    [InlineData(typeof(InterfaceBuilder))]
    [InlineData(typeof(EnumBuilder))]
    [InlineData(typeof(FileBuilder))]
    [InlineData(typeof(NamespaceBuilder))]
    public void UnsupportedContainers_DoNotExposeConstructorEntryPoints(Type type)
    {
        Assert.DoesNotContain(type.GetMethods(), method => method.Name == "Constructor");
        Assert.DoesNotContain(type.GetMethods(), method => method.Name == "Add" &&
            Array.Exists(method.GetParameters(), parameter => parameter.ParameterType == typeof(ConstructorBuilder)));
    }

    [Fact]
    public void CompleteFile_RendersConstructorsAndCompiles()
    {
        var file = CSharp.File()
            .Class("Counter", c => c.Constructor(k => k.Static())
                .Constructor(k => k.Public()).Add(CSharp.Constructor().Public().Parameter("int", "value")))
            .Struct("Point", s => s.Field("int", "x").Constructor(k => k.Public().Parameter("int", "x")
                .Body(b => b.Line("this.x = x;"))))
            .Class("Box", c => c.TypeParameter("T").Constructor(k => k.Public().Parameter("T", "value")))
            .Record("Entry", r => r.Constructor(k => k.Public().Parameter("int", "value")))
            .RecordStruct("Token", r => r.Add(CSharp.Constructor().Public().Parameter("int", "value")));

        const string expected = "class Counter\n{\n    static Counter()\n    {\n    }\n\n" +
            "    public Counter()\n    {\n    }\n\n    public Counter(int value)\n    {\n    }\n}\n\n" +
            "struct Point\n{\n    int x;\n\n    public Point(int x)\n    {\n        this.x = x;\n    }\n}\n\n" +
            "class Box<T>\n{\n    public Box(T value)\n    {\n    }\n}\n\n" +
            "record Entry\n{\n    public Entry(int value)\n    {\n    }\n}\n\n" +
            "record struct Token\n{\n    public Token(int value)\n    {\n    }\n}\n";
        OutputAssert.Equal(expected, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }
}
