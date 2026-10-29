using System;
using System.Collections.Generic;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds an enum declaration with ordered members and an optional underlying type.</summary>
/// <remarks>Builders are mutable and are not thread-safe.</remarks>
public sealed class EnumBuilder : TypeBuilder<EnumBuilder>
{
    internal string? underlyingType;
    internal readonly List<EnumMemberBuilder> members = new();

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

    /// <summary>Sets the underlying type, replacing any previous underlying type.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <returns>This enum builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type.</exception>
    public EnumBuilder UnderlyingType(Type type) => UnderlyingType(TypeName.Render(type, nameof(type)));

    /// <summary>Sets the underlying type, replacing any previous underlying type.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <returns>This enum builder.</returns>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type.</exception>
    public EnumBuilder UnderlyingType<T>() => UnderlyingType(typeof(T));

    /// <summary>Adds a member without an explicit value.</summary>
    /// <param name="name">The member identifier. Reserved keywords are escaped.</param>
    /// <returns>This enum builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public EnumBuilder Member(string name)
    {
        members.Add(new EnumMemberBuilder(name, null));
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

        members.Add(new EnumMemberBuilder(name, value));
        return this;
    }

    /// <summary>Immediately configures a new member without an explicit value and adds it.</summary>
    /// <param name="name">The member identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run once on the new member builder.</param>
    /// <returns>This enum builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public EnumBuilder Member(string name, Action<EnumMemberBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new EnumMemberBuilder(name, null);
        configure(builder);
        members.Add(builder);
        return this;
    }

    /// <summary>Immediately configures a new member with an explicit value expression and adds it.</summary>
    /// <param name="name">The member identifier. Reserved keywords are escaped.</param>
    /// <param name="value">The value expression, written without validation.</param>
    /// <param name="configure">The action to run once on the new member builder.</param>
    /// <returns>This enum builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="value"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public EnumBuilder Member(string name, string value, Action<EnumMemberBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var builder = new EnumMemberBuilder(name, value);
        configure(builder);
        members.Add(builder);
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
            members[i].Write(writer, i < members.Count - 1 ? "," : "");
        }

        writer.CloseBlock();
    }
}
