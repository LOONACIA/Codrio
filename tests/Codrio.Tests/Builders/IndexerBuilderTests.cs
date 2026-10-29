using System;
using Xunit;

namespace Codrio.Tests;

public class IndexerBuilderTests
{
    [Fact]
    public void ToString_RendersDefaultAccessorsWithoutValidatingParameterCount()
    {
        var builder = CSharp.Indexer("int");

        OutputAssert.Equal("int this[] { get; set; }\n", builder.ToString());
        OutputAssert.Equal("int this[] { get; set; }\n", builder.ToString());
        OutputAssert.Equal("int this[] { get; set; }\n", CSharp.Indexer("int", _ => { }).ToString());
        builder.Parameter("int", "index");
        OutputAssert.Equal("int this[int index] { get; set; }\n", builder.ToString());
    }

    [Theory]
    [InlineData("int")]
    [InlineData("global::System.Collections.Generic.List<string>")]
    [InlineData("unvalidated type")]
    [InlineData("")]
    public void Indexer_WritesTypeVerbatim(string type)
    {
        OutputAssert.Equal(type + " this[] { get; set; }\n", CSharp.Indexer(type).ToString());
    }

    [Fact]
    public void Parameters_PreserveOrderModifiersDefaultsAndEscapedNames()
    {
        var builder = CSharp.Indexer("int").Parameter("int", "row").Parameter("int", "column").Get();
        OutputAssert.Equal("int this[int row, int column] { get; }\n", builder.ToString());

        var configured = CSharp.Indexer("string")
            .Parameter("int", "class", p => p.In().Default("0"))
            .Parameter("int[]", "indices", p => p.Params());
        OutputAssert.Equal("string this[in int @class = 0, params int[] indices] { get; set; }\n", configured.ToString());
    }

    [Theory]
    [InlineData("G", "get;")]
    [InlineData("GI", "get; init;")]
    [InlineData("SG", "get; set;")]
    [InlineData("IG", "get; init;")]
    [InlineData("GSI", "get; init;")]
    [InlineData("GIS", "get; set;")]
    public void Accessors_UseSharedOrderingAndSetterSlot(string calls, string expected)
    {
        var builder = CSharp.Indexer("int").Parameter("int", "index");
        foreach (var call in calls)
        {
            switch (call)
            {
                case 'G': builder.Get(); break;
                case 'S': builder.Set(); break;
                case 'I': builder.Init(); break;
            }
        }

        OutputAssert.Equal("int this[int index] { " + expected + " }\n", builder.ToString());
    }

    [Fact]
    public void AccessorBodies_RenderGetterArrowAndSetterBlock()
    {
        var builder = CSharp.Indexer("string", i => i.Public().Parameter("int", "index")
            .Set(a => a.Body(b => b.Line("items[index] = value;"))).Get(a => a.Arrow("items[index]")));

        OutputAssert.Equal("public string this[int index]\n{\n    get => items[index];\n    set\n    {\n" +
            "        items[index] = value;\n    }\n}\n", builder.ToString());
    }

    [Fact]
    public void DeclarationArrow_ReplacesAccessorsAndIsClearedByAccessorCalls()
    {
        var builder = CSharp.Indexer("int", i => i.Public().Parameter("int", "i")
            .Get(a => a.Body()).Set(a => a.Body()).Arrow("values[i]"));
        OutputAssert.Equal("public int this[int i] => values[i];\n", builder.ToString());
        builder.Init(a => a.Arrow("values[i] = value")).Get();
        OutputAssert.Equal("public int this[int i]\n{\n    get;\n    init => values[i] = value;\n}\n", builder.ToString());
        builder.Arrow("0").Get();
        OutputAssert.Equal("public int this[int i] { get; }\n", builder.ToString());
    }

    [Fact]
    public void Modifiers_RenderInFixedOrderWithoutValidatingCombinations()
    {
        var builder = CSharp.Indexer("int").ReadOnly().Override().Sealed().Abstract().Virtual().New().Static().Public();

        OutputAssert.Equal("public static new virtual abstract sealed override readonly int this[] { get; set; }\n",
            builder.ToString());
    }

    [Fact]
    public void Indexer_InvokesConfigureImmediatelyOnceOnReturnedBuilder()
    {
        IndexerBuilder? configured = null;
        var calls = 0;
        var builder = CSharp.Indexer("int", i =>
        {
            configured = i;
            calls++;
            i.Parameter("int", "index").Get();
        });

        Assert.Same(builder, configured);
        Assert.Equal(1, calls);
        OutputAssert.Equal("int this[int index] { get; }\n", builder.ToString());
        OutputAssert.Equal("int this[int index] { get; }\n", builder.ToString());
        Assert.Equal(1, calls);
        Assert.NotSame(builder, CSharp.Indexer("int"));
    }

    [Fact]
    public void Parameter_InvokesConfigureImmediatelyOnceAndRetainsReference()
    {
        var builder = CSharp.Indexer("int");
        ParameterBuilder? configured = null;
        var calls = 0;
        Assert.Same(builder, builder.Parameter("int", "index", p =>
        {
            configured = p;
            calls++;
            OutputAssert.Equal("int this[] { get; set; }\n", builder.ToString());
        }));

        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        configured.Default("1");
        OutputAssert.Equal("int this[int index = 1] { get; set; }\n", builder.ToString());
        OutputAssert.Equal("int this[int index = 1] { get; set; }\n", builder.ToString());
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void TypeIndexers_ConfigureImmediatelyKeepReferencesAndReturnParent(string keyword)
    {
        switch (keyword)
        {
            case "class": AssertTypeIndexers(CSharp.Class("Outer"), keyword); break;
            case "struct": AssertTypeIndexers(CSharp.Struct("Outer"), keyword); break;
            case "interface": AssertTypeIndexers(CSharp.Interface("Outer"), keyword); break;
            case "record": AssertTypeIndexers(CSharp.Record("Outer"), keyword); break;
            case "record struct": AssertTypeIndexers(CSharp.RecordStruct("Outer"), keyword); break;
        }
    }

    private static void AssertTypeIndexers<TSelf>(TypeDeclarationBuilder<TSelf> builder, string keyword)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        IndexerBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Indexer("string").Parameter("string", "key").Get();

        Assert.Same(builder, builder.Indexer("int"));
        Assert.Same(builder, builder.Indexer("int", i =>
        {
            configured = i;
            calls++;
            i.Parameter("int", "index").Get();
        }));
        Assert.Same(builder, builder.Add(added));
        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, builder.items[1].Target);
        Assert.Same(added, builder.items[2].Target);
        OutputAssert.Equal(keyword + " Outer\n{\n    int this[] { get; set; }\n\n" +
            "    int this[int index] { get; }\n\n    string this[string key] { get; }\n}\n", builder.ToString());

        configured.Public();
        added.Arrow("key");
        var expected = keyword + " Outer\n{\n    int this[] { get; set; }\n\n" +
            "    public int this[int index] { get; }\n\n    string this[string key] => key;\n}\n";
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void MixedItems_PreserveOrderAndBlankLines()
    {
        var builder = CSharp.Class("Values").Indexer("int", i => i.Parameter("int", "i").Arrow("i"))
            .Property("int", "Count").Method("Run", m => m.Body()).Raw("// first\r\n// second")
            .Add(CSharp.Indexer("string").Parameter("string", "key").Arrow("key"));

        OutputAssert.Equal("class Values\n{\n    int this[int i] => i;\n\n    int Count { get; set; }\n\n" +
            "    void Run()\n    {\n    }\n\n    // first\n    // second\n\n" +
            "    string this[string key] => key;\n}\n", builder.ToString());
    }

    [Fact]
    public void IndexerOnlyRecord_RendersBlock()
    {
        OutputAssert.Equal("record Values\n{\n    public int this[int i] => i;\n}\n",
            CSharp.Record("Values").Indexer("int", i => i.Public().Parameter("int", "i").Arrow("i")).ToString());
    }

    [Fact]
    public void NestedClass_IndentsIndexerAndAccessorBodies()
    {
        var builder = CSharp.Class("Outer").Class("Inner", c => c.Indexer("int", i => i.Parameter("int", "index")
            .Get(a => a.Arrow("items[index]")).Set(a => a.Body(b => b.Line("items[index] = value;")))));

        OutputAssert.Equal("class Outer\n{\n    class Inner\n    {\n        int this[int index]\n        {\n" +
            "            get => items[index];\n            set\n            {\n" +
            "                items[index] = value;\n            }\n        }\n    }\n}\n", builder.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("1index")]
    [InlineData("a.b")]
    [InlineData("a b")]
    [InlineData("a-b")]
    [InlineData("@@index")]
    public void InvalidParameterNames_ThrowAtCallTimeBeforeConfiguringOrAdding(string name)
    {
        var builder = CSharp.Indexer("int");
        var configured = false;

        Assert.Throws<ArgumentException>("name", () => builder.Parameter("int", name));
        Assert.Throws<ArgumentException>("name", () => builder.Parameter("int", name, _ => configured = true));
        Assert.False(configured);
        OutputAssert.Equal("int this[] { get; set; }\n", builder.ToString());
    }

    [Fact]
    public void NullArguments_ThrowBeforeConfiguringOrAdding()
    {
        var configured = false;
        Assert.Throws<ArgumentNullException>("type", () => new IndexerBuilder(null!));
        Assert.Throws<ArgumentNullException>("type", () => CSharp.Indexer(null!));
        Assert.Throws<ArgumentNullException>("type", () => CSharp.Indexer(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Indexer("int", null!));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Indexer(null!, null!));
        var builder = CSharp.Indexer("int");
        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter((string)null!, "index"));
        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter((string)null!, "index", _ => configured = true));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter("int", null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter("int", null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Parameter("int", "index", null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Parameter((string)null!, null!, null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Get(null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Set(null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Init(null!));
        Assert.Throws<ArgumentNullException>("expression", () => builder.Arrow(null!));
        Assert.False(configured);
        OutputAssert.Equal("int this[] { get; set; }\n", builder.ToString());

        AssertNullArguments(CSharp.Class("Outer"));
        AssertNullArguments(CSharp.Struct("Outer"));
        AssertNullArguments(CSharp.Interface("Outer"));
        AssertNullArguments(CSharp.Record("Outer"));
        AssertNullArguments(CSharp.RecordStruct("Outer"));
    }

    private static void AssertNullArguments<TSelf>(TypeDeclarationBuilder<TSelf> builder)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        var configured = false;
        var before = builder.ToString();
        Assert.Throws<ArgumentNullException>("type", () => builder.Indexer(null!));
        Assert.Throws<ArgumentNullException>("type", () => builder.Indexer(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Indexer("int", null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Indexer(null!, null!));
        Assert.Throws<ArgumentNullException>("indexerBuilder", () => builder.Add((IndexerBuilder)null!));
        Assert.False(configured);
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void ConfigurationFailure_DoesNotAddParameterOrIndexer()
    {
        var builder = CSharp.Indexer("int");
        Assert.Throws<InvalidOperationException>(() => builder.Parameter("int", "index", _ => throw new InvalidOperationException()));
        OutputAssert.Equal("int this[] { get; set; }\n", builder.ToString());
        var parent = CSharp.Record("Values");
        Assert.Throws<InvalidOperationException>(() => parent.Indexer("int", _ => throw new InvalidOperationException()));
        OutputAssert.Equal("record Values;\n", parent.ToString());
    }

    [Fact]
    public void FluentMethods_ReturnSameIndexerBuilder()
    {
        var builder = CSharp.Indexer("int");

        Assert.Same(builder, builder.Parameter("int", "index"));
        Assert.Same(builder, builder.Parameter("int", "other", p => p.Default("0")));
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
        Assert.Same(builder, builder.Get());
        Assert.Same(builder, builder.Set());
        Assert.Same(builder, builder.Init());
        Assert.Same(builder, builder.Get(a => a.Body()));
        Assert.Same(builder, builder.Set(a => a.Body()));
        Assert.Same(builder, builder.Init(a => a.Body()));
        Assert.Same(builder, builder.Arrow("0"));
    }

    [Fact]
    public void Indexer_DoesNotExposeInitializer()
    {
        Assert.DoesNotContain(typeof(IndexerBuilder).GetMethods(), method => method.Name == "Initializer");
    }

    [Theory]
    [InlineData(typeof(EnumBuilder))]
    [InlineData(typeof(FileBuilder))]
    [InlineData(typeof(NamespaceBuilder))]
    public void UnsupportedContainers_DoNotExposeIndexerEntryPoints(Type type)
    {
        Assert.DoesNotContain(type.GetMethods(), method => method.Name == "Indexer");
        Assert.DoesNotContain(type.GetMethods(), method => method.Name == "Add" &&
            Array.Exists(method.GetParameters(), parameter => parameter.ParameterType == typeof(IndexerBuilder)));
    }

    [Fact]
    public void CompleteFile_RendersIndexersAndCompiles()
    {
        var file = CSharp.File()
            .Class("Grid", c => c.Field("int[]", "values", f => f.Initializer("new int[10]"))
                .Indexer("int", i => i.Public().Parameter("int", "index")
                    .Get(a => a.Arrow("values[index]")).Set(a => a.Arrow("values[index] = value")))
                .Indexer("int", i => i.Public().Parameter("int", "row").Parameter("int", "column").Arrow("values[row + column]")))
            .Class("Selection", c => c.Indexer("int", i => i.Public().Parameter("int[]", "indices", p => p.Params())
                .Arrow("indices.Length")))
            .Interface("IValues", t => t.Indexer("int", i => i.Parameter("int", "index").Get()))
            .Class("Values", c => c.BaseType("IValues").Indexer("int", i => i.Public().Parameter("int", "index").Arrow("index")))
            .Class("Base", c => c.Abstract().Indexer("int", i => i.Public().Abstract().Parameter("int", "index").Get()))
            .Class("Derived", c => c.BaseType("Base").Indexer("int", i => i.Public().Override().Parameter("int", "index").Arrow("index")))
            .Struct("Point", s => s.Indexer("int", i => i.Public().Parameter("int", "index").Arrow("index")))
            .Record("Item", r => r.Indexer("int", i => i.Public().Parameter("int", "index").Arrow("index")))
            .RecordStruct("ValueItem", r => r.Indexer("int", i => i.Public().Parameter("int", "index").Arrow("index")));
        const string expected =
            "class Grid\n{\n    int[] values = new int[10];\n\n    public int this[int index]\n    {\n" +
            "        get => values[index];\n        set => values[index] = value;\n    }\n\n" +
            "    public int this[int row, int column] => values[row + column];\n}\n\n" +
            "class Selection\n{\n    public int this[params int[] indices] => indices.Length;\n}\n\n" +
            "interface IValues\n{\n    int this[int index] { get; }\n}\n\n" +
            "class Values : IValues\n{\n    public int this[int index] => index;\n}\n\n" +
            "abstract class Base\n{\n    public abstract int this[int index] { get; }\n}\n\n" +
            "class Derived : Base\n{\n    public override int this[int index] => index;\n}\n\n" +
            "struct Point\n{\n    public int this[int index] => index;\n}\n\n" +
            "record Item\n{\n    public int this[int index] => index;\n}\n\n" +
            "record struct ValueItem\n{\n    public int this[int index] => index;\n}\n";

        OutputAssert.Equal(expected, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }
}
