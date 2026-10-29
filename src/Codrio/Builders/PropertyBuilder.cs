using System;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds a property declaration with default get and set accessors.</summary>
/// <remarks>
/// Builders are mutable and are not thread-safe. Modifier combinations are not validated.
/// Set and Init share one accessor slot; the last call wins. Accessor calls clear the expression body,
/// and Arrow clears all accessors.
/// </remarks>
public sealed class PropertyBuilder : PropertyOrIndexerBuilder<PropertyBuilder>
{
    internal readonly string type;
    internal readonly string name;
    internal string? initializer;

    internal PropertyBuilder(string type, string name)
    {
        this.type = type ?? throw new ArgumentNullException(nameof(type));
        this.name = Identifier.ValidateAndEscape(name, nameof(name));
    }

    /// <summary>Sets the initializer expression, replacing any previous initializer.</summary>
    /// <remarks>The initializer is not rendered while the property has an expression body.</remarks>
    /// <param name="expression">The initializer expression, written without validation.</param>
    /// <returns>This property builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is null.</exception>
    public PropertyBuilder Initializer(string expression)
    {
        initializer = expression ?? throw new ArgumentNullException(nameof(expression));
        return this;
    }

    internal override void Write(CodeWriter writer)
    {
        WriteDeclaration(writer, RenderModifiers() + type + " " + name, initializer);
    }
}
