using System;
using System.Collections.Generic;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds a type declaration with modifiers and ordered body items.</summary>
/// <typeparam name="TSelf">The concrete builder type returned by fluent methods.</typeparam>
/// <remarks>Builders are mutable and are not thread-safe. Modifier combinations are not validated.</remarks>
public abstract class TypeDeclarationBuilder<TSelf> : TypeBuilder<TSelf>
    where TSelf : TypeDeclarationBuilder<TSelf>
{
    internal readonly string keyword;
    internal bool isStatic;
    internal bool isAbstract;
    internal bool isSealed;
    internal bool isReadOnly;
    internal bool isPartial;
    internal readonly List<string> baseTypes = new();
    internal readonly List<TypeParameterBuilder> typeParameters = new();
    internal readonly List<Action<CodeWriter>> items = new();

    internal TypeDeclarationBuilder(string keyword, string name) : base(name)
    {
        this.keyword = keyword;
    }

    /// <summary>Includes the static modifier once.</summary>
    /// <returns>This type builder.</returns>
    public TSelf Static()
    {
        isStatic = true;
        return (TSelf)this;
    }

    /// <summary>Includes the abstract modifier once.</summary>
    /// <returns>This type builder.</returns>
    public TSelf Abstract()
    {
        isAbstract = true;
        return (TSelf)this;
    }

    /// <summary>Includes the sealed modifier once.</summary>
    /// <returns>This type builder.</returns>
    public TSelf Sealed()
    {
        isSealed = true;
        return (TSelf)this;
    }

    /// <summary>Includes the readonly modifier once.</summary>
    /// <returns>This type builder.</returns>
    public TSelf ReadOnly()
    {
        isReadOnly = true;
        return (TSelf)this;
    }

    /// <summary>Includes the partial modifier once, immediately before the type keyword.</summary>
    /// <returns>This type builder.</returns>
    public TSelf Partial()
    {
        isPartial = true;
        return (TSelf)this;
    }

    /// <summary>Adds a base class or interface to the end of the base type list.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is null.</exception>
    public TSelf BaseType(string type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        baseTypes.Add(type);
        return (TSelf)this;
    }

    /// <summary>Adds an unconstrained type parameter.</summary>
    /// <param name="name">The type parameter identifier. Reserved keywords are escaped.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf TypeParameter(string name)
    {
        typeParameters.Add(new TypeParameterBuilder(name));
        return (TSelf)this;
    }

    /// <summary>Immediately configures a new type parameter builder and adds it to the declaration.</summary>
    /// <param name="name">The type parameter identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf TypeParameter(string name, Action<TypeParameterBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new TypeParameterBuilder(name);
        configure(builder);
        typeParameters.Add(builder);
        return (TSelf)this;
    }

    /// <summary>Adds an empty nested class declaration.</summary>
    /// <param name="name">The class identifier. Reserved keywords are escaped.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf Class(string name) => Add(CSharp.Class(name));

    /// <summary>Immediately configures a new class builder and adds it as a nested declaration.</summary>
    /// <param name="name">The class identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf Class(string name, Action<ClassBuilder> configure) => Add(CSharp.Class(name, configure));

    /// <summary>Adds the supplied class builder by reference without copying it.</summary>
    /// <param name="classBuilder">The class builder to add.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="classBuilder"/> is null.</exception>
    public TSelf Add(ClassBuilder classBuilder)
    {
        if (classBuilder is null)
        {
            throw new ArgumentNullException(nameof(classBuilder));
        }

        items.Add(classBuilder.Write);
        return (TSelf)this;
    }

    /// <summary>Adds unvalidated source text, normalizing CRLF line endings to LF.</summary>
    /// <param name="code">The source text to write as one or more lines.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="code"/> is null.</exception>
    public TSelf Raw(string code)
    {
        if (code is null)
        {
            throw new ArgumentNullException(nameof(code));
        }

        items.Add(writer => writer.WriteLine(code));
        return (TSelf)this;
    }

    internal override void Write(CodeWriter writer)
    {
        var declaration = accessibility is null ? "" : accessibility + " ";
        if (isStatic)
        {
            declaration += "static ";
        }

        if (isAbstract)
        {
            declaration += "abstract ";
        }

        if (isSealed)
        {
            declaration += "sealed ";
        }

        if (isReadOnly)
        {
            declaration += "readonly ";
        }

        if (isPartial)
        {
            declaration += "partial ";
        }

        declaration += keyword + " " + name;
        if (typeParameters.Count > 0)
        {
            declaration += "<" + string.Join(", ", typeParameters.ConvertAll(parameter => parameter.name)) + ">";
        }

        if (baseTypes.Count > 0)
        {
            declaration += " : " + string.Join(", ", baseTypes);
        }

        foreach (var parameter in typeParameters)
        {
            var clause = parameter.RenderConstraintClause();
            if (clause.Length > 0)
            {
                declaration += " " + clause;
            }
        }

        writer.WriteLine(declaration);
        writer.OpenBlock();
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                writer.WriteBlankLine();
            }

            items[i](writer);
        }

        writer.CloseBlock();
    }
}
