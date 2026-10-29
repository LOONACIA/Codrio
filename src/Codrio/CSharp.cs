using System;

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
