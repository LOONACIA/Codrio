using System;
using Xunit;

namespace Codrio.Tests;

public class AccessorBuilderTests
{
    private static AccessorBuilder CreateAccessor()
    {
        AccessorBuilder? accessor = null;
        CSharp.Property("int", "X", p => p.Get(a => accessor = a));
        Assert.NotNull(accessor);
        return accessor;
    }

    [Fact]
    public void ToString_RendersBodylessAccessor()
    {
        var accessor = CreateAccessor();

        OutputAssert.Equal("get;\n", accessor.ToString());
        OutputAssert.Equal("get;\n", accessor.ToString());
    }

    [Theory]
    [InlineData("count")]
    [InlineData("unvalidated expression")]
    [InlineData("")]
    public void Arrow_RendersExpressionVerbatim(string expression)
    {
        var accessor = CreateAccessor().Arrow(expression);

        OutputAssert.Equal("get => " + expression + ";\n", accessor.ToString());
    }

    [Fact]
    public void Body_RendersEmptyBlock()
    {
        var accessor = CreateAccessor();

        OutputAssert.Equal("get\n{\n}\n", accessor.Body().ToString());
        OutputAssert.Equal("get\n{\n}\n", accessor.Body(_ => { }).ToString());
    }

    [Fact]
    public void Body_RendersLinesAndNestedBlocks()
    {
        var accessor = CreateAccessor().Body(b => b.Line("var value = count;\r\nvalue++;")
            .Block("if (value > 0)", ib => ib.Block("if (ready)", nb => nb.Line("return value;")))
            .Line("return 0;"));

        OutputAssert.Equal("get\n{\n    var value = count;\n    value++;\n" +
            "    if (value > 0)\n    {\n        if (ready)\n        {\n            return value;\n        }\n    }\n" +
            "    return 0;\n}\n", accessor.ToString());
    }

    [Fact]
    public void BodyAndArrow_LastCallWins()
    {
        var accessor = CreateAccessor().Body(b => b.Line("return 1;")).Arrow("2");
        OutputAssert.Equal("get => 2;\n", accessor.ToString());
        accessor.Arrow("3");
        OutputAssert.Equal("get => 3;\n", accessor.ToString());
        accessor.Body(b => b.Line("return 4;"));
        OutputAssert.Equal("get\n{\n    return 4;\n}\n", accessor.ToString());
        accessor.Body(b => b.Line("return 5;"));
        OutputAssert.Equal("get\n{\n    return 5;\n}\n", accessor.ToString());
        accessor.Body();
        OutputAssert.Equal("get\n{\n}\n", accessor.ToString());
        accessor.Arrow("6").Body();
        OutputAssert.Equal("get\n{\n}\n", accessor.ToString());
        accessor.Arrow("7");
        OutputAssert.Equal("get => 7;\n", accessor.ToString());
    }

    [Fact]
    public void MultilineArrow_NormalizesCrLfAndUsesAccessorIndentation()
    {
        AccessorBuilder? accessor = null;
        var property = CSharp.Property("int", "X", p => p.Get(a =>
        {
            accessor = a;
            a.Arrow("first\r\n+ second\n+ third");
        }));

        Assert.NotNull(accessor);
        OutputAssert.Equal("get => first\n+ second\n+ third;\n", accessor.ToString());
        OutputAssert.Equal("int X\n{\n    get => first\n    + second\n    + third;\n}\n", property.ToString());
    }

    [Theory]
    [InlineData("set")]
    [InlineData("init")]
    public void Setter_RendersKeywordForEveryBodyKind(string keyword)
    {
        AccessorBuilder? accessor = null;
        CSharp.Property("int", "X", p =>
        {
            if (keyword == "set")
            {
                p.Set(a => accessor = a);
            }
            else
            {
                p.Init(a => accessor = a);
            }
        });

        Assert.NotNull(accessor);
        OutputAssert.Equal(keyword + ";\n", accessor.ToString());
        OutputAssert.Equal(keyword + " => count = value;\n", accessor.Arrow("count = value").ToString());
        OutputAssert.Equal(keyword + "\n{\n    count = value;\n}\n",
            accessor.Body(b => b.Line("count = value;")).ToString());
    }

    [Fact]
    public void Body_InvokesConfigureImmediatelyOnceAndRetainsFreshBuilder()
    {
        var accessor = CreateAccessor();
        BodyBuilder? body = null;
        var calls = 0;
        accessor.Body(b =>
        {
            body = b;
            calls++;
            OutputAssert.Equal("get;\n", accessor.ToString());
            b.Line("First();");
        });

        Assert.Equal(1, calls);
        Assert.NotNull(body);
        body.Line("Second();");
        const string expected = "get\n{\n    First();\n    Second();\n}\n";
        OutputAssert.Equal(expected, accessor.ToString());
        OutputAssert.Equal(expected, accessor.ToString());
        Assert.Equal(1, calls);
        accessor.Body(b =>
        {
            Assert.NotSame(body, b);
            OutputAssert.Equal("", b.ToString());
        });
        body.Line("Detached();");
        OutputAssert.Equal("get\n{\n}\n", accessor.ToString());
    }

    [Fact]
    public void NullArguments_ThrowWithoutReplacingBody()
    {
        var accessor = CreateAccessor().Arrow("1");

        Assert.Throws<ArgumentNullException>("configure", () => accessor.Body(null!));
        Assert.Throws<ArgumentNullException>("expression", () => accessor.Arrow(null!));
        OutputAssert.Equal("get => 1;\n", accessor.ToString());
    }

    [Fact]
    public void BodyMethods_ReturnSameAccessorBuilder()
    {
        var accessor = CreateAccessor();

        Assert.Same(accessor, accessor.Body());
        Assert.Same(accessor, accessor.Body(b => b.Line("return 1;")));
        Assert.Same(accessor, accessor.Arrow("1"));
    }
}
