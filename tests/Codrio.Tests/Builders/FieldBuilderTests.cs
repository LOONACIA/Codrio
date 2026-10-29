using System;
using Xunit;

namespace Codrio.Tests;

public class FieldBuilderTests
{
    [Theory]
    [InlineData("count", "int count;\n")]
    [InlineData("class", "int @class;\n")]
    [InlineData("@class", "int @class;\n")]
    public void ToString_RendersField(string name, string expected)
    {
        var builder = CSharp.Field("int", name);

        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
    }

    [Theory]
    [InlineData("int")]
    [InlineData("global::System.Collections.Generic.List<string>")]
    [InlineData("unvalidated type")]
    [InlineData("")]
    public void Field_WritesTypeVerbatim(string type)
    {
        OutputAssert.Equal(type + " value;\n", CSharp.Field(type, "value").ToString());
    }

    [Theory]
    [InlineData("42")]
    [InlineData("new int[] { 1, 2 }")]
    [InlineData("unvalidated expression")]
    [InlineData("")]
    public void Initializer_WritesExpressionVerbatimAndLastCallWins(string expression)
    {
        var builder = CSharp.Field("int", "count").Initializer("0").Initializer(expression);

        OutputAssert.Equal("int count = " + expression + ";\n", builder.ToString());
    }

    [Fact]
    public void Const_RendersImmediatelyBeforeType()
    {
        OutputAssert.Equal("const int X = 1;\n", CSharp.Field("int", "X").Const().Initializer("1").ToString());
        OutputAssert.Equal("public new const int X = 1;\n",
            CSharp.Field("int", "X").Const().New().Public().Initializer("1").ToString());
    }

    [Fact]
    public void Modifiers_RenderInFixedOrderOnceWithoutValidatingCombinations()
    {
        var builder = CSharp.Field("int", "X").Const().ReadOnly().Override().Sealed()
            .Abstract().Virtual().New().Static().Public().Initializer("1");
        const string expected = "public static new virtual abstract sealed override readonly const int X = 1;\n";

        OutputAssert.Equal(expected, builder.ToString());
        builder.Static().New().Virtual().Abstract().Sealed().Override().ReadOnly().Const();
        OutputAssert.Equal(expected, builder.ToString());
    }

    [Fact]
    public void Field_InvokesConfigureImmediatelyOnceOnReturnedBuilder()
    {
        FieldBuilder? configured = null;
        var calls = 0;
        var builder = CSharp.Field("int", "count", f =>
        {
            configured = f;
            calls++;
            f.Private().Initializer("0");
        });

        Assert.Same(builder, configured);
        Assert.Equal(1, calls);
        OutputAssert.Equal("private int count = 0;\n", builder.ToString());
        OutputAssert.Equal("private int count = 0;\n", builder.ToString());
        Assert.Equal(1, calls);
        Assert.NotSame(builder, CSharp.Field("int", "count"));
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void TypeFields_ConfigureImmediatelyKeepReferencesAndReturnParent(string keyword)
    {
        switch (keyword)
        {
            case "class": AssertTypeFields(CSharp.Class("Outer"), keyword); break;
            case "struct": AssertTypeFields(CSharp.Struct("Outer"), keyword); break;
            case "record": AssertTypeFields(CSharp.Record("Outer"), keyword); break;
            case "record struct": AssertTypeFields(CSharp.RecordStruct("Outer"), keyword); break;
        }
    }

    private static void AssertTypeFields<TSelf>(ClassOrStructBuilder<TSelf> builder, string keyword)
        where TSelf : ClassOrStructBuilder<TSelf>
    {
        FieldBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Field("int", "added");

        Assert.Same(builder, builder.Field("int", "first"));
        Assert.Same(builder, builder.Field("int", "count", f =>
        {
            configured = f;
            calls++;
            f.Public();
        }));
        Assert.Same(builder, builder.Add(added));
        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, builder.items[1].Target);
        Assert.Same(added, builder.items[2].Target);
        OutputAssert.Equal(keyword + " Outer\n{\n    int first;\n\n    public int count;\n\n    int added;\n}\n", builder.ToString());

        configured.Private();
        added.Static().Initializer("2");
        var expected = keyword + " Outer\n{\n    int first;\n\n    private int count;\n\n    static int added = 2;\n}\n";
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void MixedBodyItems_PreserveOrderSpacingAndNestedFieldIndentation()
    {
        var builder = CSharp.Class("Outer").Field("int", "first")
            .Method("Run", m => m.Body())
            .Class("Nested", c => c.Field("string", "name"))
            .Raw("// first\r\n// second")
            .Add(CSharp.Field("int", "last"));

        OutputAssert.Equal(
            "class Outer\n{\n    int first;\n\n    void Run()\n    {\n    }\n\n" +
            "    class Nested\n    {\n        string name;\n    }\n\n    // first\n    // second\n\n    int last;\n}\n",
            builder.ToString());
    }

    [Fact]
    public void ConsecutiveFields_RenderWithBlankLine()
    {
        var builder = CSharp.Class("Limits", c => c.Public().Static()
            .Field("int", "Max", f => f.Public().Const().Initializer("10"))
            .Field("string", "name", f => f.Private().Static().ReadOnly().Initializer("\"x\"")));

        OutputAssert.Equal("public static class Limits\n{\n    public const int Max = 10;\n\n    private static readonly string name = \"x\";\n}\n",
            builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("1count")]
    [InlineData("count.Value")]
    [InlineData("count Value")]
    [InlineData("count-Value")]
    [InlineData("@@count")]
    public void InvalidNames_ThrowAtCallTimeBeforeConfiguringOrAdding(string name)
    {
        var configured = false;
        Assert.Throws<ArgumentException>("name", () => CSharp.Field("int", name));
        Assert.Throws<ArgumentException>("name", () => CSharp.Field("int", name, _ => configured = true));
        Assert.False(configured);

        AssertInvalidName(CSharp.Class("Outer"), name);
        AssertInvalidName(CSharp.Struct("Outer"), name);
        AssertInvalidName(CSharp.Record("Outer"), name);
        AssertInvalidName(CSharp.RecordStruct("Outer"), name);
    }

    private static void AssertInvalidName<TSelf>(ClassOrStructBuilder<TSelf> builder, string name)
        where TSelf : ClassOrStructBuilder<TSelf>
    {
        var configured = false;
        var before = builder.ToString();
        Assert.Throws<ArgumentException>("name", () => builder.Field("int", name));
        Assert.Throws<ArgumentException>("name", () => builder.Field("int", name, _ => configured = true));
        Assert.False(configured);
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void NullArguments_ThrowWithParameterNameBeforeConfiguringOrAdding()
    {
        var configured = false;
        Assert.Throws<ArgumentNullException>("type", () => CSharp.Field(null!, "count"));
        Assert.Throws<ArgumentNullException>("type", () => CSharp.Field(null!, "count", _ => configured = true));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Field("int", null!));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Field("int", null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Field("int", "count", null!));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Field(null!, null!, null!));
        Assert.False(configured);
        var field = CSharp.Field("int", "count").Initializer("1");
        Assert.Throws<ArgumentNullException>("expression", () => field.Initializer(null!));
        OutputAssert.Equal("int count = 1;\n", field.ToString());

        AssertNullArguments(CSharp.Class("Outer"));
        AssertNullArguments(CSharp.Struct("Outer"));
        AssertNullArguments(CSharp.Record("Outer"));
        AssertNullArguments(CSharp.RecordStruct("Outer"));
    }

    private static void AssertNullArguments<TSelf>(ClassOrStructBuilder<TSelf> builder)
        where TSelf : ClassOrStructBuilder<TSelf>
    {
        var configured = false;
        var before = builder.ToString();
        Assert.Throws<ArgumentNullException>("type", () => builder.Field(null!, "count"));
        Assert.Throws<ArgumentNullException>("type", () => builder.Field(null!, "count", _ => configured = true));
        Assert.Throws<ArgumentNullException>("name", () => builder.Field("int", null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Field("int", null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Field("int", "count", null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Field(null!, null!, null!));
        Assert.Throws<ArgumentNullException>("fieldBuilder", () => builder.Add((FieldBuilder)null!));
        Assert.False(configured);
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void FluentMethods_ReturnSameFieldBuilder()
    {
        var builder = CSharp.Field("int", "count");

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
        Assert.Same(builder, builder.Const());
        Assert.Same(builder, builder.Initializer("1"));
    }

    [Theory]
    [InlineData(typeof(InterfaceBuilder))]
    [InlineData(typeof(EnumBuilder))]
    [InlineData(typeof(FileBuilder))]
    [InlineData(typeof(NamespaceBuilder))]
    public void UnsupportedContainers_DoNotExposeFieldEntryPoints(Type type)
    {
        Assert.DoesNotContain(type.GetMethods(), method => method.Name == "Field");
        Assert.DoesNotContain(type.GetMethods(), method => method.Name == "Add" &&
            Array.Exists(method.GetParameters(), parameter => parameter.ParameterType == typeof(FieldBuilder)));
    }

    [Fact]
    public void CompleteFile_RendersFieldsAndCompiles()
    {
        var file = CSharp.File()
            .Class("Values", c => c
                .Field("int", "Max", f => f.Public().Const().Initializer("10"))
                .Add(CSharp.Field("string", "Name").Static().ReadOnly().Initializer("\"x\""))
                .Field("int", "count", f => f.Initializer("1"))
                .Field("int", "class"))
            .Class("Box", c => c.TypeParameter("T").Field("T", "value"))
            .Struct("Point", s => s.Field("int", "x").Field("int", "Origin", f => f.Static().Initializer("0")))
            .Struct("ReadOnlyPoint", s => s.ReadOnly().Field("int", "x", f => f.ReadOnly()))
            .Record("Counter", r => r.Field("int", "count", f => f.Initializer("1"))
                .Method("GetCount", m => m.Returns("int").Arrow("count")))
            .RecordStruct("ValueCounter", r => r.Field("int", "count")
                .Field("int", "Start", f => f.Static().Initializer("1")));
        const string expected =
            "class Values\n{\n    public const int Max = 10;\n\n    static readonly string Name = \"x\";\n\n" +
            "    int count = 1;\n\n    int @class;\n}\n\n" +
            "class Box<T>\n{\n    T value;\n}\n\n" +
            "struct Point\n{\n    int x;\n\n    static int Origin = 0;\n}\n\n" +
            "readonly struct ReadOnlyPoint\n{\n    readonly int x;\n}\n\n" +
            "record Counter\n{\n    int count = 1;\n\n    int GetCount() => count;\n}\n\n" +
            "record struct ValueCounter\n{\n    int count;\n\n    static int Start = 1;\n}\n";

        OutputAssert.Equal(expected, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }
}
