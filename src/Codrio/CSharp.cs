using System;

namespace Codrio;

/// <summary>Provides entry points for building C# source code.</summary>
public static class CSharp
{
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
}
