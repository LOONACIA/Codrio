using System;
using Xunit;

namespace Codrio.Tests;

public class DelegateBuilderTests
{
    [Fact]
    public void DefaultDelegate_RendersVoidAndEmptyParameters()
    {
        OutputAssert.Equal("delegate void Handler();\n", CSharp.Delegate("Handler").ToString());
        OutputAssert.Equal("delegate void Handler();\n", CSharp.Delegate("Handler", _ => { }).ToString());
    }

    [Theory]
    [InlineData("int")]
    [InlineData("global::System.Collections.Generic.List<string>")]
    [InlineData("unvalidated type")]
    [InlineData("")]
    public void Returns_WritesTypeVerbatimAndLastCallWins(string type)
    {
        OutputAssert.Equal("delegate " + type + " Handler();\n",
            CSharp.Delegate("Handler").Returns("bool").Returns(type).ToString());
    }

    [Fact]
    public void Parameters_RenderInOrderWithModifiersAndDefaults()
    {
        var builder = CSharp.Delegate("Handler")
            .Parameter("string", "text")
            .Parameter("int", "count", p => p.Ref())
            .Parameter("int", "value", p => p.Out())
            .Parameter("int", "delta", p => p.In())
            .Parameter("bool", "enabled", p => p.Default("false"))
            .Parameter("object[]", "args", p => p.Params());

        OutputAssert.Equal("delegate void Handler(string text, ref int count, out int value, in int delta, bool enabled = false, params object[] args);\n",
            builder.ToString());
    }

    [Fact]
    public void TypeParameters_RenderInOrderWithOnlyNonemptyConstraints()
    {
        var builder = CSharp.Delegate("Handler").TypeParameter("T")
            .TypeParameter("TValue", t => t.Constraint("class").Constraint("new()"))
            .TypeParameter("TUnused", _ => { })
            .TypeParameter("TKey", t => t.Constraint("struct"));

        OutputAssert.Equal("delegate void Handler<T, TValue, TUnused, TKey>() where TValue : class, new() where TKey : struct;\n",
            builder.ToString());
    }

    [Fact]
    public void Declaration_RendersInSyntaxOrderRegardlessOfCallOrder()
    {
        var first = CSharp.Delegate("Converter", d => d.Public().Returns("TOutput")
            .TypeParameter("TInput").TypeParameter("TOutput", t => t.Constraint("class"))
            .Parameter("TInput", "input"));
        var second = CSharp.Delegate("Converter").Parameter("TInput", "input")
            .TypeParameter("TInput").Returns("TOutput")
            .TypeParameter("TOutput", t => t.Constraint("class")).Public();
        const string expected = "public delegate TOutput Converter<TInput, TOutput>(TInput input) where TOutput : class;\n";

        OutputAssert.Equal(expected, first.ToString());
        OutputAssert.Equal(expected, second.ToString());
    }

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected")]
    [InlineData("private")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public void Accessibility_LastCallWins(string accessibility)
    {
        var builder = CSharp.Delegate("Handler").Public().Internal();
        switch (accessibility)
        {
            case "public": builder.Public(); break;
            case "internal": builder.Internal(); break;
            case "protected": builder.Protected(); break;
            case "private": builder.Private(); break;
            case "protected internal": builder.ProtectedInternal(); break;
            case "private protected": builder.PrivateProtected(); break;
        }

        OutputAssert.Equal(accessibility + " delegate void Handler();\n", builder.ToString());
    }

    [Fact]
    public void Modifiers_RenderInFixedOrderOnceWithoutValidatingCombinations()
    {
        var builder = CSharp.Delegate("Handler").ReadOnly().Override().Sealed()
            .Abstract().Virtual().New().Static().Public();
        const string expected = "public static new virtual abstract sealed override readonly delegate void Handler();\n";

        OutputAssert.Equal(expected, builder.ToString());
        builder.Static().New().Virtual().Abstract().Sealed().Override().ReadOnly();
        OutputAssert.Equal(expected, builder.ToString());
    }

    [Fact]
    public void Delegate_InvokesConfigureImmediatelyOnceOnReturnedBuilder()
    {
        DelegateBuilder? configured = null;
        var calls = 0;
        var builder = CSharp.Delegate("Handler", d =>
        {
            configured = d;
            calls++;
            d.Public();
        });

        Assert.Same(builder, configured);
        Assert.Equal(1, calls);
        OutputAssert.Equal("public delegate void Handler();\n", builder.ToString());
        OutputAssert.Equal("public delegate void Handler();\n", builder.ToString());
        Assert.Equal(1, calls);
        Assert.NotSame(builder, CSharp.Delegate("Handler"));
    }

    [Fact]
    public void ParameterConfiguration_RunsImmediatelyOnceAndRetainsBuilders()
    {
        var builder = CSharp.Delegate("Handler");
        ParameterBuilder? parameter = null;
        TypeParameterBuilder? typeParameter = null;
        var parameterCalls = 0;
        var typeParameterCalls = 0;
        builder.Parameter("int", "value", p =>
        {
            parameter = p;
            parameterCalls++;
            OutputAssert.Equal("delegate void Handler();\n", builder.ToString());
        });
        builder.TypeParameter("T", t =>
        {
            typeParameter = t;
            typeParameterCalls++;
            OutputAssert.Equal("delegate void Handler(int value);\n", builder.ToString());
        });

        Assert.NotNull(parameter);
        Assert.NotNull(typeParameter);
        Assert.Same(parameter, builder.parameters[0]);
        Assert.Same(typeParameter, builder.typeParameters[0]);
        Assert.Equal(1, parameterCalls);
        Assert.Equal(1, typeParameterCalls);
        OutputAssert.Equal("delegate void Handler<T>(int value);\n", builder.ToString());
        parameter.Default("7");
        typeParameter.Constraint("class");
        OutputAssert.Equal("delegate void Handler<T>(int value = 7) where T : class;\n", builder.ToString());
        OutputAssert.Equal("delegate void Handler<T>(int value = 7) where T : class;\n", builder.ToString());
        Assert.Equal(1, parameterCalls);
        Assert.Equal(1, typeParameterCalls);
    }

    [Fact]
    public void FileDelegates_ConfigureImmediatelyKeepReferencesAndReturnParent()
    {
        var builder = CSharp.File();
        DelegateBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Delegate("Added");
        Assert.Same(builder, builder.Delegate("Handler"));
        Assert.Same(builder, builder.Delegate("Count", d =>
        {
            configured = d;
            calls++;
            d.Returns("int");
        }));
        Assert.Same(builder, builder.Add(added));

        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, builder.items[1].Target);
        Assert.Same(added, builder.items[2].Target);
        OutputAssert.Equal("delegate void Handler();\n\ndelegate int Count();\n\ndelegate void Added();\n", builder.ToString());
        configured.Returns("long");
        added.Public();
        OutputAssert.Equal("delegate void Handler();\n\ndelegate long Count();\n\npublic delegate void Added();\n", builder.ToString());
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NamespaceDelegates_ConfigureImmediatelyKeepReferencesAndReturnParent(bool fileScoped)
    {
        var builder = fileScoped ? CSharp.FileScopedNamespace("App") : CSharp.Namespace("App");
        DelegateBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Delegate("Added");
        Assert.Same(builder, builder.Delegate("Handler"));
        Assert.Same(builder, builder.Delegate("Count", d =>
        {
            configured = d;
            calls++;
            d.Returns("int");
        }));
        Assert.Same(builder, builder.Add(added));

        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, builder.items[1].Target);
        Assert.Same(added, builder.items[2].Target);
        OutputAssert.Equal(fileScoped
            ? "namespace App;\n\ndelegate void Handler();\n\ndelegate int Count();\n\ndelegate void Added();\n"
            : "namespace App\n{\n    delegate void Handler();\n\n    delegate int Count();\n\n    delegate void Added();\n}\n", builder.ToString());
        configured.Returns("long");
        added.Public();
        OutputAssert.Equal(fileScoped
            ? "namespace App;\n\ndelegate void Handler();\n\ndelegate long Count();\n\npublic delegate void Added();\n"
            : "namespace App\n{\n    delegate void Handler();\n\n    delegate long Count();\n\n    public delegate void Added();\n}\n", builder.ToString());
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void TypeDelegates_ConfigureImmediatelyKeepReferencesAndReturnParent(string keyword)
    {
        switch (keyword)
        {
            case "class": AssertTypeDelegates(CSharp.Class("Outer"), keyword); break;
            case "struct": AssertTypeDelegates(CSharp.Struct("Outer"), keyword); break;
            case "interface": AssertTypeDelegates(CSharp.Interface("Outer"), keyword); break;
            case "record": AssertTypeDelegates(CSharp.Record("Outer"), keyword); break;
            case "record struct": AssertTypeDelegates(CSharp.RecordStruct("Outer"), keyword); break;
        }
    }

    private static void AssertTypeDelegates<TSelf>(TypeDeclarationBuilder<TSelf> builder, string keyword)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        DelegateBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Delegate("Added");
        Assert.Same(builder, builder.Delegate("Handler"));
        Assert.Same(builder, builder.Delegate("Count", d =>
        {
            configured = d;
            calls++;
            d.Returns("int");
        }));
        Assert.Same(builder, builder.Add(added));

        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, builder.items[1].Target);
        Assert.Same(added, builder.items[2].Target);
        OutputAssert.Equal(keyword + " Outer\n{\n    delegate void Handler();\n\n    delegate int Count();\n\n    delegate void Added();\n}\n", builder.ToString());
        configured.Returns("long");
        added.Public();
        OutputAssert.Equal(keyword + " Outer\n{\n    delegate void Handler();\n\n    delegate long Count();\n\n    public delegate void Added();\n}\n", builder.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void MixedItems_PreserveOrderSpacingAndNestedIndentation()
    {
        var file = CSharp.File().Delegate("First").Class("Outer", c => c.Delegate("Second")
                .Class("Nested", n => n.Delegate("Inside")).Raw("// body").Add(CSharp.Delegate("Third")))
            .Raw("// file").Add(CSharp.Delegate("Last"));

        OutputAssert.Equal("delegate void First();\n\nclass Outer\n{\n    delegate void Second();\n\n" +
            "    class Nested\n    {\n        delegate void Inside();\n    }\n\n    // body\n\n    delegate void Third();\n}\n\n" +
            "// file\n\ndelegate void Last();\n", file.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NamespaceMixedItems_PreserveOrderAndSpacing(bool fileScoped)
    {
        var builder = fileScoped ? CSharp.FileScopedNamespace("App") : CSharp.Namespace("App");
        builder.Delegate("Handler", d => d.Public()).Class("Worker", c => c.Raw("// body")).Add(CSharp.Delegate("Last"));

        OutputAssert.Equal(fileScoped
            ? "namespace App;\n\npublic delegate void Handler();\n\nclass Worker\n{\n    // body\n}\n\ndelegate void Last();\n"
            : "namespace App\n{\n    public delegate void Handler();\n\n    class Worker\n    {\n        // body\n    }\n\n    delegate void Last();\n}\n", builder.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RecordWithOnlyDelegate_RendersBlock(bool isStruct)
    {
        var builder = isStruct ? CSharp.RecordStruct("Outer") : CSharp.Record("Outer");
        builder.Delegate("Handler");

        OutputAssert.Equal((isStruct ? "record struct" : "record") + " Outer\n{\n    delegate void Handler();\n}\n", builder.ToString());
    }

    [Theory]
    [InlineData("delegate")]
    [InlineData("@delegate")]
    public void Names_EscapeKeywords(string name)
    {
        OutputAssert.Equal("delegate void @delegate<@class>(int @event) where @class : class;\n",
            CSharp.Delegate(name).Parameter("int", "event").TypeParameter("class", t => t.Constraint("class")).ToString());
        OutputAssert.Equal("delegate void @delegate<@class>(int @event = 0);\n",
            CSharp.Delegate(name).Parameter("int", "@event", p => p.Default("0")).TypeParameter("@class").ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("1Handler")]
    [InlineData("a.b")]
    [InlineData("a b")]
    [InlineData("a-b")]
    [InlineData("@@Handler")]
    public void InvalidNames_ThrowAtCallTimeBeforeConfiguringOrAdding(string name)
    {
        var configured = false;
        Assert.Throws<ArgumentException>("name", () => CSharp.Delegate(name));
        Assert.Throws<ArgumentException>("name", () => CSharp.Delegate(name, _ => configured = true));
        var builder = CSharp.Delegate("Handler");
        Assert.Throws<ArgumentException>("name", () => builder.Parameter("int", name));
        Assert.Throws<ArgumentException>("name", () => builder.Parameter("int", name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => builder.TypeParameter(name));
        Assert.Throws<ArgumentException>("name", () => builder.TypeParameter(name, _ => configured = true));
        Assert.False(configured);
        OutputAssert.Equal("delegate void Handler();\n", builder.ToString());

        AssertInvalidTypeName(CSharp.Class("Outer"), name);
        AssertInvalidTypeName(CSharp.Struct("Outer"), name);
        AssertInvalidTypeName(CSharp.Interface("Outer"), name);
        AssertInvalidTypeName(CSharp.Record("Outer"), name);
        AssertInvalidTypeName(CSharp.RecordStruct("Outer"), name);
        var file = CSharp.File();
        Assert.Throws<ArgumentException>("name", () => file.Delegate(name));
        Assert.Throws<ArgumentException>("name", () => file.Delegate(name, _ => configured = true));
        OutputAssert.Equal("", file.ToString());
        foreach (var ns in new[] { CSharp.Namespace("App"), CSharp.FileScopedNamespace("App") })
        {
            var before = ns.ToString();
            Assert.Throws<ArgumentException>("name", () => ns.Delegate(name));
            Assert.Throws<ArgumentException>("name", () => ns.Delegate(name, _ => configured = true));
            OutputAssert.Equal(before, ns.ToString());
        }

        Assert.False(configured);
    }

    private static void AssertInvalidTypeName<TSelf>(TypeDeclarationBuilder<TSelf> builder, string name)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        var configured = false;
        var before = builder.ToString();
        Assert.Throws<ArgumentException>("name", () => builder.Delegate(name));
        Assert.Throws<ArgumentException>("name", () => builder.Delegate(name, _ => configured = true));
        Assert.False(configured);
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void NullArguments_ThrowWithParameterNameBeforeConfiguringOrAdding()
    {
        var configured = false;
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Delegate(null!));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Delegate(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Delegate("Handler", null!));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Delegate(null!, null!));
        var builder = CSharp.Delegate("Handler").Returns("int");
        Assert.Throws<ArgumentNullException>("type", () => builder.Returns(null!));
        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter(null!, "value"));
        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter(null!, "value", _ => configured = true));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter("int", null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter("int", null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Parameter("int", "value", null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Parameter(null!, null!, null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.TypeParameter(null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.TypeParameter(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.TypeParameter("T", null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.TypeParameter(null!, null!));
        Assert.False(configured);
        OutputAssert.Equal("delegate int Handler();\n", builder.ToString());

        AssertNullTypeArguments(CSharp.Class("Outer"));
        AssertNullTypeArguments(CSharp.Struct("Outer"));
        AssertNullTypeArguments(CSharp.Interface("Outer"));
        AssertNullTypeArguments(CSharp.Record("Outer"));
        AssertNullTypeArguments(CSharp.RecordStruct("Outer"));
        var file = CSharp.File();
        Assert.Throws<ArgumentNullException>("name", () => file.Delegate(null!));
        Assert.Throws<ArgumentNullException>("name", () => file.Delegate(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => file.Delegate("Handler", null!));
        Assert.Throws<ArgumentNullException>("configure", () => file.Delegate(null!, null!));
        Assert.Throws<ArgumentNullException>("delegateBuilder", () => file.Add((DelegateBuilder)null!));
        OutputAssert.Equal("", file.ToString());
        foreach (var ns in new[] { CSharp.Namespace("App"), CSharp.FileScopedNamespace("App") })
        {
            var before = ns.ToString();
            Assert.Throws<ArgumentNullException>("name", () => ns.Delegate(null!));
            Assert.Throws<ArgumentNullException>("name", () => ns.Delegate(null!, _ => configured = true));
            Assert.Throws<ArgumentNullException>("configure", () => ns.Delegate("Handler", null!));
            Assert.Throws<ArgumentNullException>("configure", () => ns.Delegate(null!, null!));
            Assert.Throws<ArgumentNullException>("delegateBuilder", () => ns.Add((DelegateBuilder)null!));
            OutputAssert.Equal(before, ns.ToString());
        }

        Assert.False(configured);
    }

    private static void AssertNullTypeArguments<TSelf>(TypeDeclarationBuilder<TSelf> builder)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        var configured = false;
        var before = builder.ToString();
        Assert.Throws<ArgumentNullException>("name", () => builder.Delegate(null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Delegate(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Delegate("Handler", null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Delegate(null!, null!));
        Assert.Throws<ArgumentNullException>("delegateBuilder", () => builder.Add((DelegateBuilder)null!));
        Assert.False(configured);
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void FluentMethods_ReturnSameDelegateBuilder()
    {
        var builder = CSharp.Delegate("Handler");
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
        Assert.Same(builder, builder.Returns("int"));
        Assert.Same(builder, builder.Parameter("int", "value"));
        Assert.Same(builder, builder.Parameter("int", "other", _ => { }));
        Assert.Same(builder, builder.TypeParameter("T"));
        Assert.Same(builder, builder.TypeParameter("U", _ => { }));
    }

    [Fact]
    public void Enum_DoesNotExposeDelegateEntryPoints()
    {
        Assert.DoesNotContain(typeof(EnumBuilder).GetMethods(), method => method.Name == "Delegate");
        Assert.DoesNotContain(typeof(EnumBuilder).GetMethods(), method => method.Name == "Add" &&
            Array.Exists(method.GetParameters(), parameter => parameter.ParameterType == typeof(DelegateBuilder)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompleteFile_RendersDelegatesAndCompiles(bool fileScoped)
    {
        var file = CSharp.File().AutoGeneratedHeader().NullableEnable().Using("System");
        if (!fileScoped)
        {
            file.Delegate("Notify", d => d.Public().Parameter("string", "message"));
        }

        var ns = fileScoped ? CSharp.FileScopedNamespace("App") : CSharp.Namespace("App");
        ns.Delegate("Converter", d => d.Public().Returns("TOutput").TypeParameter("TInput")
                .TypeParameter("TOutput", t => t.Constraint("class").Constraint("IDisposable"))
                .Parameter("TInput", "input"))
            .Add(CSharp.Delegate("TryParse").Public().Returns("bool").Parameter("string", "text")
                .Parameter("int", "value", p => p.Out()))
            .Delegate("Update", d => d.Public().Parameter("int", "count", p => p.Ref())
                .Parameter("int", "delta", p => p.In()).Parameter("object[]", "args", p => p.Params()))
            .Class("Worker", c => c.Delegate("Handler", d => d.Private().Parameter("string", "message"))
                .Field("Handler?", "handler", f => f.Private()))
            .Interface("IWorker", i => i.Delegate("Handler"))
            .Struct("Value", s => s.Delegate("Handler"))
            .Record("RecordValue", r => r.Delegate("Handler"))
            .RecordStruct("RecordStructValue", r => r.Delegate("Handler"));
        file.Add(ns);
        const string contents =
            "public delegate TOutput Converter<TInput, TOutput>(TInput input) where TOutput : class, IDisposable;\n\n" +
            "public delegate bool TryParse(string text, out int value);\n\n" +
            "public delegate void Update(ref int count, in int delta, params object[] args);\n\n" +
            "class Worker\n{\n    private delegate void Handler(string message);\n\n    private Handler? handler;\n}\n\n" +
            "interface IWorker\n{\n    delegate void Handler();\n}\n\n" +
            "struct Value\n{\n    delegate void Handler();\n}\n\n" +
            "record RecordValue\n{\n    delegate void Handler();\n}\n\n" +
            "record struct RecordStructValue\n{\n    delegate void Handler();\n}\n";
        var expected = "// <auto-generated/>\n#nullable enable\n\nusing System;\n\n" + (fileScoped
            ? "namespace App;\n\n" + contents
            : "public delegate void Notify(string message);\n\nnamespace App\n{\n" +
                "    " + contents.TrimEnd('\n').Replace("\n", "\n    ").Replace("\n    \n", "\n\n") + "\n}\n");

        OutputAssert.Equal(expected, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }
}
