using System;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds a method declaration.</summary>
/// <remarks>Builders are mutable and are not thread-safe. Modifier combinations are not validated.</remarks>
public sealed class MethodBuilder : MemberBuilder<MethodBuilder>
{
    internal readonly string name;
    internal string returnType = "void";
    internal bool isPartial;

    internal MethodBuilder(string name)
    {
        this.name = Identifier.ValidateAndEscape(name, nameof(name));
    }

    /// <summary>Sets the return type, replacing any previous return type. The default is void.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <returns>This method builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is null.</exception>
    public MethodBuilder Returns(string type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        returnType = type;
        return this;
    }

    /// <summary>Includes the partial modifier once, immediately before the return type.</summary>
    /// <returns>This method builder.</returns>
    public MethodBuilder Partial()
    {
        isPartial = true;
        return this;
    }

    internal override void Write(CodeWriter writer)
    {
        var declaration = RenderModifiers();
        if (isPartial)
        {
            declaration += "partial ";
        }

        writer.WriteLine(declaration + returnType + " " + name + "();");
    }
}
