using System;
using Xunit;

namespace Codrio.Tests;

public class MethodBuilderTests
{
    [Theory]
    [InlineData("Run", "void Run();\n")]
    [InlineData("class", "void @class();\n")]
    [InlineData("@class", "void @class();\n")]
    public void ToString_RendersBodilessMethod(string name, string expected)
    {
        var builder = CSharp.Method(name);

        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
    }

    [Theory]
    [InlineData("int")]
    [InlineData("global::System.Collections.Generic.List<string>")]
    [InlineData("unvalidated type")]
    [InlineData("")]
    public void Returns_WritesTypeVerbatimAndLastCallWins(string type)
    {
        var builder = CSharp.Method("Run").Returns("bool").Returns(type);

        OutputAssert.Equal(type + " Run();\n", builder.ToString());
    }

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected")]
    [InlineData("private")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public void Accessibility_RendersSelectedValueAndLastCallWins(string accessibility)
    {
        var builder = CSharp.Method("Run");
        OutputAssert.Equal("void Run();\n", builder.ToString());
        builder.Public().Internal();

        switch (accessibility)
        {
            case "public": builder.Public(); break;
            case "internal": builder.Internal(); break;
            case "protected": builder.Protected(); break;
            case "private": builder.Private(); break;
            case "protected internal": builder.ProtectedInternal(); break;
            case "private protected": builder.PrivateProtected(); break;
        }

        OutputAssert.Equal(accessibility + " void Run();\n", builder.ToString());
    }

    [Theory]
    [InlineData("static")]
    [InlineData("new")]
    [InlineData("virtual")]
    [InlineData("abstract")]
    [InlineData("sealed")]
    [InlineData("override")]
    [InlineData("readonly")]
    [InlineData("partial")]
    public void Modifiers_RenderIndividually(string modifier)
    {
        var builder = CSharp.Method("Run");
        switch (modifier)
        {
            case "static": builder.Static(); break;
            case "new": builder.New(); break;
            case "virtual": builder.Virtual(); break;
            case "abstract": builder.Abstract(); break;
            case "sealed": builder.Sealed(); break;
            case "override": builder.Override(); break;
            case "readonly": builder.ReadOnly(); break;
            case "partial": builder.Partial(); break;
        }

        OutputAssert.Equal(modifier + " void Run();\n", builder.ToString());
    }

    [Fact]
    public void Modifiers_RenderInFixedOrderOnceWithoutValidatingCombinations()
    {
        var builder = CSharp.Method("Run").Partial().ReadOnly().Override().Sealed()
            .Abstract().Virtual().New().Static().Public();
        const string expected = "public static new virtual abstract sealed override readonly partial void Run();\n";

        OutputAssert.Equal(expected, builder.ToString());
        builder.Static().New().Virtual().Abstract().Sealed().Override().ReadOnly().Partial();
        OutputAssert.Equal(expected, builder.ToString());
    }

    [Fact]
    public void Method_InvokesConfigureImmediatelyOnceOnReturnedBuilder()
    {
        MethodBuilder? configured = null;
        var calls = 0;
        var builder = CSharp.Method("OnChanged", m =>
        {
            configured = m;
            calls++;
            m.Partial().Private();
        });

        Assert.Same(builder, configured);
        Assert.Equal(1, calls);
        OutputAssert.Equal("private partial void OnChanged();\n", builder.ToString());
        OutputAssert.Equal("private partial void OnChanged();\n", builder.ToString());
        Assert.Equal(1, calls);
        Assert.NotSame(builder, CSharp.Method("OnChanged"));
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void TypeMethods_ConfigureImmediatelyKeepReferencesAndReturnParent(string keyword)
    {
        switch (keyword)
        {
            case "class": AssertTypeMethods(CSharp.Class("Outer"), keyword); break;
            case "struct": AssertTypeMethods(CSharp.Struct("Outer"), keyword); break;
            case "interface": AssertTypeMethods(CSharp.Interface("Outer"), keyword); break;
            case "record": AssertTypeMethods(CSharp.Record("Outer"), keyword); break;
            case "record struct": AssertTypeMethods(CSharp.RecordStruct("Outer"), keyword); break;
        }
    }

    private static void AssertTypeMethods<TSelf>(TypeDeclarationBuilder<TSelf> builder, string keyword)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        MethodBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Method("Added");

        Assert.Same(builder, builder.Method("Run"));
        Assert.Same(builder, builder.Method("Count", m =>
        {
            configured = m;
            calls++;
            m.Returns("int");
        }));
        Assert.Same(builder, builder.Add(added));
        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, builder.items[1].Target);
        Assert.Same(added, builder.items[2].Target);
        OutputAssert.Equal(keyword + " Outer\n{\n    void Run();\n\n    int Count();\n\n    void Added();\n}\n", builder.ToString());

        configured.Returns("long");
        added.Public().Returns("string");
        var expected = keyword + " Outer\n{\n    void Run();\n\n    long Count();\n\n    public string Added();\n}\n";
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void MixedBodyItems_PreserveOrderSpacingAndNestedMethodIndentation()
    {
        var builder = CSharp.Class("Outer").Method("First")
            .Class("Nested", c => c.Method("Inside"))
            .Raw("// first\r\n// second")
            .Add(CSharp.Method("Last").Returns("int"));

        OutputAssert.Equal(
            "class Outer\n{\n    void First();\n\n    class Nested\n    {\n        void Inside();\n    }\n\n    // first\n    // second\n\n    int Last();\n}\n",
            builder.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("1Run")]
    [InlineData("Run.Value")]
    [InlineData("Run Value")]
    [InlineData("Run-Value")]
    [InlineData("@@Run")]
    public void InvalidNames_ThrowAtCallTimeBeforeConfiguringOrAdding(string name)
    {
        var configured = false;
        Assert.Throws<ArgumentException>("name", () => CSharp.Method(name));
        Assert.Throws<ArgumentException>("name", () => CSharp.Method(name, _ => configured = true));
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
        Assert.Throws<ArgumentException>("name", () => builder.Method(name));
        Assert.Throws<ArgumentException>("name", () => builder.Method(name, _ => configured = true));
        Assert.False(configured);
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void NullArguments_ThrowWithParameterNameBeforeConfiguringOrAdding()
    {
        var configured = false;
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Method(null!));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Method(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Method("Run", null!));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Method(null!, null!));
        Assert.False(configured);
        var method = CSharp.Method("Run").Returns("int");
        Assert.Throws<ArgumentNullException>("type", () => method.Returns(null!));
        OutputAssert.Equal("int Run();\n", method.ToString());

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
        Assert.Throws<ArgumentNullException>("name", () => builder.Method(null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Method(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Method("Run", null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Method(null!, null!));
        Assert.Throws<ArgumentNullException>("methodBuilder", () => builder.Add((MethodBuilder)null!));
        Assert.False(configured);
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void FluentMethods_ReturnSameMethodBuilder()
    {
        var builder = CSharp.Method("Run");

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
        Assert.Same(builder, builder.Partial());
        Assert.Same(builder, builder.Returns("int"));
    }

    [Fact]
    public void UnsupportedContainers_DoNotExposeMethodEntryPoints()
    {
        Assert.DoesNotContain(typeof(EnumBuilder).GetMethods(), method => method.Name == "Method");
        Assert.DoesNotContain(typeof(FileBuilder).GetMethods(), method => method.Name == "Method");
        Assert.DoesNotContain(typeof(NamespaceBuilder).GetMethods(), method => method.Name == "Method");
    }

    [Fact]
    public void CompleteFile_RendersMethodDeclarationsAndCompiles()
    {
        var file = CSharp.File().AutoGeneratedHeader().NullableEnable().Using("System")
            .Namespace("Example", n => n
                .Interface("IRunner", i => i.Public().Method("Run")
                    .Method("Count", m => m.Returns("int")).Method("class", m => m.Returns("IDisposable")))
                .Class("Runner", c => c.Abstract()
                    .Method("Run", m => m.Public().Abstract())
                    .Method("Count", m => m.Protected().Abstract().Returns("int")))
                .Class("Observer", c => c.Partial().Method("OnChanged", m => m.Partial()))
                .Struct("ValueObserver", s => s.Partial().Method("OnChanged", m => m.Partial()))
                .Record("Shape", r => r.Abstract().Method("Area", m => m.Public().Abstract().Returns("double"))));
        const string expected =
            "// <auto-generated/>\n#nullable enable\n\nusing System;\n\nnamespace Example\n{\n" +
            "    public interface IRunner\n    {\n        void Run();\n\n        int Count();\n\n        IDisposable @class();\n    }\n\n" +
            "    abstract class Runner\n    {\n        public abstract void Run();\n\n        protected abstract int Count();\n    }\n\n" +
            "    partial class Observer\n    {\n        partial void OnChanged();\n    }\n\n" +
            "    partial struct ValueObserver\n    {\n        partial void OnChanged();\n    }\n\n" +
            "    abstract record Shape\n    {\n        public abstract double Area();\n    }\n}\n";

        OutputAssert.Equal(expected, file.ToString());
        OutputAssert.Equal(expected, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }
}
