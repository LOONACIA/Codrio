using System;
using System.Collections.Generic;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds an enum member with documentation, attributes, and an optional value expression.</summary>
/// <remarks>Builders are mutable and are not thread-safe. Value expressions are written verbatim without validation. Documentation lines precede attribute lines, followed by the declaration. Attributes are written in addition order. ToString renders without a comma; the containing enum adds commas between members.</remarks>
public sealed class EnumMemberBuilder
{
    internal readonly string name;
    internal readonly string? value;
    internal readonly List<string> attributes = new();
    internal DocBuilder? doc;

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

    /// <summary>Adds an attribute from a <see cref="Type"/> before the enum member in addition order, including duplicates.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <param name="arguments">Raw argument strings joined with <c>, </c> inside parentheses. An empty array omits the parentheses.</param>
    /// <returns>This enum member builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="arguments"/>, or an element of <paramref name="arguments"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type.</exception>
    /// <remarks>The Attribute suffix is preserved. Target prefixes such as <c>property:</c> or <c>param:</c> require the string overload. Whether the type derives from System.Attribute is not validated.</remarks>
    public EnumMemberBuilder Attribute(Type type, params string[] arguments) => Attribute(TypeName.Render(type, nameof(type)), arguments);

    /// <summary>Adds an attribute from <typeparamref name="TAttribute"/> before the enum member in addition order, including duplicates.</summary>
    /// <typeparam name="TAttribute">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="arguments">Raw argument strings joined with <c>, </c> inside parentheses. An empty array omits the parentheses.</param>
    /// <returns>This enum member builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="arguments"/> or an element of <paramref name="arguments"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="TAttribute"/> cannot be written as a C# type name, such as a compiler-generated type.</exception>
    /// <remarks>The Attribute suffix is preserved. Target prefixes such as <c>property:</c> or <c>param:</c> require the string overload.</remarks>
    public EnumMemberBuilder Attribute<TAttribute>(params string[] arguments) where TAttribute : System.Attribute => Attribute(typeof(TAttribute), arguments);

    /// <summary>Immediately configures documentation, replacing any previous documentation.</summary>
    /// <param name="configure">The action to run once on a new documentation builder.</param>
    /// <returns>This enum member builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is null.</exception>
    public EnumMemberBuilder Doc(Action<DocBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new DocBuilder();
        configure(builder);
        doc = builder;
        return this;
    }

    internal void WritePreamble(CodeWriter writer)
    {
        doc?.Write(writer);
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

    /// <summary>Renders the enum member with its documentation and attributes.</summary>
    /// <returns>The enum member source with LF line endings and no trailing comma.</returns>
    public override string ToString()
    {
        var writer = new CodeWriter();
        Write(writer, "");
        return writer.ToString();
    }
}
