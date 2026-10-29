using System;
using System.Collections.Generic;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds an ordered sequence of code lines and blocks.</summary>
/// <remarks>Builders are mutable and are not thread-safe. Statements and expressions are written as strings without validation.</remarks>
public sealed class BodyBuilder
{
    internal readonly List<Action<CodeWriter>> items = new();

    internal BodyBuilder()
    {
    }

    /// <summary>Adds code at the current indentation, normalizing CRLF line endings to LF.</summary>
    /// <param name="code">One or more code lines, written without validation. Empty lines have no indentation.</param>
    /// <returns>This body builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> is null.</exception>
    public BodyBuilder Line(string code)
    {
        if (code is null)
        {
            throw new ArgumentNullException(nameof(code));
        }

        items.Add(writer => writer.WriteLine(code));
        return this;
    }

    /// <summary>Adds a header followed by an empty block.</summary>
    /// <param name="header">The block header, written without validation.</param>
    /// <returns>This body builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="header"/> is null.</exception>
    public BodyBuilder Block(string header) => Block(header, _ => { });

    /// <summary>Immediately configures a new body builder and adds it as a block after the header.</summary>
    /// <param name="header">The block header, written without validation.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>This body builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="header"/> or <paramref name="configure"/> is null.</exception>
    public BodyBuilder Block(string header, Action<BodyBuilder> configure)
    {
        if (header is null)
        {
            throw new ArgumentNullException(nameof(header));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new BodyBuilder();
        configure(builder);
        items.Add(writer =>
        {
            writer.WriteLine(header);
            writer.OpenBlock();
            builder.Write(writer);
            writer.CloseBlock();
        });
        return this;
    }

    internal void Write(CodeWriter writer)
    {
        foreach (var item in items)
        {
            item(writer);
        }
    }

    /// <summary>Renders the body contents without enclosing braces or initial indentation.</summary>
    /// <returns>The body source with LF line endings, or an empty string when there are no items.</returns>
    public override string ToString()
    {
        var writer = new CodeWriter();
        Write(writer);
        return writer.ToString();
    }
}
