using System;
using System.Collections.Generic;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds an enum declaration with ordered members and an optional underlying type.</summary>
/// <remarks>Builders are mutable and are not thread-safe.</remarks>
public sealed class EnumBuilder : TypeBuilder<EnumBuilder>
{
    internal string? underlyingType;
    internal readonly List<string> members = new();

    internal EnumBuilder(string name) : base(name)
    {
    }

    /// <summary>Sets the underlying type, replacing any previous underlying type.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <returns>This enum builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is null.</exception>
    public EnumBuilder UnderlyingType(string type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        underlyingType = type;
        return this;
    }

    /// <summary>Adds a member without an explicit value.</summary>
    /// <param name="name">The member identifier. Reserved keywords are escaped.</param>
    /// <returns>This enum builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public EnumBuilder Member(string name)
    {
        members.Add(Identifier.ValidateAndEscape(name, nameof(name)));
        return this;
    }

    /// <summary>Adds a member with an explicit value expression.</summary>
    /// <param name="name">The member identifier. Reserved keywords are escaped.</param>
    /// <param name="value">The value expression, written without validation.</param>
    /// <returns>This enum builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public EnumBuilder Member(string name, string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        members.Add(Identifier.ValidateAndEscape(name, nameof(name)) + " = " + value);
        return this;
    }

    internal override void Write(CodeWriter writer)
    {
        WritePreamble(writer);
        var declaration = accessibility is null ? "" : accessibility + " ";
        declaration += "enum " + name;
        if (underlyingType is not null)
        {
            declaration += " : " + underlyingType;
        }

        writer.WriteLine(declaration);
        writer.OpenBlock();
        for (var i = 0; i < members.Count; i++)
        {
            writer.WriteLine(members[i] + (i < members.Count - 1 ? "," : ""));
        }

        writer.CloseBlock();
    }
}
