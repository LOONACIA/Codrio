using System;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Provides entry points for building C# source code.</summary>
public static class CSharp
{
    /// <summary>Creates a constructor builder with an empty block body. Its name comes from the containing type.</summary>
    /// <returns>A new constructor builder.</returns>
    public static ConstructorBuilder Constructor() => new();

    /// <summary>Creates a constructor builder and configures it immediately. Its name comes from the containing type.</summary>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>The configured constructor builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is null.</exception>
    public static ConstructorBuilder Constructor(Action<ConstructorBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = Constructor();
        configure(builder);
        return builder;
    }

    /// <summary>Creates a field declaration without an initializer.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The field identifier. Reserved keywords are escaped.</param>
    /// <returns>A new field builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static FieldBuilder Field(string type, string name) => new(type, name);

    /// <summary>Creates a field builder and configures it immediately.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The field identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>The configured field builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static FieldBuilder Field(string type, string name, Action<FieldBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = Field(type, name);
        configure(builder);
        return builder;
    }

    /// <summary>Creates a field declaration without an initializer.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <param name="name">The field identifier. Reserved keywords are escaped.</param>
    /// <returns>A new field builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    public static FieldBuilder Field(Type type, string name) => Field(TypeName.Render(type, nameof(type)), name);

    /// <summary>Creates a field builder and configures it immediately.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <param name="name">The field identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>The configured field builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    public static FieldBuilder Field(Type type, string name, Action<FieldBuilder> configure) => Field(TypeName.Render(type, nameof(type)), name, configure);

    /// <summary>Creates a field declaration without an initializer.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="name">The field identifier. Reserved keywords are escaped.</param>
    /// <returns>A new field builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    public static FieldBuilder Field<T>(string name) => Field(typeof(T), name);

    /// <summary>Creates a field builder and configures it immediately.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="name">The field identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>The configured field builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    public static FieldBuilder Field<T>(string name, Action<FieldBuilder> configure) => Field(typeof(T), name, configure);

    /// <summary>Creates a field-like event declaration.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The event identifier. Reserved keywords are escaped.</param>
    /// <returns>A new event builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static EventBuilder Event(string type, string name) => new(type, name);

    /// <summary>Creates a field-like event builder and configures it immediately.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The event identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>The configured event builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static EventBuilder Event(string type, string name, Action<EventBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = Event(type, name);
        configure(builder);
        return builder;
    }

    /// <summary>Creates a field-like event declaration.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <param name="name">The event identifier. Reserved keywords are escaped.</param>
    /// <returns>A new event builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    public static EventBuilder Event(Type type, string name) => Event(TypeName.Render(type, nameof(type)), name);

    /// <summary>Creates a field-like event builder and configures it immediately.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <param name="name">The event identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>The configured event builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    public static EventBuilder Event(Type type, string name, Action<EventBuilder> configure) => Event(TypeName.Render(type, nameof(type)), name, configure);

    /// <summary>Creates a field-like event declaration.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="name">The event identifier. Reserved keywords are escaped.</param>
    /// <returns>A new event builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    public static EventBuilder Event<T>(string name) => Event(typeof(T), name);

    /// <summary>Creates a field-like event builder and configures it immediately.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="name">The event identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>The configured event builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    public static EventBuilder Event<T>(string name, Action<EventBuilder> configure) => Event(typeof(T), name, configure);

    /// <summary>Creates a property declaration with get and set accessors.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The property identifier. Reserved keywords are escaped.</param>
    /// <returns>A new property builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static PropertyBuilder Property(string type, string name) => new(type, name);

    /// <summary>Creates a property builder and configures it immediately.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The property identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>The configured property builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static PropertyBuilder Property(string type, string name, Action<PropertyBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = Property(type, name);
        configure(builder);
        return builder;
    }

    /// <summary>Creates a property declaration with get and set accessors.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <param name="name">The property identifier. Reserved keywords are escaped.</param>
    /// <returns>A new property builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    public static PropertyBuilder Property(Type type, string name) => Property(TypeName.Render(type, nameof(type)), name);

    /// <summary>Creates a property builder and configures it immediately.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <param name="name">The property identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>The configured property builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    public static PropertyBuilder Property(Type type, string name, Action<PropertyBuilder> configure) => Property(TypeName.Render(type, nameof(type)), name, configure);

    /// <summary>Creates a property declaration with get and set accessors.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="name">The property identifier. Reserved keywords are escaped.</param>
    /// <returns>A new property builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    public static PropertyBuilder Property<T>(string name) => Property(typeof(T), name);

    /// <summary>Creates a property builder and configures it immediately.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="name">The property identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>The configured property builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type. <paramref name="name"/> is not a valid C# identifier.</exception>
    public static PropertyBuilder Property<T>(string name, Action<PropertyBuilder> configure) => Property(typeof(T), name, configure);

    /// <summary>Creates an indexer declaration with get and set accessors and no parameters.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <returns>A new indexer builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is null.</exception>
    public static IndexerBuilder Indexer(string type) => new(type);

    /// <summary>Creates an indexer builder and configures it immediately.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>The configured indexer builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="configure"/> is null.</exception>
    public static IndexerBuilder Indexer(string type, Action<IndexerBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = Indexer(type);
        configure(builder);
        return builder;
    }

    /// <summary>Creates an indexer declaration with get and set accessors and no parameters.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <returns>A new indexer builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type.</exception>
    public static IndexerBuilder Indexer(Type type) => Indexer(TypeName.Render(type, nameof(type)));

    /// <summary>Creates an indexer builder and configures it immediately.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>The configured indexer builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type.</exception>
    public static IndexerBuilder Indexer(Type type, Action<IndexerBuilder> configure) => Indexer(TypeName.Render(type, nameof(type)), configure);

    /// <summary>Creates an indexer declaration with get and set accessors and no parameters.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <returns>A new indexer builder.</returns>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type.</exception>
    public static IndexerBuilder Indexer<T>() => Indexer(typeof(T));

    /// <summary>Creates an indexer builder and configures it immediately.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>The configured indexer builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type.</exception>
    public static IndexerBuilder Indexer<T>(Action<IndexerBuilder> configure) => Indexer(typeof(T), configure);

    /// <summary>Creates a delegate declaration with a void return type and no parameters.</summary>
    /// <param name="name">The delegate identifier. Reserved keywords are escaped.</param>
    /// <returns>A new delegate builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static DelegateBuilder Delegate(string name) => new(name);

    /// <summary>Creates a delegate builder and configures it immediately.</summary>
    /// <param name="name">The delegate identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>The configured delegate builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static DelegateBuilder Delegate(string name, Action<DelegateBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = Delegate(name);
        configure(builder);
        return builder;
    }

    /// <summary>Creates an operator declaration with no parameters or body.</summary>
    /// <param name="returnType">The type reference, written without validation.</param>
    /// <param name="symbol">The operator symbol, written without validation.</param>
    /// <returns>A new operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="returnType"/> or <paramref name="symbol"/> is null.</exception>
    public static OperatorBuilder Operator(string returnType, string symbol)
    {
        if (returnType is null)
        {
            throw new ArgumentNullException(nameof(returnType));
        }

        if (symbol is null)
        {
            throw new ArgumentNullException(nameof(symbol));
        }

        return new OperatorBuilder(returnType + " operator " + symbol);
    }

    /// <summary>Creates an operator builder and configures it immediately.</summary>
    /// <param name="returnType">The type reference, written without validation.</param>
    /// <param name="symbol">The operator symbol, written without validation.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>The configured operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="returnType"/>, <paramref name="symbol"/>, or <paramref name="configure"/> is null.</exception>
    public static OperatorBuilder Operator(string returnType, string symbol, Action<OperatorBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = Operator(returnType, symbol);
        configure(builder);
        return builder;
    }

    /// <summary>Creates an operator declaration with no parameters or body.</summary>
    /// <param name="returnType">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <param name="symbol">The operator symbol, written without validation.</param>
    /// <returns>A new operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="returnType"/> or <paramref name="symbol"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="returnType"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type.</exception>
    public static OperatorBuilder Operator(Type returnType, string symbol) => Operator(TypeName.Render(returnType, nameof(returnType)), symbol);

    /// <summary>Creates an operator builder and configures it immediately.</summary>
    /// <param name="returnType">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <param name="symbol">The operator symbol, written without validation.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>The configured operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="returnType"/>, <paramref name="symbol"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="returnType"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type.</exception>
    public static OperatorBuilder Operator(Type returnType, string symbol, Action<OperatorBuilder> configure) => Operator(TypeName.Render(returnType, nameof(returnType)), symbol, configure);

    /// <summary>Creates an operator declaration with no parameters or body.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="symbol">The operator symbol, written without validation.</param>
    /// <returns>A new operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="symbol"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type.</exception>
    public static OperatorBuilder Operator<T>(string symbol) => Operator(typeof(T), symbol);

    /// <summary>Creates an operator builder and configures it immediately.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="symbol">The operator symbol, written without validation.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>The configured operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="symbol"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type.</exception>
    public static OperatorBuilder Operator<T>(string symbol, Action<OperatorBuilder> configure) => Operator(typeof(T), symbol, configure);

    /// <summary>Creates an implicit conversion operator declaration with no parameters or body.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <returns>A new operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is null.</exception>
    public static OperatorBuilder ImplicitOperator(string type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return new OperatorBuilder("implicit operator " + type);
    }

    /// <summary>Creates an implicit conversion operator builder and configures it immediately.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>The configured operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="configure"/> is null.</exception>
    public static OperatorBuilder ImplicitOperator(string type, Action<OperatorBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = ImplicitOperator(type);
        configure(builder);
        return builder;
    }

    /// <summary>Creates an implicit conversion operator declaration with no parameters or body.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <returns>A new operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type.</exception>
    public static OperatorBuilder ImplicitOperator(Type type) => ImplicitOperator(TypeName.Render(type, nameof(type)));

    /// <summary>Creates an implicit conversion operator builder and configures it immediately.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>The configured operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type.</exception>
    public static OperatorBuilder ImplicitOperator(Type type, Action<OperatorBuilder> configure) => ImplicitOperator(TypeName.Render(type, nameof(type)), configure);

    /// <summary>Creates an implicit conversion operator declaration with no parameters or body.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <returns>A new operator builder.</returns>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type.</exception>
    public static OperatorBuilder ImplicitOperator<T>() => ImplicitOperator(typeof(T));

    /// <summary>Creates an implicit conversion operator builder and configures it immediately.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>The configured operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type.</exception>
    public static OperatorBuilder ImplicitOperator<T>(Action<OperatorBuilder> configure) => ImplicitOperator(typeof(T), configure);

    /// <summary>Creates an explicit conversion operator declaration with no parameters or body.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <returns>A new operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is null.</exception>
    public static OperatorBuilder ExplicitOperator(string type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return new OperatorBuilder("explicit operator " + type);
    }

    /// <summary>Creates an explicit conversion operator builder and configures it immediately.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>The configured operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="configure"/> is null.</exception>
    public static OperatorBuilder ExplicitOperator(string type, Action<OperatorBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = ExplicitOperator(type);
        configure(builder);
        return builder;
    }

    /// <summary>Creates an explicit conversion operator declaration with no parameters or body.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <returns>A new operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type.</exception>
    public static OperatorBuilder ExplicitOperator(Type type) => ExplicitOperator(TypeName.Render(type, nameof(type)));

    /// <summary>Creates an explicit conversion operator builder and configures it immediately.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>The configured operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type.</exception>
    public static OperatorBuilder ExplicitOperator(Type type, Action<OperatorBuilder> configure) => ExplicitOperator(TypeName.Render(type, nameof(type)), configure);

    /// <summary>Creates an explicit conversion operator declaration with no parameters or body.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <returns>A new operator builder.</returns>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type.</exception>
    public static OperatorBuilder ExplicitOperator<T>() => ExplicitOperator(typeof(T));

    /// <summary>Creates an explicit conversion operator builder and configures it immediately.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>The configured operator builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type.</exception>
    public static OperatorBuilder ExplicitOperator<T>(Action<OperatorBuilder> configure) => ExplicitOperator(typeof(T), configure);

    /// <summary>Creates a method declaration with a void return type and no body.</summary>
    /// <param name="name">The method identifier. Reserved keywords are escaped.</param>
    /// <returns>A new method builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static MethodBuilder Method(string name) => new(name);

    /// <summary>Creates a method builder and configures it immediately.</summary>
    /// <param name="name">The method identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>The configured method builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static MethodBuilder Method(string name, Action<MethodBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = Method(name);
        configure(builder);
        return builder;
    }

    /// <summary>Creates an empty source file builder.</summary>
    /// <returns>A new file builder.</returns>
    public static FileBuilder File() => new();

    /// <summary>Creates a source file builder and configures it immediately.</summary>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>The configured file builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is null.</exception>
    public static FileBuilder File(Action<FileBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = File();
        configure(builder);
        return builder;
    }

    /// <summary>Creates an empty block namespace declaration.</summary>
    /// <param name="name">The namespace name, written without validation.</param>
    /// <returns>A new namespace builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    public static NamespaceBuilder Namespace(string name) => new(name, false);

    /// <summary>Creates a block namespace builder and configures it immediately.</summary>
    /// <param name="name">The namespace name, written without validation.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>The configured namespace builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    public static NamespaceBuilder Namespace(string name, Action<NamespaceBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = Namespace(name);
        configure(builder);
        return builder;
    }

    /// <summary>Creates an empty file-scoped namespace declaration.</summary>
    /// <remarks>A file may contain only one file-scoped namespace, before any type declarations, and cannot mix it with block namespaces. These constraints are not validated.</remarks>
    /// <param name="name">The namespace name, written without validation.</param>
    /// <returns>A new namespace builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    public static NamespaceBuilder FileScopedNamespace(string name) => new(name, true);

    /// <summary>Creates a file-scoped namespace builder and configures it immediately.</summary>
    /// <remarks>A file may contain only one file-scoped namespace, before any type declarations, and cannot mix it with block namespaces. These constraints are not validated.</remarks>
    /// <param name="name">The namespace name, written without validation.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>The configured namespace builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    public static NamespaceBuilder FileScopedNamespace(string name, Action<NamespaceBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = FileScopedNamespace(name);
        configure(builder);
        return builder;
    }

    /// <summary>Creates an empty class declaration.</summary>
    /// <param name="name">The class identifier. Reserved keywords are escaped.</param>
    /// <returns>A new class builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static ClassBuilder Class(string name) => new(name);

    /// <summary>Creates a class builder and configures it immediately.</summary>
    /// <param name="name">The class identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>The configured class builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static ClassBuilder Class(string name, Action<ClassBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = Class(name);
        configure(builder);
        return builder;
    }

    /// <summary>Creates an empty struct declaration.</summary>
    /// <param name="name">The struct identifier. Reserved keywords are escaped.</param>
    /// <returns>A new struct builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static StructBuilder Struct(string name) => new(name);

    /// <summary>Creates a struct builder and configures it immediately.</summary>
    /// <param name="name">The struct identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>The configured struct builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static StructBuilder Struct(string name, Action<StructBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = Struct(name);
        configure(builder);
        return builder;
    }

    /// <summary>Creates an empty interface declaration.</summary>
    /// <param name="name">The interface identifier. Reserved keywords are escaped.</param>
    /// <returns>A new interface builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static InterfaceBuilder Interface(string name) => new(name);

    /// <summary>Creates an interface builder and configures it immediately.</summary>
    /// <param name="name">The interface identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>The configured interface builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static InterfaceBuilder Interface(string name, Action<InterfaceBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = Interface(name);
        configure(builder);
        return builder;
    }

    /// <summary>Creates an empty record declaration.</summary>
    /// <param name="name">The record identifier. Reserved keywords are escaped.</param>
    /// <returns>A new record builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static RecordBuilder Record(string name) => new(name, false);

    /// <summary>Creates a record builder and configures it immediately.</summary>
    /// <param name="name">The record identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>The configured record builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static RecordBuilder Record(string name, Action<RecordBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = Record(name);
        configure(builder);
        return builder;
    }

    /// <summary>Creates an empty record struct declaration.</summary>
    /// <param name="name">The record struct identifier. Reserved keywords are escaped.</param>
    /// <returns>A new record struct builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static RecordBuilder RecordStruct(string name) => new(name, true);

    /// <summary>Creates a record struct builder and configures it immediately.</summary>
    /// <param name="name">The record struct identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>The configured record struct builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static RecordBuilder RecordStruct(string name, Action<RecordBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = RecordStruct(name);
        configure(builder);
        return builder;
    }

    /// <summary>Creates an empty enum declaration.</summary>
    /// <param name="name">The enum identifier. Reserved keywords are escaped.</param>
    /// <returns>A new enum builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static EnumBuilder Enum(string name) => new(name);

    /// <summary>Creates an enum builder and configures it immediately.</summary>
    /// <param name="name">The enum identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>The configured enum builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public static EnumBuilder Enum(string name, Action<EnumBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = Enum(name);
        configure(builder);
        return builder;
    }
}
