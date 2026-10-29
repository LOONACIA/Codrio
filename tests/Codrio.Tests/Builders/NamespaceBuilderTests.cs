using System;
using Xunit;

namespace Codrio.Tests;

public class NamespaceBuilderTests
{
    [Fact]
    public void EmptyFileScopedNamespace_RendersSemicolon()
    {
        OutputAssert.Equal("namespace A;\n", CSharp.FileScopedNamespace("A").ToString());
    }

    [Fact]
    public void FileScopedNamespace_InvokesConfigureImmediatelyAndRendersClassesWithoutIndentation()
    {
        NamespaceBuilder? configured = null;
        var calls = 0;
        var builder = CSharp.FileScopedNamespace("A.B", n =>
        {
            configured = n;
            calls++;
            n.Class("C").Class("D");
        });
        const string expected = "namespace A.B;\n\nclass C\n{\n}\n\nclass D\n{\n}\n";

        Assert.Same(builder, configured);
        Assert.Equal(1, calls);
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("A.B")]
    [InlineData("not a namespace!")]
    [InlineData("class")]
    public void FileScopedNamespace_WritesNameWithoutValidation(string name)
    {
        OutputAssert.Equal("namespace " + name + ";\n", CSharp.FileScopedNamespace(name).ToString());
    }

    [Fact]
    public void FileScopedNamespaceNullArguments_ThrowWithParameterName()
    {
        var configured = false;

        Assert.Throws<ArgumentNullException>("name", () => CSharp.FileScopedNamespace(null!));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.FileScopedNamespace(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.FileScopedNamespace("A", null!));
        Assert.False(configured);
    }

    [Fact]
    public void EmptyNamespace_RendersEmptyBlock()
    {
        OutputAssert.Equal("namespace A\n{\n}\n", CSharp.Namespace("A").ToString());
    }

    [Fact]
    public void Classes_RenderInOrderWithIndentationAndOneBlankLine()
    {
        var builder = CSharp.Namespace("A.B", n => n.Class("C").Class("D"));
        const string expected = "namespace A.B\n{\n    class C\n    {\n    }\n\n    class D\n    {\n    }\n}\n";

        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("A.B")]
    [InlineData("not a namespace!")]
    [InlineData("class")]
    public void Namespace_WritesNameWithoutValidation(string name)
    {
        OutputAssert.Equal("namespace " + name + "\n{\n}\n", CSharp.Namespace(name).ToString());
    }

    [Fact]
    public void Namespace_InvokesConfigureImmediatelyOnReturnedBuilder()
    {
        NamespaceBuilder? configured = null;
        var calls = 0;
        var builder = CSharp.Namespace("A", n =>
        {
            configured = n;
            calls++;
            n.Class("C");
        });

        Assert.Same(builder, configured);
        Assert.Equal(1, calls);
        OutputAssert.Equal("namespace A\n{\n    class C\n    {\n    }\n}\n", builder.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Class_InvokesConfigureImmediatelyAndAddsClass()
    {
        ClassBuilder? configured = null;
        var calls = 0;
        var builder = CSharp.Namespace("A").Class("C", c =>
        {
            configured = c;
            calls++;
        });

        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        OutputAssert.Equal("class C\n{\n}\n", configured.ToString());
        OutputAssert.Equal("namespace A\n{\n    class C\n    {\n    }\n}\n", builder.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Add_RendersSuppliedClassInOrder()
    {
        var builder = CSharp.Namespace("A").Class("C").Add(CSharp.Class("D"));

        OutputAssert.Equal("namespace A\n{\n    class C\n    {\n    }\n\n    class D\n    {\n    }\n}\n", builder.ToString());
    }

    [Fact]
    public void KeywordClassName_IsEscapedAndCompiles()
    {
        var output = CSharp.Namespace("A").Class("class").ToString();

        OutputAssert.Equal("namespace A\n{\n    class @class\n    {\n    }\n}\n", output);
        CompilationAssert.NoErrors(output);
    }

    [Fact]
    public void FluentMethods_ReturnSameNamespaceBuilder()
    {
        var builder = CSharp.Namespace("A");

        Assert.Same(builder, builder.Class("C"));
        Assert.Same(builder, builder.Class("D", _ => { }));
        Assert.Same(builder, builder.Add(CSharp.Class("E")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("1Foo")]
    [InlineData("Foo.Bar")]
    [InlineData("Foo Bar")]
    [InlineData("Foo-Bar")]
    [InlineData("@@Foo")]
    public void Class_RejectsInvalidNameAtCallTime(string name)
    {
        var builder = CSharp.Namespace("A");
        var configured = false;

        Assert.Throws<ArgumentException>("name", () => builder.Class(name));
        Assert.Throws<ArgumentException>("name", () => builder.Class(name, _ => configured = true));
        Assert.False(configured);
        OutputAssert.Equal("namespace A\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void NullArguments_ThrowWithParameterName()
    {
        var builder = CSharp.Namespace("A");

        Assert.Throws<ArgumentNullException>("name", () => CSharp.Namespace(null!));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Namespace(null!, _ => { }));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Namespace("A", null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Class(null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Class(null!, _ => { }));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Class("C", null!));
        Assert.Throws<ArgumentNullException>("classBuilder", () => builder.Add((ClassBuilder)null!));
        OutputAssert.Equal("namespace A\n{\n}\n", builder.ToString());
    }
}
