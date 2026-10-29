using System;
using System.Collections.Generic;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds a type declaration that supports primary constructor parameters.</summary>
/// <typeparam name="TSelf">The concrete builder type returned by fluent methods.</typeparam>
/// <remarks>Builders are mutable and are not thread-safe.</remarks>
public abstract class ClassOrStructBuilder<TSelf> : TypeDeclarationBuilder<TSelf>
    where TSelf : ClassOrStructBuilder<TSelf>
{
    internal readonly List<ParameterBuilder> primaryParameters = new();

    internal ClassOrStructBuilder(string keyword, string name) : base(keyword, name)
    {
    }

    /// <summary>Adds a primary constructor parameter to the end of the parameter list.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf PrimaryParameter(string type, string name)
    {
        primaryParameters.Add(new ParameterBuilder(type, name));
        return (TSelf)this;
    }

    /// <summary>Immediately configures a new parameter builder and adds it to the primary constructor.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf PrimaryParameter(string type, string name, Action<ParameterBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new ParameterBuilder(type, name);
        configure(builder);
        primaryParameters.Add(builder);
        return (TSelf)this;
    }

    /// <summary>Adds a primary constructor parameter from a <see cref="Type"/> to the end of the parameter list.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    /// <remarks>For ref, out, or in parameters, use a non-by-ref type and configure the parameter with <see cref="ParameterBuilder.Ref"/>, <see cref="ParameterBuilder.Out"/>, or <see cref="ParameterBuilder.In"/>. By-ref types are rejected with <see cref="ArgumentException"/>.</remarks>
    public TSelf PrimaryParameter(Type type, string name) => PrimaryParameter(TypeName.Render(type, nameof(type)), name);

    /// <summary>Immediately configures a new parameter builder and adds it to the primary constructor.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    /// <remarks>For ref, out, or in parameters, use a non-by-ref type and configure the parameter with <see cref="ParameterBuilder.Ref"/>, <see cref="ParameterBuilder.Out"/>, or <see cref="ParameterBuilder.In"/>. By-ref types are rejected with <see cref="ArgumentException"/>.</remarks>
    public TSelf PrimaryParameter(Type type, string name, Action<ParameterBuilder> configure) => PrimaryParameter(TypeName.Render(type, nameof(type)), name, configure);

    /// <summary>Adds a primary constructor parameter from <typeparamref name="T"/> to the end of the parameter list.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    /// <remarks>For ref, out, or in parameters, use a non-by-ref type and configure the parameter with <see cref="ParameterBuilder.Ref"/>, <see cref="ParameterBuilder.Out"/>, or <see cref="ParameterBuilder.In"/>. By-ref types are rejected with <see cref="ArgumentException"/>.</remarks>
    public TSelf PrimaryParameter<T>(string name) => PrimaryParameter(typeof(T), name);

    /// <summary>Immediately configures a new parameter builder and adds it to the primary constructor.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    /// <remarks>For ref, out, or in parameters, use a non-by-ref type and configure the parameter with <see cref="ParameterBuilder.Ref"/>, <see cref="ParameterBuilder.Out"/>, or <see cref="ParameterBuilder.In"/>. By-ref types are rejected with <see cref="ArgumentException"/>.</remarks>
    public TSelf PrimaryParameter<T>(string name, Action<ParameterBuilder> configure) => PrimaryParameter(typeof(T), name, configure);

    /// <summary>Adds a field declaration without an initializer.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The field identifier. Reserved keywords are escaped.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf Field(string type, string name) => Add(CSharp.Field(type, name));

    /// <summary>Immediately configures a new field builder and adds it to the declaration.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The field identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf Field(string type, string name, Action<FieldBuilder> configure) => Add(CSharp.Field(type, name, configure));

    /// <summary>Adds the supplied field builder by reference without copying it.</summary>
    /// <param name="fieldBuilder">The field builder to add.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fieldBuilder"/> is null.</exception>
    public TSelf Add(FieldBuilder fieldBuilder)
    {
        if (fieldBuilder is null)
        {
            throw new ArgumentNullException(nameof(fieldBuilder));
        }

        items.Add(fieldBuilder.Write);
        return (TSelf)this;
    }

    /// <summary>Adds a constructor with an empty block body using this type's name.</summary>
    /// <returns>This type builder.</returns>
    public TSelf Constructor() => Add(CSharp.Constructor());

    /// <summary>Immediately configures a new constructor builder and adds it using this type's name.</summary>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is null.</exception>
    public TSelf Constructor(Action<ConstructorBuilder> configure) => Add(CSharp.Constructor(configure));

    /// <summary>Adds the supplied constructor builder by reference, using this type's escaped name when rendering.</summary>
    /// <param name="constructorBuilder">The constructor builder to add without copying.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="constructorBuilder"/> is null.</exception>
    public TSelf Add(ConstructorBuilder constructorBuilder)
    {
        if (constructorBuilder is null)
        {
            throw new ArgumentNullException(nameof(constructorBuilder));
        }

        items.Add(writer => constructorBuilder.Write(writer, name));
        return (TSelf)this;
    }

    internal override string ParameterList() => primaryParameters.Count == 0
        ? ""
        : "(" + string.Join(", ", primaryParameters.ConvertAll(parameter => parameter.RenderParameter())) + ")";
}
