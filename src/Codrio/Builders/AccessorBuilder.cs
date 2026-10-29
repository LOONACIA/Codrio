using System;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds an accessor declaration.</summary>
/// <remarks>Builders are mutable and are not thread-safe. Statements and expressions are written as strings without validation.</remarks>
public sealed class AccessorBuilder
{
    internal readonly string keyword;
    internal BodyBuilder? body;
    internal string? expression;

    internal bool HasBody => body is not null || expression is not null;

    internal AccessorBuilder(string keyword)
    {
        this.keyword = keyword;
    }

    /// <summary>Sets an empty block body, replacing any previous block or expression body.</summary>
    /// <returns>This accessor builder.</returns>
    public AccessorBuilder Body()
    {
        body = new BodyBuilder();
        expression = null;
        return this;
    }

    /// <summary>Immediately configures a new body builder, replacing any previous block or expression body.</summary>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>This accessor builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is null.</exception>
    public AccessorBuilder Body(Action<BodyBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new BodyBuilder();
        configure(builder);
        body = builder;
        expression = null;
        return this;
    }

    /// <summary>Sets an expression body, replacing any previous block or expression body.</summary>
    /// <param name="expression">The expression, written without validation.</param>
    /// <returns>This accessor builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is null.</exception>
    public AccessorBuilder Arrow(string expression)
    {
        if (expression is null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        this.expression = expression;
        body = null;
        return this;
    }

    internal void Write(CodeWriter writer)
    {
        if (body is not null)
        {
            writer.WriteLine(keyword);
            writer.OpenBlock();
            body.Write(writer);
            writer.CloseBlock();
        }
        else
        {
            writer.WriteLine(keyword + (expression is null ? ";" : " => " + expression + ";"));
        }
    }

    /// <summary>Renders the accessor declaration.</summary>
    /// <returns>The accessor source with LF line endings.</returns>
    public override string ToString()
    {
        var writer = new CodeWriter();
        Write(writer);
        return writer.ToString();
    }
}
