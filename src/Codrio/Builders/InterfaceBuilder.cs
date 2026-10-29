namespace Codrio;

/// <summary>Builds an interface declaration.</summary>
/// <remarks>Builders are mutable and are not thread-safe.</remarks>
public sealed class InterfaceBuilder : TypeDeclarationBuilder<InterfaceBuilder>
{
    internal InterfaceBuilder(string name) : base("interface", name)
    {
    }
}
