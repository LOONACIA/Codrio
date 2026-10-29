namespace Codrio;

/// <summary>Builds a struct declaration.</summary>
/// <remarks>Builders are mutable and are not thread-safe.</remarks>
public sealed class StructBuilder : TypeDeclarationBuilder<StructBuilder>
{
    internal StructBuilder(string name) : base("struct", name)
    {
    }
}
