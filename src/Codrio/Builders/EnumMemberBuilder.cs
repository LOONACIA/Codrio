using System;
using System.Collections.Generic;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds an enum member with attributes and an optional value expression.</summary>
/// <remarks>Builders are mutable and are not thread-safe. Value expressions are written verbatim without validation. ToString renders without a comma; the containing enum adds commas between members.</remarks>
public sealed class EnumMemberBuilder
{
    internal readonly string name;
    internal readonly string? value;
    internal readonly List<string> attributes = new();

    internal EnumMemberBuilder(string name, string? value)
    {
        this.name = Identifier.ValidateAndEscape(name, nameof(name));
        this.value = value;
    }

    /// <summary>Adds an attribute before the enum member in addition order, including duplicates.</summary>
    /// <param name="name">The attribute name, written verbatim without validation.</param>
    /// <param name="arguments">Raw argument strings joined with <c>, </c> inside parentheses. An empty array omits the parentheses.</param>
    /// <returns>This enum member builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="arguments"/>, or an element of <paramref name="arguments"/> is null.</exception>
    public EnumMemberBuilder Attribute(string name, params string[] arguments)
    {
        attributes.Add(AttributeSyntax.Render(name, arguments));
        return this;
    }

    internal void WritePreamble(CodeWriter writer)
    {
        foreach (var attribute in attributes)
        {
            writer.WriteLine(attribute);
        }
    }

    internal void Write(CodeWriter writer, string suffix)
    {
        WritePreamble(writer);
        writer.WriteLine(name + (value is null ? "" : " = " + value) + suffix);
    }

    /// <summary>Renders the enum member and its attributes.</summary>
    /// <returns>The enum member source with LF line endings and no trailing comma.</returns>
    public override string ToString()
    {
        var writer = new CodeWriter();
        Write(writer, "");
        return writer.ToString();
    }
}
