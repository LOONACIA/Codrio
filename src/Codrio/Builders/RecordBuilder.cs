namespace Codrio;

/// <summary>Builds a record class or record struct declaration.</summary>
/// <remarks>Builders are mutable and are not thread-safe. Declarations without body items end with a semicolon.</remarks>
public sealed class RecordBuilder : ClassOrStructBuilder<RecordBuilder>
{
    internal RecordBuilder(string name, bool isStruct) : base(isStruct ? "record struct" : "record", name)
    {
    }

    internal override bool EndsWithSemicolonWhenEmpty => true;
}
