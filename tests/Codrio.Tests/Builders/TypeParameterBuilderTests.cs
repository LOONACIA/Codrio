using System;
using Xunit;

namespace Codrio.Tests;

public class TypeParameterBuilderTests
{
    [Fact]
    public void ToString_RendersConstraintFragmentWithoutLineEnding()
    {
        TypeParameterBuilder? parameter = null;
        CSharp.Class("Repo").TypeParameter("T", t => parameter = t);

        Assert.NotNull(parameter);
        OutputAssert.Equal("", parameter.ToString());
        parameter.Constraint("class");
        OutputAssert.Equal("where T : class", parameter.ToString());
        parameter.Constraint("new()");
        OutputAssert.Equal("where T : class, new()", parameter.ToString());
    }

    [Fact]
    public void TypeParameter_ConfiguresImmediatelyOnceAndRetainsBuilder()
    {
        TypeParameterBuilder? parameter = null;
        var calls = 0;
        var builder = CSharp.Class("Repo").TypeParameter("T", t =>
        {
            parameter = t;
            calls++;
            t.Constraint("class");
        });

        Assert.NotNull(parameter);
        Assert.Equal(1, calls);
        OutputAssert.Equal("class Repo<T> where T : class\n{\n}\n", builder.ToString());
        Assert.Same(parameter, parameter.Constraint("new()"));
        OutputAssert.Equal("class Repo<T> where T : class, new()\n{\n}\n", builder.ToString());
        OutputAssert.Equal("class Repo<T> where T : class, new()\n{\n}\n", builder.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void ToString_EscapesKeywordName()
    {
        CSharp.Class("Repo").TypeParameter("class", t =>
        {
            t.Constraint("class");
            OutputAssert.Equal("where @class : class", t.ToString());
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a constraint")]
    [InlineData(" global::Example.IValue ")]
    public void Constraint_PreservesUnvalidatedText(string constraint)
    {
        var builder = CSharp.Class("Repo").TypeParameter("T", t =>
        {
            t.Constraint(constraint);
            OutputAssert.Equal("where T : " + constraint, t.ToString());
        });

        OutputAssert.Equal("class Repo<T> where T : " + constraint + "\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void Constraint_RejectsNullArgument()
    {
        CSharp.Class("Repo").TypeParameter("T", t =>
        {
            Assert.Throws<ArgumentNullException>("constraint", () => t.Constraint(null!));
            OutputAssert.Equal("", t.ToString());
        });
    }
}
