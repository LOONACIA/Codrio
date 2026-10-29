using System;
using System.Collections.Generic;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds XML documentation comments.</summary>
/// <remarks>
/// Builders are mutable and are not thread-safe. Elements are written in call order, including duplicates.
/// Only Summary, Param, and Returns text is XML-escaped; ampersands and angle brackets are escaped, while quotes are preserved.
/// Xml input is written verbatim without validation. Each element starts on a new documentation line.
/// Multiline content is prefixed with triple slashes on every line, normalizing CRLF to LF; empty lines have no trailing space.
/// </remarks>
public sealed class DocBuilder
{
    internal readonly List<string> elements = new();

    internal DocBuilder()
    {
    }

    /// <summary>Adds a summary element with XML-escaped text.</summary>
    /// <param name="text">The summary text.</param>
    /// <returns>This documentation builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    public DocBuilder Summary(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        elements.Add("<summary>" + XmlText.Escape(text) + "</summary>");
        return this;
    }

    /// <summary>Adds a parameter element with XML-escaped text.</summary>
    /// <param name="name">A valid C# identifier. Reserved keywords are prefixed with <c>@</c>.</param>
    /// <param name="text">The parameter description.</param>
    /// <returns>This documentation builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public DocBuilder Param(string name, string text)
    {
        var escapedName = Identifier.ValidateAndEscape(name, nameof(name));
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        elements.Add("<param name=\"" + escapedName + "\">" + XmlText.Escape(text) + "</param>");
        return this;
    }

    /// <summary>Adds a returns element with XML-escaped text.</summary>
    /// <param name="text">The return value description.</param>
    /// <returns>This documentation builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    public DocBuilder Returns(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        elements.Add("<returns>" + XmlText.Escape(text) + "</returns>");
        return this;
    }

    /// <summary>Adds raw XML without escaping or validation.</summary>
    /// <param name="xml">The raw XML documentation content.</param>
    /// <returns>This documentation builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="xml"/> is null.</exception>
    public DocBuilder Xml(string xml)
    {
        if (xml is null)
        {
            throw new ArgumentNullException(nameof(xml));
        }

        elements.Add(xml);
        return this;
    }

    internal void Write(CodeWriter writer)
    {
        foreach (var element in elements)
        {
            writer.WriteDocumentationComment(element);
        }
    }

    /// <summary>Renders the documentation comments without initial indentation.</summary>
    /// <returns>The documentation source with LF line endings, or an empty string when there are no elements.</returns>
    public override string ToString()
    {
        var writer = new CodeWriter();
        Write(writer);
        return writer.ToString();
    }
}
