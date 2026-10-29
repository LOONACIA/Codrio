using System;
using Xunit;

namespace Codrio.Tests;

public class PropertyBuilderTests
{
    [Fact]
    public void AccessorBodies_RenderBlockWithGetterBeforeSetter()
    {
        var builder = CSharp.Property("int", "Count", p => p.Public()
            .Set(a => a.Body(b => b.Line("count = value;"))).Get(a => a.Arrow("count")));

        OutputAssert.Equal("public int Count\n{\n    get => count;\n    set\n    {\n        count = value;\n    }\n}\n",
            builder.ToString());
    }

    [Fact]
    public void GetterBody_RendersWithoutImplicitSetter()
    {
        var builder = CSharp.Property("int", "Count", p => p.Get(a => a.Body(b => b.Line("return count;"))));

        OutputAssert.Equal("int Count\n{\n    get\n    {\n        return count;\n    }\n}\n", builder.ToString());
    }

    [Fact]
    public void MixedAccessors_RenderBodylessAccessorInsideBlock()
    {
        var builder = CSharp.Property("int", "Count").Init(a => a.Arrow("field = value")).Get();
        OutputAssert.Equal("int Count\n{\n    get;\n    init => field = value;\n}\n", builder.ToString());
        builder.Set(a => a.Body());
        OutputAssert.Equal("int Count\n{\n    get;\n    set\n    {\n    }\n}\n", builder.ToString());
        builder.Get(a => a.Arrow("field")).Set();
        OutputAssert.Equal("int Count\n{\n    get => field;\n    set;\n}\n", builder.ToString());
    }

    [Fact]
    public void BodylessCalls_ReplaceConfiguredAccessors()
    {
        var builder = CSharp.Property("int", "Count").Get(a => a.Arrow("count"));
        builder.Get();
        OutputAssert.Equal("int Count { get; }\n", builder.ToString());
        builder.Set(a => a.Body(b => b.Line("count = value;"))).Set();
        OutputAssert.Equal("int Count { get; set; }\n", builder.ToString());
        builder.Init(a => a.Arrow("count = value")).Init();
        OutputAssert.Equal("int Count { get; init; }\n", builder.ToString());
    }

    [Fact]
    public void ConfiguredSetAndInit_ShareSlotWithLastCallWinning()
    {
        var builder = CSharp.Property("int", "Count").Get(a => a.Arrow("count"))
            .Set(a => a.Body()).Init(a => a.Arrow("count = value"));
        OutputAssert.Equal("int Count\n{\n    get => count;\n    init => count = value;\n}\n", builder.ToString());
        builder.Set(a => a.Body(b => b.Line("count = value;")));
        OutputAssert.Equal("int Count\n{\n    get => count;\n    set\n    {\n        count = value;\n    }\n}\n",
            builder.ToString());
        builder.Init();
        OutputAssert.Equal("int Count\n{\n    get => count;\n    init;\n}\n", builder.ToString());
        builder.Init(a => a.Body()).Set();
        OutputAssert.Equal("int Count\n{\n    get => count;\n    set;\n}\n", builder.ToString());
    }

    [Fact]
    public void ArrowAndConfiguredAccessors_LastCallWins()
    {
        var builder = CSharp.Property("int", "Count").Get(a => a.Body()).Set(a => a.Body()).Arrow("1");
        OutputAssert.Equal("int Count => 1;\n", builder.ToString());
        builder.Get(a => a.Arrow("2"));
        OutputAssert.Equal("int Count\n{\n    get => 2;\n}\n", builder.ToString());
        builder.Arrow("3").Set(a => a.Body());
        OutputAssert.Equal("int Count\n{\n    set\n    {\n    }\n}\n", builder.ToString());
        builder.Arrow("4").Init(a => a.Arrow("count = value"));
        OutputAssert.Equal("int Count\n{\n    init => count = value;\n}\n", builder.ToString());
    }

    [Fact]
    public void BlockInitializer_IsRetainedAcrossDeclarationArrow()
    {
        var builder = CSharp.Property("string", "Name", p => p.Public()
            .Get(a => a.Arrow("field")).Set(a => a.Arrow("field = value.Trim()")).Initializer("\"\""));
        OutputAssert.Equal("public string Name\n{\n    get => field;\n    set => field = value.Trim();\n} = \"\";\n",
            builder.ToString());
        builder.Arrow("name");
        OutputAssert.Equal("public string Name => name;\n", builder.ToString());
        builder.Get(a => a.Body(b => b.Line("return field;")));
        OutputAssert.Equal("public string Name\n{\n    get\n    {\n        return field;\n    }\n} = \"\";\n",
            builder.ToString());
    }

    [Fact]
    public void NestedClass_IndentsPropertyAccessorAndBodyBlocks()
    {
        var builder = CSharp.Class("Outer").Class("Inner", c => c.Property("int", "Count", p => p
            .Get(a => a.Body(b => b.Block("if (ready)", ib => ib.Line("return field;"))
                .Line("return 0;"))).Initializer("0"))
            .Property("int", "Next"));

        OutputAssert.Equal("class Outer\n{\n    class Inner\n    {\n        int Count\n        {\n" +
            "            get\n            {\n                if (ready)\n                {\n" +
            "                    return field;\n                }\n                return 0;\n            }\n" +
            "        } = 0;\n\n        int Next { get; set; }\n    }\n}\n", builder.ToString());
    }

    [Fact]
    public void EmptyAccessorConfiguration_KeepsSingleLineFormat()
    {
        var builder = CSharp.Property("int", "Count", p => p.Get(_ => { }));
        OutputAssert.Equal("int Count { get; }\n", builder.ToString());
        builder.Set(_ => { });
        OutputAssert.Equal("int Count { get; set; }\n", builder.ToString());
        builder.Init(_ => { });
        OutputAssert.Equal("int Count { get; init; }\n", builder.ToString());
    }

    [Theory]
    [InlineData("get")]
    [InlineData("set")]
    [InlineData("init")]
    public void AccessorConfiguration_RunsImmediatelyOnceAndRetainsFreshBuilder(string keyword)
    {
        var builder = CSharp.Property("int", "Count").Arrow("0");
        AccessorBuilder? accessor = null;
        var calls = 0;
        Action<AccessorBuilder> configure = a =>
        {
            calls++;
            accessor = a;
            OutputAssert.Equal(keyword + ";\n", a.ToString());
            OutputAssert.Equal("int Count => 0;\n", builder.ToString());
            a.Arrow("1");
        };
        switch (keyword)
        {
            case "get": Assert.Same(builder, builder.Get(configure)); break;
            case "set": Assert.Same(builder, builder.Set(configure)); break;
            case "init": Assert.Same(builder, builder.Init(configure)); break;
        }

        Assert.Equal(1, calls);
        Assert.NotNull(accessor);
        accessor.Arrow("2");
        var expected = "int Count\n{\n    " + keyword + " => 2;\n}\n";
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
        Action<AccessorBuilder> replace = a =>
        {
            Assert.NotSame(accessor, a);
            OutputAssert.Equal(keyword + ";\n", a.ToString());
        };
        switch (keyword)
        {
            case "get": builder.Get(replace); break;
            case "set": builder.Set(replace); break;
            case "init": builder.Init(replace); break;
        }

        accessor.Body();
        OutputAssert.Equal("int Count { " + keyword + "; }\n", builder.ToString());
    }

    [Fact]
    public void NullAccessorConfiguration_ThrowsWithoutChangingDeclaration()
    {
        var builder = CSharp.Property("int", "Count").Arrow("1");

        Assert.Throws<ArgumentNullException>("configure", () => builder.Get(null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Set(null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Init(null!));
        OutputAssert.Equal("int Count => 1;\n", builder.ToString());
    }

    [Fact]
    public void CompleteFile_RendersAccessorBodiesAndCompiles()
    {
        var file = CSharp.File()
            .Class("Counter", c => c.Field("int", "count")
                .Property("int", "Count", p => p.Public().Get(a => a.Arrow("count"))
                    .Set(a => a.Body(b => b.Line("count = value;"))))
                .Property("int", "Positive", p => p.Public().Get(a => a.Body(b => b
                    .Block("if (count > 0)", ib => ib.Line("return count;"))
                    .Block("else", eb => eb.Line("return 0;")))))
                .Property("int", "Ignored", p => p.Public().Get(a => a.Arrow("0")).Set(a => a.Body()))
                .Property("int", "Zero", p => p.Public().Static().Get(a => a.Body(b => b.Line("return 0;")))))
            .Class("Base", c => c.Abstract().Property("int", "Value", p => p.Public().Abstract().Get()))
            .Class("Derived", c => c.BaseType("Base").Property("int", "Value", p => p.Public().Override()
                .Get(a => a.Body(b => b.Line("return 1;")))))
            .Struct("Point", s => s.Property("int", "Zero", p => p.Public().ReadOnly()
                .Get(a => a.Body(b => b.Line("return 0;")))))
            .Interface("ICounter", i => i.Property("int", "Count", p => p.Get(a => a.Arrow("1"))))
            .Class("Implementation", c => c.BaseType("ICounter"));
        const string expected =
            "class Counter\n{\n    int count;\n\n    public int Count\n    {\n        get => count;\n" +
            "        set\n        {\n            count = value;\n        }\n    }\n\n" +
            "    public int Positive\n    {\n        get\n        {\n            if (count > 0)\n            {\n" +
            "                return count;\n            }\n            else\n            {\n" +
            "                return 0;\n            }\n        }\n    }\n\n" +
            "    public int Ignored\n    {\n        get => 0;\n        set\n        {\n        }\n    }\n\n" +
            "    public static int Zero\n    {\n        get\n        {\n            return 0;\n        }\n    }\n}\n\n" +
            "abstract class Base\n{\n    public abstract int Value { get; }\n}\n\n" +
            "class Derived : Base\n{\n    public override int Value\n    {\n        get\n        {\n" +
            "            return 1;\n        }\n    }\n}\n\n" +
            "struct Point\n{\n    public readonly int Zero\n    {\n        get\n        {\n" +
            "            return 0;\n        }\n    }\n}\n\n" +
            "interface ICounter\n{\n    int Count\n    {\n        get => 1;\n    }\n}\n\n" +
            "class Implementation : ICounter\n{\n}\n";

        OutputAssert.Equal(expected, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }

    [Theory]
    [InlineData("Count", "int Count { get; set; }\n")]
    [InlineData("class", "int @class { get; set; }\n")]
    [InlineData("@class", "int @class { get; set; }\n")]
    public void ToString_RendersDefaultAccessorsAndEscapesNames(string name, string expected)
    {
        var builder = CSharp.Property("int", name);

        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, CSharp.Property("int", name, _ => { }).ToString());
    }

    [Theory]
    [InlineData("int")]
    [InlineData("global::System.Collections.Generic.List<string>")]
    [InlineData("unvalidated type")]
    [InlineData("")]
    public void Property_WritesTypeVerbatim(string type)
    {
        OutputAssert.Equal(type + " Count { get; set; }\n", CSharp.Property(type, "Count").ToString());
    }

    [Theory]
    [InlineData("G", "get;")]
    [InlineData("GS", "get; set;")]
    [InlineData("GI", "get; init;")]
    [InlineData("S", "set;")]
    [InlineData("I", "init;")]
    [InlineData("SG", "get; set;")]
    [InlineData("IG", "get; init;")]
    [InlineData("SI", "init;")]
    [InlineData("IS", "set;")]
    [InlineData("GSI", "get; init;")]
    [InlineData("GIS", "get; set;")]
    [InlineData("GGSS", "get; set;")]
    [InlineData("IIGG", "get; init;")]
    public void Accessors_RenderInFixedOrderWithLastSetterWinning(string calls, string expected)
    {
        var builder = CSharp.Property("int", "Count");
        foreach (var call in calls)
        {
            switch (call)
            {
                case 'G': builder.Get(); break;
                case 'S': builder.Set(); break;
                case 'I': builder.Init(); break;
            }
        }

        OutputAssert.Equal("int Count { " + expected + " }\n", builder.ToString());
    }

    [Theory]
    [InlineData("42")]
    [InlineData("unvalidated expression")]
    [InlineData("")]
    public void Expressions_AreVerbatimAndLastCallWins(string expression)
    {
        OutputAssert.Equal("int Count { get; set; } = " + expression + ";\n",
            CSharp.Property("int", "Count").Initializer("0").Initializer(expression).ToString());
        OutputAssert.Equal("int Count => " + expression + ";\n",
            CSharp.Property("int", "Count").Arrow("0").Arrow(expression).ToString());
    }

    [Fact]
    public void ArrowAndAccessors_LastCallWinsAndInitializerIsRetainedButOmittedForArrow()
    {
        var builder = CSharp.Property("int", "Count").Get().Set().Initializer("1").Arrow("2");
        OutputAssert.Equal("int Count => 2;\n", builder.ToString());
        builder.Initializer("3");
        OutputAssert.Equal("int Count => 2;\n", builder.ToString());
        builder.Get();
        OutputAssert.Equal("int Count { get; } = 3;\n", builder.ToString());
        builder.Arrow("4").Set();
        OutputAssert.Equal("int Count { set; } = 3;\n", builder.ToString());
        builder.Arrow("5").Init();
        OutputAssert.Equal("int Count { init; } = 3;\n", builder.ToString());
        builder.Get().Arrow("6");
        OutputAssert.Equal("int Count => 6;\n", builder.ToString());
    }

    [Fact]
    public void Modifiers_RenderInFixedOrderOnceWithoutValidatingCombinations()
    {
        var builder = CSharp.Property("int", "X").ReadOnly().Override().Sealed()
            .Abstract().Virtual().New().Static().Public();
        const string expected = "public static new virtual abstract sealed override readonly int X { get; set; }\n";

        OutputAssert.Equal(expected, builder.ToString());
        builder.Static().New().Virtual().Abstract().Sealed().Override().ReadOnly();
        OutputAssert.Equal(expected, builder.ToString());
    }

    [Fact]
    public void Accessibility_LastCallWins()
    {
        var builder = CSharp.Property("int", "Count").Public().Internal();
        OutputAssert.Equal("internal int Count { get; set; }\n", builder.ToString());
        builder.Protected();
        OutputAssert.Equal("protected int Count { get; set; }\n", builder.ToString());
        builder.Private();
        OutputAssert.Equal("private int Count { get; set; }\n", builder.ToString());
        builder.ProtectedInternal();
        OutputAssert.Equal("protected internal int Count { get; set; }\n", builder.ToString());
        builder.PrivateProtected();
        OutputAssert.Equal("private protected int Count { get; set; }\n", builder.ToString());
        builder.Public();
        OutputAssert.Equal("public int Count { get; set; }\n", builder.ToString());
    }

    [Theory]
    [InlineData("get")]
    [InlineData("set")]
    [InlineData("init")]
    public void Accessor_RendersStandaloneFragment(string keyword)
    {
        OutputAssert.Equal(keyword + ";\n", new AccessorBuilder(keyword).ToString());
    }

    [Fact]
    public void Property_InvokesConfigureImmediatelyOnceOnReturnedBuilder()
    {
        PropertyBuilder? configured = null;
        var calls = 0;
        var builder = CSharp.Property("int", "Count", p =>
        {
            configured = p;
            calls++;
            p.Public();
        });

        Assert.Same(builder, configured);
        Assert.Equal(1, calls);
        OutputAssert.Equal("public int Count { get; set; }\n", builder.ToString());
        OutputAssert.Equal("public int Count { get; set; }\n", builder.ToString());
        Assert.Equal(1, calls);
        Assert.NotSame(builder, CSharp.Property("int", "Count"));
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void TypeProperties_ConfigureImmediatelyKeepReferencesAndReturnParent(string keyword)
    {
        switch (keyword)
        {
            case "class": AssertTypeProperties(CSharp.Class("Outer"), keyword); break;
            case "struct": AssertTypeProperties(CSharp.Struct("Outer"), keyword); break;
            case "interface": AssertTypeProperties(CSharp.Interface("Outer"), keyword); break;
            case "record": AssertTypeProperties(CSharp.Record("Outer"), keyword); break;
            case "record struct": AssertTypeProperties(CSharp.RecordStruct("Outer"), keyword); break;
        }
    }

    private static void AssertTypeProperties<TSelf>(TypeDeclarationBuilder<TSelf> builder, string keyword)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        PropertyBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Property("int", "Added");

        Assert.Same(builder, builder.Property("int", "First"));
        Assert.Same(builder, builder.Property("int", "Count", p =>
        {
            configured = p;
            calls++;
            p.Public().Get();
        }));
        Assert.Same(builder, builder.Add(added));
        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, builder.items[1].Target);
        Assert.Same(added, builder.items[2].Target);
        OutputAssert.Equal(keyword + " Outer\n{\n    int First { get; set; }\n\n    public int Count { get; }\n\n    int Added { get; set; }\n}\n",
            builder.ToString());

        configured.Private();
        added.Arrow("2");
        var expected = keyword + " Outer\n{\n    int First { get; set; }\n\n    private int Count { get; }\n\n    int Added => 2;\n}\n";
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void MixedBodyItems_PreserveOrderSpacingAndNestedPropertyIndentation()
    {
        var builder = CSharp.Class("Outer").Property("int", "First")
            .Field("int", "count")
            .Method("Run", m => m.Body())
            .Class("Nested", c => c.Property("string", "Name"))
            .Raw("// first\r\n// second")
            .Add(CSharp.Property("int", "Last"));

        OutputAssert.Equal(
            "class Outer\n{\n    int First { get; set; }\n\n    int count;\n\n    void Run()\n    {\n    }\n\n" +
            "    class Nested\n    {\n        string Name { get; set; }\n    }\n\n" +
            "    // first\n    // second\n\n    int Last { get; set; }\n}\n", builder.ToString());
    }

    [Fact]
    public void PersonExample_RendersInitInitializerAndArrow()
    {
        var builder = CSharp.Class("Person", c => c.Public()
            .Property("string", "Name", p => p.Public().Init().Get().Initializer("\"\""))
            .Property("int", "Age", p => p.Public().Arrow("0")));

        OutputAssert.Equal("public class Person\n{\n    public string Name { get; init; } = \"\";\n\n    public int Age => 0;\n}\n",
            builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("1Count")]
    [InlineData("Count.Value")]
    [InlineData("Count Value")]
    [InlineData("Count-Value")]
    [InlineData("@@Count")]
    public void InvalidNames_ThrowAtCallTimeBeforeConfiguringOrAdding(string name)
    {
        var configured = false;
        Assert.Throws<ArgumentException>("name", () => CSharp.Property("int", name));
        Assert.Throws<ArgumentException>("name", () => CSharp.Property("int", name, _ => configured = true));
        Assert.False(configured);

        AssertInvalidName(CSharp.Class("Outer"), name);
        AssertInvalidName(CSharp.Struct("Outer"), name);
        AssertInvalidName(CSharp.Interface("Outer"), name);
        AssertInvalidName(CSharp.Record("Outer"), name);
        AssertInvalidName(CSharp.RecordStruct("Outer"), name);
    }

    private static void AssertInvalidName<TSelf>(TypeDeclarationBuilder<TSelf> builder, string name)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        var configured = false;
        var before = builder.ToString();
        Assert.Throws<ArgumentException>("name", () => builder.Property("int", name));
        Assert.Throws<ArgumentException>("name", () => builder.Property("int", name, _ => configured = true));
        Assert.False(configured);
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void NullArguments_ThrowWithParameterNameBeforeConfiguringOrAdding()
    {
        var configured = false;
        Assert.Throws<ArgumentNullException>("type", () => CSharp.Property(null!, "Count"));
        Assert.Throws<ArgumentNullException>("type", () => CSharp.Property(null!, "Count", _ => configured = true));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Property("int", null!));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Property("int", null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Property("int", "Count", null!));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Property(null!, null!, null!));
        Assert.False(configured);
        var property = CSharp.Property("int", "Count").Get().Initializer("1");
        Assert.Throws<ArgumentNullException>("expression", () => property.Initializer(null!));
        Assert.Throws<ArgumentNullException>("expression", () => property.Arrow(null!));
        OutputAssert.Equal("int Count { get; } = 1;\n", property.ToString());

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
        Assert.Throws<ArgumentNullException>("type", () => builder.Property(null!, "Count"));
        Assert.Throws<ArgumentNullException>("type", () => builder.Property(null!, "Count", _ => configured = true));
        Assert.Throws<ArgumentNullException>("name", () => builder.Property("int", null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Property("int", null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Property("int", "Count", null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Property(null!, null!, null!));
        Assert.Throws<ArgumentNullException>("propertyBuilder", () => builder.Add((PropertyBuilder)null!));
        Assert.False(configured);
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void FluentMethods_ReturnSamePropertyBuilder()
    {
        var builder = CSharp.Property("int", "Count");

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
        Assert.Same(builder, builder.Arrow("0"));
        Assert.Same(builder, builder.Initializer("1"));
    }

    [Theory]
    [InlineData(typeof(EnumBuilder))]
    [InlineData(typeof(FileBuilder))]
    [InlineData(typeof(NamespaceBuilder))]
    public void UnsupportedContainers_DoNotExposePropertyEntryPoints(Type type)
    {
        Assert.DoesNotContain(type.GetMethods(), method => method.Name == "Property");
        Assert.DoesNotContain(type.GetMethods(), method => method.Name == "Add" &&
            Array.Exists(method.GetParameters(), parameter => parameter.ParameterType == typeof(PropertyBuilder)));
    }

    [Fact]
    public void CompleteFile_RendersPropertiesAndCompiles()
    {
        var file = CSharp.File().AutoGeneratedHeader().NullableEnable().Using("System")
            .Namespace("Example", n => n
                .Class("Values", c => c
                    .Property("string", "Name", p => p.Public().Initializer("\"\""))
                    .Property("int", "Count", p => p.Public().Get())
                    .Property("DateTime", "Now", p => p.Public().Arrow("DateTime.Now"))
                    .Property("int", "class", p => p.Public()))
                .Class("Base", c => c.Abstract()
                    .Property("int", "Value", p => p.Public().Abstract().Get())
                    .Property("int", "Count", p => p.Public().Virtual().Get().Set()))
                .Class("Derived", c => c.Sealed().BaseType("Base")
                    .Property("int", "Value", p => p.Public().Override().Arrow("1"))
                    .Property("int", "Count", p => p.Public().Override().Get().Set()))
                .Interface("IValues", i => i
                    .Property("int", "Value", p => p.Get())
                    .Property("int", "Count", p => p.Get().Set()))
                .Class("Implementation", c => c.BaseType("IValues")
                    .Property("int", "Value", p => p.Public().Get())
                    .Property("int", "Count", p => p.Public()))
                .Record("Item", r => r.Property("int", "Value", p => p.Public().Get().Init()))
                .RecordStruct("ValueItem", r => r.Property("int", "Value", p => p.Public().Get().Init()))
                .Struct("Point", s => s
                    .Property("int", "X", p => p.Public())
                    .Property("int", "Zero", p => p.Public().ReadOnly().Arrow("0")))
                .Class("Constants", c => c.Static()
                    .Property("int", "Zero", p => p.Public().Static().Arrow("0"))));
        const string expected =
            "// <auto-generated/>\n#nullable enable\n\nusing System;\n\nnamespace Example\n{\n" +
            "    class Values\n    {\n        public string Name { get; set; } = \"\";\n\n" +
            "        public int Count { get; }\n\n        public DateTime Now => DateTime.Now;\n\n" +
            "        public int @class { get; set; }\n    }\n\n" +
            "    abstract class Base\n    {\n        public abstract int Value { get; }\n\n" +
            "        public virtual int Count { get; set; }\n    }\n\n" +
            "    sealed class Derived : Base\n    {\n        public override int Value => 1;\n\n" +
            "        public override int Count { get; set; }\n    }\n\n" +
            "    interface IValues\n    {\n        int Value { get; }\n\n        int Count { get; set; }\n    }\n\n" +
            "    class Implementation : IValues\n    {\n        public int Value { get; }\n\n" +
            "        public int Count { get; set; }\n    }\n\n" +
            "    record Item\n    {\n        public int Value { get; init; }\n    }\n\n" +
            "    record struct ValueItem\n    {\n        public int Value { get; init; }\n    }\n\n" +
            "    struct Point\n    {\n        public int X { get; set; }\n\n        public readonly int Zero => 0;\n    }\n\n" +
            "    static class Constants\n    {\n        public static int Zero => 0;\n    }\n}\n";

        OutputAssert.Equal(expected, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }
}
