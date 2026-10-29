using System;
using System.Collections.Generic;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds an indexer declaration with default get and set accessors.</summary>
/// <remarks>
/// Builders are mutable and are not thread-safe. Modifier combinations and parameter counts are not validated.
/// An indexer without parameters renders this[]; validation is left to the compiler.
/// </remarks>
public sealed class IndexerBuilder : PropertyOrIndexerBuilder<IndexerBuilder>
{
    internal readonly string type;
    internal readonly List<ParameterBuilder> parameters = new();

    internal IndexerBuilder(string type)
    {
        this.type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>Adds a parameter to the end of the parameter list.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <returns>This indexer builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public IndexerBuilder Parameter(string type, string name)
    {
        parameters.Add(new ParameterBuilder(type, name));
        return this;
    }

    /// <summary>Immediately configures a new parameter builder and adds it to the indexer.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>This indexer builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public IndexerBuilder Parameter(string type, string name, Action<ParameterBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new ParameterBuilder(type, name);
        configure(builder);
        parameters.Add(builder);
        return this;
    }

    internal override void Write(CodeWriter writer)
    {
        WriteDeclaration(writer, RenderModifiers() + type + " this[" +
            string.Join(", ", parameters.ConvertAll(parameter => parameter.RenderParameter())) + "]", null);
    }
}
