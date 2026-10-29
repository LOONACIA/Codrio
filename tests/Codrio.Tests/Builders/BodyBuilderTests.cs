using System;
using Xunit;

namespace Codrio.Tests;

public class BodyBuilderTests
{
    [Fact]
    public void Line_PreservesOrderWithoutAutomaticBlankLines()
    {
        BodyBuilder? body = null;
        var method = CSharp.Method("M", m => m.Body(b =>
        {
            body = b;
            b.Line("First();");
        }));

        OutputAssert.Equal("void M()\n{\n    First();\n}\n", method.ToString());
        Assert.NotNull(body);
        body.Line("Second();").Line("Third();");
        OutputAssert.Equal("void M()\n{\n    First();\n    Second();\n    Third();\n}\n", method.ToString());
    }

    [Fact]
    public void EmptyLine_HasNoIndentation()
    {
        var method = CSharp.Method("M", m => m.Body(b => b.Line("First();").Line("").Line("Last();")));

        OutputAssert.Equal("void M()\n{\n    First();\n\n    Last();\n}\n", method.ToString());
    }

    [Fact]
    public void MultilineLine_NormalizesLineEndingsAndIndentsEachNonemptyLineInBlock()
    {
        var method = CSharp.Method("M", m => m.Body(b => b.Block("if (ready)", ib =>
            ib.Line("First();\r\n\r\nSecond();\nThird();"))));

        OutputAssert.Equal(
            "void M()\n{\n    if (ready)\n    {\n        First();\n\n        Second();\n        Third();\n    }\n}\n",
            method.ToString());
    }

    [Fact]
    public void NestedBlocks_IndentTwoLevels()
    {
        var method = CSharp.Method("M", m => m.Body(b => b.Block("if (ready)", ib =>
            ib.Block("while (running)", wb => wb.Line("Tick();")))));

        OutputAssert.Equal(
            "void M()\n{\n    if (ready)\n    {\n        while (running)\n        {\n            Tick();\n        }\n    }\n}\n",
            method.ToString());
    }

    [Fact]
    public void BlockWithoutConfigure_RendersEmptyBlock()
    {
        var method = CSharp.Method("M", m => m.Body(b => b.Block("if (ready)")));

        OutputAssert.Equal("void M()\n{\n    if (ready)\n    {\n    }\n}\n", method.ToString());
    }

    [Fact]
    public void ConsecutiveBlocks_RenderAllmanIfElseWithoutBlankLines()
    {
        var method = CSharp.Method("M", m => m.Body(b => b
            .Block("if (ready)", ib => ib.Line("Start();"))
            .Block("else", eb => eb.Line("Stop();"))));

        OutputAssert.Equal(
            "void M()\n{\n    if (ready)\n    {\n        Start();\n    }\n    else\n    {\n        Stop();\n    }\n}\n",
            method.ToString());
    }

    [Fact]
    public void ToString_RendersOnlyContentsAtZeroIndentation()
    {
        CSharp.Method("M", m => m.Body(b =>
        {
            OutputAssert.Equal("", b.ToString());
            b.Line("First();").Block("if (ready)", ib => ib.Line("Next();"));
            const string expected = "First();\nif (ready)\n{\n    Next();\n}\n";
            OutputAssert.Equal(expected, b.ToString());
            OutputAssert.Equal(expected, b.ToString());
        }));
    }

    [Fact]
    public void Block_InvokesConfigureImmediatelyOnceAndRetainsChildBuilder()
    {
        BodyBuilder? child = null;
        var calls = 0;
        var method = CSharp.Method("M", m => m.Body(b =>
        {
            b.Block("if (ready)", ib =>
            {
                child = ib;
                calls++;
                Assert.NotSame(b, ib);
                OutputAssert.Equal("", b.ToString());
                ib.Line("First();");
            });
            Assert.Equal(1, calls);
        }));

        Assert.NotNull(child);
        OutputAssert.Equal("First();\n", child.ToString());
        child.Line("Second();");
        const string expected = "void M()\n{\n    if (ready)\n    {\n        First();\n        Second();\n    }\n}\n";
        OutputAssert.Equal(expected, method.ToString());
        OutputAssert.Equal(expected, method.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void NullArguments_ThrowBeforeConfiguringOrAdding()
    {
        CSharp.Method("M", m => m.Body(b =>
        {
            var configured = false;
            Assert.Throws<ArgumentNullException>("code", () => b.Line(null!));
            Assert.Throws<ArgumentNullException>("header", () => b.Block(null!));
            Assert.Throws<ArgumentNullException>("header", () => b.Block(null!, _ => configured = true));
            Assert.Throws<ArgumentNullException>("configure", () => b.Block("if (ready)", null!));
            Assert.False(configured);
            OutputAssert.Equal("", b.ToString());
        }));
    }

    [Fact]
    public void FluentMethods_ReturnSameBodyBuilder()
    {
        CSharp.Method("M", m => m.Body(b =>
        {
            Assert.Same(b, b.Line("Run();"));
            Assert.Same(b, b.Block("if (ready)"));
            Assert.Same(b, b.Block("else", ib => ib.Line("Wait();")));
        }));
    }
}
