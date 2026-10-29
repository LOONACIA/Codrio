using System;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Provides accessors and expression bodies for property and indexer declarations.</summary>
/// <typeparam name="TSelf">The concrete builder type returned by fluent methods.</typeparam>
/// <remarks>
/// Builders are mutable and are not thread-safe. Modifier combinations are not validated.
/// Set and Init share one accessor slot; the last call wins. Accessor calls clear the expression body,
/// and Arrow clears all accessors. With neither specified, get and set accessors are rendered.
/// </remarks>
public abstract class PropertyOrIndexerBuilder<TSelf> : MemberBuilder<TSelf>
    where TSelf : PropertyOrIndexerBuilder<TSelf>
{
    internal AccessorBuilder? getter;
    internal AccessorBuilder? setter;
    internal string? expression;

    internal PropertyOrIndexerBuilder()
    {
    }

    /// <summary>Sets a bodyless get accessor, replacing any previous getter and clearing the expression body.</summary>
    /// <returns>This declaration builder.</returns>
    public TSelf Get()
    {
        getter = new AccessorBuilder("get");
        expression = null;
        return (TSelf)this;
    }

    /// <summary>Sets a bodyless set accessor, replacing any previous set or init accessor and clearing the expression body.</summary>
    /// <returns>This declaration builder.</returns>
    public TSelf Set()
    {
        setter = new AccessorBuilder("set");
        expression = null;
        return (TSelf)this;
    }

    /// <summary>Sets a bodyless init accessor, replacing any previous set or init accessor and clearing the expression body.</summary>
    /// <returns>This declaration builder.</returns>
    public TSelf Init()
    {
        setter = new AccessorBuilder("init");
        expression = null;
        return (TSelf)this;
    }

    /// <summary>Sets the declaration's expression body, replacing any previous expression body and clearing all accessors.</summary>
    /// <remarks>An initializer, if present, is not rendered while the declaration has an expression body.</remarks>
    /// <param name="expression">The expression, written without validation.</param>
    /// <returns>This declaration builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is null.</exception>
    public TSelf Arrow(string expression)
    {
        this.expression = expression ?? throw new ArgumentNullException(nameof(expression));
        getter = null;
        setter = null;
        return (TSelf)this;
    }

    internal void WriteDeclaration(CodeWriter writer, string head, string? initializer)
    {
        if (expression is not null)
        {
            writer.WriteLine(head + " => " + expression + ";");
            return;
        }

        var accessors = "";
        if (getter is not null)
        {
            accessors = getter.keyword + ";";
        }

        if (setter is not null)
        {
            accessors += (getter is null ? "" : " ") + setter.keyword + ";";
        }

        if (getter is null && setter is null)
        {
            accessors = "get; set;";
        }

        var declaration = head + " { " + accessors + " }";
        if (initializer is not null)
        {
            declaration += " = " + initializer + ";";
        }

        writer.WriteLine(declaration);
    }
}
