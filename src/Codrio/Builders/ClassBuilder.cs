namespace Codrio;

/// <summary>Builds a class declaration.</summary>
/// <remarks>Builders are mutable and are not thread-safe.</remarks>
public sealed class ClassBuilder : ClassOrStructBuilder<ClassBuilder>
{
    internal ClassBuilder(string name) : base("class", name)
    {
    }
}
