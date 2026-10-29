using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds a class declaration with an empty body.</summary>
/// <remarks>Builders are mutable and are not thread-safe.</remarks>
public sealed class ClassBuilder
{
    internal readonly string name;

    internal ClassBuilder(string name)
    {
        this.name = Identifier.ValidateAndEscape(name, nameof(name));
    }

    internal void Write(CodeWriter writer)
    {
        writer.WriteLine("class " + name);
        writer.OpenBlock();
        writer.CloseBlock();
    }

    /// <summary>Renders the class declaration.</summary>
    /// <returns>The class source with Allman braces and LF line endings.</returns>
    public override string ToString()
    {
        var writer = new CodeWriter();
        Write(writer);
        return writer.ToString();
    }
}
