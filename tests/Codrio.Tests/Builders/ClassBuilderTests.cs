using System;
using Xunit;

namespace Codrio.Tests;

public class ClassBuilderTests
{
    [Theory]
    [InlineData("Foo", "class Foo\n{\n}\n")]
    [InlineData("class", "class @class\n{\n}\n")]
    [InlineData("@class", "class @class\n{\n}\n")]
    public void ToString_RendersEmptyClass(string name, string expected)
    {
        var builder = CSharp.Class(name);

        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Fact]
    public void Class_InvokesConfigureImmediatelyOnReturnedBuilder()
    {
        ClassBuilder? configured = null;
        var calls = 0;

        var builder = CSharp.Class("Foo", c =>
        {
            configured = c;
            calls++;
        });

        Assert.Same(builder, configured);
        Assert.Equal(1, calls);
        OutputAssert.Equal("class Foo\n{\n}\n", builder.ToString());
        OutputAssert.Equal("class Foo\n{\n}\n", builder.ToString());
        Assert.Equal(1, calls);
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
        Assert.Throws<ArgumentException>("name", () => CSharp.Class(name));
        var configured = false;
        Assert.Throws<ArgumentException>("name", () => CSharp.Class(name, _ => configured = true));
        Assert.False(configured);
    }

    [Fact]
    public void Class_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Class(null!));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Class(null!, _ => { }));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Class("Foo", null!));
    }
}
