using System;
using System.Collections.Generic;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Provides accessibility and common modifiers for a member declaration.</summary>
/// <typeparam name="TSelf">The concrete builder type returned by fluent methods.</typeparam>
/// <remarks>Builders are mutable and are not thread-safe. Modifier combinations are not validated. Documentation lines precede attribute lines, followed by the declaration. Attributes are written in addition order.</remarks>
public abstract class MemberBuilder<TSelf> where TSelf : MemberBuilder<TSelf>
{
    internal string? accessibility;
    internal bool isStatic;
    internal bool isNew;
    internal bool isVirtual;
    internal bool isAbstract;
    internal bool isSealed;
    internal bool isOverride;
    internal bool isReadOnly;
    internal readonly List<string> attributes = new();
    internal DocBuilder? doc;

    internal MemberBuilder()
    {
    }

    /// <summary>Sets public accessibility, replacing any previous accessibility.</summary>
    /// <returns>This member builder.</returns>
    public TSelf Public()
    {
        accessibility = "public";
        return (TSelf)this;
    }

    /// <summary>Sets internal accessibility, replacing any previous accessibility.</summary>
    /// <returns>This member builder.</returns>
    public TSelf Internal()
    {
        accessibility = "internal";
        return (TSelf)this;
    }

    /// <summary>Sets protected accessibility, replacing any previous accessibility.</summary>
    /// <returns>This member builder.</returns>
    public TSelf Protected()
    {
        accessibility = "protected";
        return (TSelf)this;
    }

    /// <summary>Sets private accessibility, replacing any previous accessibility.</summary>
    /// <returns>This member builder.</returns>
    public TSelf Private()
    {
        accessibility = "private";
        return (TSelf)this;
    }

    /// <summary>Sets protected internal accessibility, replacing any previous accessibility.</summary>
    /// <returns>This member builder.</returns>
    public TSelf ProtectedInternal()
    {
        accessibility = "protected internal";
        return (TSelf)this;
    }

    /// <summary>Sets private protected accessibility, replacing any previous accessibility.</summary>
    /// <returns>This member builder.</returns>
    public TSelf PrivateProtected()
    {
        accessibility = "private protected";
        return (TSelf)this;
    }

    /// <summary>Includes the static modifier once.</summary>
    /// <returns>This member builder.</returns>
    public TSelf Static()
    {
        isStatic = true;
        return (TSelf)this;
    }

    /// <summary>Includes the new modifier once.</summary>
    /// <returns>This member builder.</returns>
    public TSelf New()
    {
        isNew = true;
        return (TSelf)this;
    }

    /// <summary>Includes the virtual modifier once.</summary>
    /// <returns>This member builder.</returns>
    public TSelf Virtual()
    {
        isVirtual = true;
        return (TSelf)this;
    }

    /// <summary>Includes the abstract modifier once.</summary>
    /// <returns>This member builder.</returns>
    public TSelf Abstract()
    {
        isAbstract = true;
        return (TSelf)this;
    }

    /// <summary>Includes the sealed modifier once.</summary>
    /// <returns>This member builder.</returns>
    public TSelf Sealed()
    {
        isSealed = true;
        return (TSelf)this;
    }

    /// <summary>Includes the override modifier once.</summary>
    /// <returns>This member builder.</returns>
    public TSelf Override()
    {
        isOverride = true;
        return (TSelf)this;
    }

    /// <summary>Includes the readonly modifier once.</summary>
    /// <returns>This member builder.</returns>
    public TSelf ReadOnly()
    {
        isReadOnly = true;
        return (TSelf)this;
    }

    /// <summary>Adds an attribute before the member declaration.</summary>
    /// <param name="name">The attribute name, written verbatim without validation, including target prefixes such as <c>return:</c>.</param>
    /// <param name="arguments">Raw argument strings joined with <c>, </c> inside parentheses. An empty array omits the parentheses.</param>
    /// <returns>This member builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="arguments"/>, or an element of <paramref name="arguments"/> is null.</exception>
    public TSelf Attribute(string name, params string[] arguments)
    {
        attributes.Add(AttributeSyntax.Render(name, arguments));
        return (TSelf)this;
    }

    /// <summary>Adds an attribute from a <see cref="Type"/> before the member declaration.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <param name="arguments">Raw argument strings joined with <c>, </c> inside parentheses. An empty array omits the parentheses.</param>
    /// <returns>This member builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="arguments"/>, or an element of <paramref name="arguments"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type.</exception>
    /// <remarks>The Attribute suffix is preserved. Target prefixes such as <c>return:</c> require the string overload. Whether the type derives from System.Attribute is not validated.</remarks>
    public TSelf Attribute(Type type, params string[] arguments) => Attribute(TypeName.Render(type, nameof(type)), arguments);

    /// <summary>Adds an attribute from <typeparamref name="TAttribute"/> before the member declaration.</summary>
    /// <typeparam name="TAttribute">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="arguments">Raw argument strings joined with <c>, </c> inside parentheses. An empty array omits the parentheses.</param>
    /// <returns>This member builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="arguments"/> or an element of <paramref name="arguments"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="TAttribute"/> cannot be written as a C# type name, such as a compiler-generated type.</exception>
    /// <remarks>The Attribute suffix is preserved. Target prefixes such as <c>return:</c> require the string overload.</remarks>
    public TSelf Attribute<TAttribute>(params string[] arguments) where TAttribute : System.Attribute => Attribute(typeof(TAttribute), arguments);

    /// <summary>Immediately configures documentation, replacing any previous documentation.</summary>
    /// <param name="configure">The action to run once on a new documentation builder.</param>
    /// <returns>This member builder.</returns>
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

    internal string RenderModifiers()
    {
        var modifiers = accessibility is null ? "" : accessibility + " ";
        if (isStatic)
        {
            modifiers += "static ";
        }

        if (isNew)
        {
            modifiers += "new ";
        }

        if (isVirtual)
        {
            modifiers += "virtual ";
        }

        if (isAbstract)
        {
            modifiers += "abstract ";
        }

        if (isSealed)
        {
            modifiers += "sealed ";
        }

        if (isOverride)
        {
            modifiers += "override ";
        }

        if (isReadOnly)
        {
            modifiers += "readonly ";
        }

        return modifiers;
    }

    internal abstract void Write(CodeWriter writer);

    /// <summary>Renders the member declaration.</summary>
    /// <returns>The member source with LF line endings.</returns>
    public override string ToString()
    {
        var writer = new CodeWriter();
        Write(writer);
        return writer.ToString();
    }
}
