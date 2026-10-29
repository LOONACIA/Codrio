using System;
using System.Collections.Generic;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Provides the name and accessibility of a type declaration.</summary>
/// <typeparam name="TSelf">The concrete builder type returned by fluent methods.</typeparam>
/// <remarks>Builders are mutable and are not thread-safe. Documentation lines precede attribute lines, followed by the declaration. Attributes are written in addition order.</remarks>
public abstract class TypeBuilder<TSelf> where TSelf : TypeBuilder<TSelf>
{
    internal readonly string name;
    internal string? accessibility;
    internal readonly List<string> attributes = new();
    internal DocBuilder? doc;

    internal TypeBuilder(string name)
    {
        this.name = Identifier.ValidateAndEscape(name, nameof(name));
    }

    /// <summary>Sets public accessibility, replacing any previous accessibility.</summary>
    /// <returns>This type builder.</returns>
    public TSelf Public()
    {
        accessibility = "public";
        return (TSelf)this;
    }

    /// <summary>Sets internal accessibility, replacing any previous accessibility.</summary>
    /// <returns>This type builder.</returns>
    public TSelf Internal()
    {
        accessibility = "internal";
        return (TSelf)this;
    }

    /// <summary>Sets protected accessibility, replacing any previous accessibility.</summary>
    /// <returns>This type builder.</returns>
    public TSelf Protected()
    {
        accessibility = "protected";
        return (TSelf)this;
    }

    /// <summary>Sets private accessibility, replacing any previous accessibility.</summary>
    /// <returns>This type builder.</returns>
    public TSelf Private()
    {
        accessibility = "private";
        return (TSelf)this;
    }

    /// <summary>Sets protected internal accessibility, replacing any previous accessibility.</summary>
    /// <returns>This type builder.</returns>
    public TSelf ProtectedInternal()
    {
        accessibility = "protected internal";
        return (TSelf)this;
    }

    /// <summary>Sets private protected accessibility, replacing any previous accessibility.</summary>
    /// <returns>This type builder.</returns>
    public TSelf PrivateProtected()
    {
        accessibility = "private protected";
        return (TSelf)this;
    }

    /// <summary>Adds an attribute before the type declaration.</summary>
    /// <param name="name">The attribute name, written verbatim without validation, including target prefixes such as <c>return:</c>.</param>
    /// <param name="arguments">Raw argument strings joined with <c>, </c> inside parentheses. An empty array omits the parentheses.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="arguments"/>, or an element of <paramref name="arguments"/> is null.</exception>
    public TSelf Attribute(string name, params string[] arguments)
    {
        attributes.Add(AttributeSyntax.Render(name, arguments));
        return (TSelf)this;
    }

    /// <summary>Immediately configures documentation, replacing any previous documentation.</summary>
    /// <param name="configure">The action to run once on a new documentation builder.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is null.</exception>
    public TSelf Doc(Action<DocBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new DocBuilder();
        configure(builder);
        doc = builder;
        return (TSelf)this;
    }

    internal void WritePreamble(CodeWriter writer)
    {
        doc?.Write(writer);
        foreach (var attribute in attributes)
        {
            writer.WriteLine(attribute);
        }
    }

    internal abstract void Write(CodeWriter writer);

    /// <summary>Renders the type declaration.</summary>
    /// <returns>The type source with Allman braces and LF line endings.</returns>
    public override string ToString()
    {
        var writer = new CodeWriter();
        Write(writer);
        return writer.ToString();
    }
}
