using Codrio.Rendering;
using Xunit;

namespace Codrio.Tests;

public class CodeWriterTests
{
    [Theory]
    [InlineData("first\n\nthird\n")]
    [InlineData("first\r\n\r\nthird\r\n")]
    public void WriteDocumentationComment_PrefixesEveryLineWithoutTrailingSpacesOnEmptyLines(string text)
    {
        var writer = new CodeWriter();

        writer.WriteDocumentationComment(text);
        writer.WriteDocumentationComment("");

        OutputAssert.Equal("/// first\n///\n/// third\n///\n///\n", writer.ToString());
    }

    [Theory]
    [InlineData("first\n\nthird\n")]
    [InlineData("first\r\n\r\nthird\r\n")]
    public void WriteDocumentationComment_IndentsEveryLineInsideBlocks(string text)
    {
        var writer = new CodeWriter();
        writer.OpenBlock();

        writer.WriteDocumentationComment(text);
        writer.WriteLine("next");
        writer.CloseBlock();
        writer.WriteDocumentationComment("last");

        OutputAssert.Equal("{\n    /// first\n    ///\n    /// third\n    ///\n    next\n}\n/// last\n", writer.ToString());
    }

    [Fact]
    public void CloseBlock_WithSuffixRestoresOuterIndentation()
    {
        var writer = new CodeWriter();
        writer.OpenBlock();
        writer.WriteLine("int Count");
        writer.OpenBlock();
        writer.WriteLine("get => field;");
        writer.CloseBlock(" = 0;");
        writer.WriteLine("next");
        writer.CloseBlock();
        writer.WriteLine("last");

        OutputAssert.Equal("{\n    int Count\n    {\n        get => field;\n    } = 0;\n    next\n}\nlast\n",
            writer.ToString());
    }

    [Fact]
    public void WriteLine_WritesUnindentedLine()
    {
        var writer = new CodeWriter();

        writer.WriteLine("text");

        OutputAssert.Equal("text\n", writer.ToString());
    }

    [Fact]
    public void Blocks_UseAllmanBracesAndFourSpaceIndentation()
    {
        var writer = new CodeWriter();

        writer.WriteLine("namespace Example");
        writer.OpenBlock();
        writer.WriteLine("class Foo");
        writer.OpenBlock();
        writer.WriteLine("void M()");
        writer.OpenBlock();
        writer.CloseBlock();
        writer.CloseBlock();
        writer.CloseBlock();
        writer.WriteLine("class Bar {}");

        const string expected = """
            namespace Example
            {
                class Foo
                {
                    void M()
                    {
                    }
                }
            }
            class Bar {}

            """;
        var output = writer.ToString();
        OutputAssert.Equal(expected, output);
        Assert.DoesNotContain("\r", output);
        Assert.DoesNotContain("\t", output);
        CompilationAssert.NoErrors(output);
    }

    [Theory]
    [InlineData("first\nsecond")]
    [InlineData("first\r\nsecond")]
    public void WriteLine_IndentsEveryLineAndNormalizesCrLf(string text)
    {
        var writer = new CodeWriter();
        writer.OpenBlock();

        writer.WriteLine(text);
        writer.CloseBlock();

        var output = writer.ToString();
        OutputAssert.Equal("{\n    first\n    second\n}\n", output);
        Assert.DoesNotContain("\r", output);
        Assert.DoesNotContain("\t", output);
    }

    [Fact]
    public void EmptyLines_HaveNoIndentation()
    {
        var writer = new CodeWriter();
        writer.OpenBlock();

        writer.WriteLine("\nfirst\n\nsecond\r\n");
        writer.WriteLine("");
        writer.WriteBlankLine();
        writer.CloseBlock();

        OutputAssert.Equal("{\n\n    first\n\n    second\n\n\n\n}\n", writer.ToString());
    }
}
