using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds an accessor declaration.</summary>
/// <remarks>Builders are mutable and are not thread-safe.</remarks>
public sealed class AccessorBuilder
{
    internal readonly string keyword;

    internal AccessorBuilder(string keyword)
    {
        this.keyword = keyword;
    }

    internal void Write(CodeWriter writer)
    {
        writer.WriteLine(keyword + ";");
    }

    /// <summary>Renders the accessor declaration.</summary>
    /// <returns>The accessor source with LF line endings.</returns>
    public override string ToString()
    {
        var writer = new CodeWriter();
        Write(writer);
        return writer.ToString();
    }
}
