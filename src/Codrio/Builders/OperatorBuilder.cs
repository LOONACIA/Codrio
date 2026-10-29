using System;
using System.Collections.Generic;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds an operator or conversion operator declaration.</summary>
/// <remarks>
/// Builders are mutable and are not thread-safe. Modifier combinations, operator symbols, and parameter counts are not validated.
/// The static modifier is not added automatically.
/// </remarks>
public sealed class OperatorBuilder : MemberBuilder<OperatorBuilder>
{
    internal readonly string head;
    internal readonly List<ParameterBuilder> parameters = new();
    internal BodyBuilder? body;
    internal string? expression;

    internal OperatorBuilder(string head)
    {
        this.head = head;
    }

    /// <summary>Adds a parameter to the end of the parameter list.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <returns>This operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public OperatorBuilder Parameter(string type, string name)
    {
        parameters.Add(new ParameterBuilder(type, name));
        return this;
    }

    /// <summary>Immediately configures a new parameter builder and adds it to the operator.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public OperatorBuilder Parameter(string type, string name, Action<ParameterBuilder> configure)
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

    /// <summary>Adds a parameter from a <see cref="Type"/> to the end of the parameter list.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <returns>This operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    /// <remarks>For ref, out, or in parameters, use a non-by-ref type and configure the parameter with <see cref="ParameterBuilder.Ref"/>, <see cref="ParameterBuilder.Out"/>, or <see cref="ParameterBuilder.In"/>. By-ref types are rejected with <see cref="ArgumentException"/>.</remarks>
    public OperatorBuilder Parameter(Type type, string name) => Parameter(TypeName.Render(type, nameof(type)), name);

    /// <summary>Immediately configures a new parameter builder and adds it to the operator.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    /// <remarks>For ref, out, or in parameters, use a non-by-ref type and configure the parameter with <see cref="ParameterBuilder.Ref"/>, <see cref="ParameterBuilder.Out"/>, or <see cref="ParameterBuilder.In"/>. By-ref types are rejected with <see cref="ArgumentException"/>.</remarks>
    public OperatorBuilder Parameter(Type type, string name, Action<ParameterBuilder> configure) => Parameter(TypeName.Render(type, nameof(type)), name, configure);

    /// <summary>Adds a parameter from <typeparamref name="T"/> to the end of the parameter list.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <returns>This operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    /// <remarks>For ref, out, or in parameters, use a non-by-ref type and configure the parameter with <see cref="ParameterBuilder.Ref"/>, <see cref="ParameterBuilder.Out"/>, or <see cref="ParameterBuilder.In"/>. By-ref types are rejected with <see cref="ArgumentException"/>.</remarks>
    public OperatorBuilder Parameter<T>(string name) => Parameter(typeof(T), name);

    /// <summary>Immediately configures a new parameter builder and adds it to the operator.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    /// <remarks>For ref, out, or in parameters, use a non-by-ref type and configure the parameter with <see cref="ParameterBuilder.Ref"/>, <see cref="ParameterBuilder.Out"/>, or <see cref="ParameterBuilder.In"/>. By-ref types are rejected with <see cref="ArgumentException"/>.</remarks>
    public OperatorBuilder Parameter<T>(string name, Action<ParameterBuilder> configure) => Parameter(typeof(T), name, configure);

    /// <summary>Sets an empty block body, replacing any previous block or expression body.</summary>
    /// <returns>This operator builder.</returns>
    public OperatorBuilder Body()
    {
        body = new BodyBuilder();
        expression = null;
        return this;
    }

    /// <summary>Immediately configures a new body builder, replacing any previous block or expression body.</summary>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>This operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is null.</exception>
    public OperatorBuilder Body(Action<BodyBuilder> configure)
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
    /// <returns>This operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is null.</exception>
    public OperatorBuilder Arrow(string expression)
    {
        if (expression is null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        this.expression = expression;
        body = null;
        return this;
    }

    internal override void Write(CodeWriter writer)
    {
        WritePreamble(writer);
        var declaration = RenderModifiers() + head + "(" +
            string.Join(", ", parameters.ConvertAll(parameter => parameter.RenderParameter())) + ")";

        if (body is not null)
        {
            writer.WriteLine(declaration);
            writer.OpenBlock();
            body.Write(writer);
            writer.CloseBlock();
        }
        else
        {
            writer.WriteLine(declaration + (expression is null ? ";" : " => " + expression + ";"));
        }
    }
}
