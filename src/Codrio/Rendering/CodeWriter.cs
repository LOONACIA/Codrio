using System.Text;

namespace Codrio.Rendering;

internal sealed class CodeWriter
{
    private readonly StringBuilder builder = new();
    private int indentation;

    internal void WriteLine(string text)
    {
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.Length > 0)
            {
                builder.Append(' ', indentation * 4);
                builder.Append(line);
            }

            builder.Append('\n');
        }
    }

    internal void WriteDocumentationComment(string text)
    {
        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            WriteLine(line.Length == 0 ? "///" : "/// " + line);
        }
    }

    internal void WriteBlankLine()
    {
        builder.Append('\n');
    }

    internal void OpenBlock()
    {
        WriteLine("{");
        indentation++;
    }

    internal void CloseBlock()
    {
        indentation--;
        WriteLine("}");
    }

    internal void CloseBlock(string suffix)
    {
        indentation--;
        WriteLine("}" + suffix);
    }

    public override string ToString() => builder.ToString();
}
