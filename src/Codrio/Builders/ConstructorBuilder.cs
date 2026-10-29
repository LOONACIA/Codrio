using System;
using System.Collections.Generic;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds a constructor declaration with a block body.</summary>
/// <remarks>
/// Builders are mutable and are not thread-safe. Modifier combinations are not validated.
/// When added to a type, the constructor uses that type's escaped name without type parameters.
/// Standalone rendering omits the name because the containing type is unknown, even after the builder has been added to a type.
/// </remarks>
public sealed class ConstructorBuilder : MemberBuilder<ConstructorBuilder>
{
    internal readonly List<ParameterBuilder> parameters = new();
    internal BodyBuilder? body;

    internal ConstructorBuilder()
    {
    }

    /// <summary>Adds a parameter to the end of the parameter list.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <returns>This constructor builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public ConstructorBuilder Parameter(string type, string name)
    {
        parameters.Add(new ParameterBuilder(type, name));
        return this;
    }

    /// <summary>Immediately configures a new parameter builder and adds it to the constructor.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This constructor builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public ConstructorBuilder Parameter(string type, string name, Action<ParameterBuilder> configure)
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
    /// <returns>This constructor builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    /// <remarks>For ref, out, or in parameters, use a non-by-ref type and configure the parameter with <see cref="ParameterBuilder.Ref"/>, <see cref="ParameterBuilder.Out"/>, or <see cref="ParameterBuilder.In"/>. By-ref types are rejected with <see cref="ArgumentException"/>.</remarks>
    public ConstructorBuilder Parameter(Type type, string name) => Parameter(TypeName.Render(type, nameof(type)), name);

    /// <summary>Immediately configures a new parameter builder and adds it to the constructor.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This constructor builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    /// <remarks>For ref, out, or in parameters, use a non-by-ref type and configure the parameter with <see cref="ParameterBuilder.Ref"/>, <see cref="ParameterBuilder.Out"/>, or <see cref="ParameterBuilder.In"/>. By-ref types are rejected with <see cref="ArgumentException"/>.</remarks>
    public ConstructorBuilder Parameter(Type type, string name, Action<ParameterBuilder> configure) => Parameter(TypeName.Render(type, nameof(type)), name, configure);

    /// <summary>Adds a parameter from <typeparamref name="T"/> to the end of the parameter list.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <returns>This constructor builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    /// <remarks>For ref, out, or in parameters, use a non-by-ref type and configure the parameter with <see cref="ParameterBuilder.Ref"/>, <see cref="ParameterBuilder.Out"/>, or <see cref="ParameterBuilder.In"/>. By-ref types are rejected with <see cref="ArgumentException"/>.</remarks>
    public ConstructorBuilder Parameter<T>(string name) => Parameter(typeof(T), name);

    /// <summary>Immediately configures a new parameter builder and adds it to the constructor.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This constructor builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    /// <remarks>For ref, out, or in parameters, use a non-by-ref type and configure the parameter with <see cref="ParameterBuilder.Ref"/>, <see cref="ParameterBuilder.Out"/>, or <see cref="ParameterBuilder.In"/>. By-ref types are rejected with <see cref="ArgumentException"/>.</remarks>
    public ConstructorBuilder Parameter<T>(string name, Action<ParameterBuilder> configure) => Parameter(typeof(T), name, configure);

    /// <summary>Sets an empty block body, replacing any previous body. Constructors without a body also render an empty block.</summary>
    /// <returns>This constructor builder.</returns>
    public ConstructorBuilder Body()
    {
        body = new BodyBuilder();
        return this;
    }

    /// <summary>Immediately configures a new body builder, replacing any previous body.</summary>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>This constructor builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is null.</exception>
    public ConstructorBuilder Body(Action<BodyBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new BodyBuilder();
        configure(builder);
        body = builder;
        return this;
    }

    internal void Write(CodeWriter writer, string typeName)
    {
        WritePreamble(writer);
        writer.WriteLine(RenderModifiers() + typeName + "(" +
            string.Join(", ", parameters.ConvertAll(parameter => parameter.RenderParameter())) + ")");
        writer.OpenBlock();
        body?.Write(writer);
        writer.CloseBlock();
    }

    internal override void Write(CodeWriter writer) => Write(writer, "");

    /// <summary>Renders the constructor without a name because the containing type is unknown.</summary>
    /// <returns>The constructor source with LF line endings and an omitted name, even after being added to a type.</returns>
    public override string ToString() => base.ToString();
}
