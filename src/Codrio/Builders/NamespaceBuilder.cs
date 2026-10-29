using System;
using System.Collections.Generic;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds a block or file-scoped namespace containing type declarations.</summary>
/// <remarks>Builders are mutable and are not thread-safe.</remarks>
public sealed class NamespaceBuilder
{
    internal readonly string name;
    internal readonly bool fileScoped;
    internal readonly List<Action<CodeWriter>> items = new();

    internal NamespaceBuilder(string name, bool fileScoped)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        this.name = name;
        this.fileScoped = fileScoped;
    }

    /// <summary>Adds an empty class declaration.</summary>
    /// <param name="name">The class identifier. Reserved keywords are escaped.</param>
    /// <returns>This namespace builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public NamespaceBuilder Class(string name) => Add(CSharp.Class(name));

    /// <summary>Immediately configures a new class builder and adds it to the namespace.</summary>
    /// <param name="name">The class identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This namespace builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public NamespaceBuilder Class(string name, Action<ClassBuilder> configure) => Add(CSharp.Class(name, configure));

    /// <summary>Adds the supplied class builder by reference without copying it.</summary>
    /// <param name="classBuilder">The class builder to add.</param>
    /// <returns>This namespace builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="classBuilder"/> is null.</exception>
    public NamespaceBuilder Add(ClassBuilder classBuilder)
    {
        if (classBuilder is null)
        {
            throw new ArgumentNullException(nameof(classBuilder));
        }

        items.Add(classBuilder.Write);
        return this;
    }

    /// <summary>Adds an empty struct declaration.</summary>
    /// <param name="name">The struct identifier. Reserved keywords are escaped.</param>
    /// <returns>This namespace builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public NamespaceBuilder Struct(string name) => Add(CSharp.Struct(name));

    /// <summary>Immediately configures a new struct builder and adds it to the namespace.</summary>
    /// <param name="name">The struct identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This namespace builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public NamespaceBuilder Struct(string name, Action<StructBuilder> configure) => Add(CSharp.Struct(name, configure));

    /// <summary>Adds the supplied struct builder by reference without copying it.</summary>
    /// <param name="structBuilder">The struct builder to add.</param>
    /// <returns>This namespace builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="structBuilder"/> is null.</exception>
    public NamespaceBuilder Add(StructBuilder structBuilder)
    {
        if (structBuilder is null)
        {
            throw new ArgumentNullException(nameof(structBuilder));
        }

        items.Add(structBuilder.Write);
        return this;
    }

    /// <summary>Adds an empty interface declaration.</summary>
    /// <param name="name">The interface identifier. Reserved keywords are escaped.</param>
    /// <returns>This namespace builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public NamespaceBuilder Interface(string name) => Add(CSharp.Interface(name));

    /// <summary>Immediately configures a new interface builder and adds it to the namespace.</summary>
    /// <param name="name">The interface identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This namespace builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public NamespaceBuilder Interface(string name, Action<InterfaceBuilder> configure) => Add(CSharp.Interface(name, configure));

    /// <summary>Adds the supplied interface builder by reference without copying it.</summary>
    /// <param name="interfaceBuilder">The interface builder to add.</param>
    /// <returns>This namespace builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="interfaceBuilder"/> is null.</exception>
    public NamespaceBuilder Add(InterfaceBuilder interfaceBuilder)
    {
        if (interfaceBuilder is null)
        {
            throw new ArgumentNullException(nameof(interfaceBuilder));
        }

        items.Add(interfaceBuilder.Write);
        return this;
    }

    internal void Write(CodeWriter writer)
    {
        if (fileScoped)
        {
            writer.WriteLine("namespace " + name + ";");
            if (items.Count > 0)
            {
                writer.WriteBlankLine();
            }
        }
        else
        {
            writer.WriteLine("namespace " + name);
            writer.OpenBlock();
        }

        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                writer.WriteBlankLine();
            }

            items[i](writer);
        }

        if (!fileScoped)
        {
            writer.CloseBlock();
        }
    }

    /// <summary>Renders the namespace in its selected form with one blank line between declarations.</summary>
    /// <returns>The namespace source with LF line endings and Allman braces for block namespaces.</returns>
    public override string ToString()
    {
        var writer = new CodeWriter();
        Write(writer);
        return writer.ToString();
    }
}
