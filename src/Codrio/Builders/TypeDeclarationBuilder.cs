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

    /// <summary>Adds a field-like event declaration.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The event identifier. Reserved keywords are escaped.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf Event(string type, string name) => Add(CSharp.Event(type, name));

    /// <summary>Immediately configures a new field-like event builder and adds it to the declaration.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The event identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf Event(string type, string name, Action<EventBuilder> configure) => Add(CSharp.Event(type, name, configure));

    /// <summary>Adds the supplied event builder by reference without copying it.</summary>
    /// <param name="eventBuilder">The event builder to add.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="eventBuilder"/> is null.</exception>
    public TSelf Add(EventBuilder eventBuilder)
    {
        if (eventBuilder is null)
        {
            throw new ArgumentNullException(nameof(eventBuilder));
        }

        items.Add(eventBuilder.Write);
        return (TSelf)this;
    }

    /// <summary>Adds a property declaration with get and set accessors.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The property identifier. Reserved keywords are escaped.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf Property(string type, string name) => Add(CSharp.Property(type, name));

    /// <summary>Immediately configures a new property builder and adds it to the declaration.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The property identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf Property(string type, string name, Action<PropertyBuilder> configure) => Add(CSharp.Property(type, name, configure));

    /// <summary>Adds the supplied property builder by reference without copying it.</summary>
    /// <param name="propertyBuilder">The property builder to add.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="propertyBuilder"/> is null.</exception>
    public TSelf Add(PropertyBuilder propertyBuilder)
    {
        if (propertyBuilder is null)
        {
            throw new ArgumentNullException(nameof(propertyBuilder));
        }

        items.Add(propertyBuilder.Write);
        return (TSelf)this;
    }

    /// <summary>Adds an indexer declaration with get and set accessors and no parameters.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is null.</exception>
    public TSelf Indexer(string type) => Add(CSharp.Indexer(type));

    /// <summary>Immediately configures a new indexer builder and adds it to the declaration.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="configure"/> is null.</exception>
    public TSelf Indexer(string type, Action<IndexerBuilder> configure) => Add(CSharp.Indexer(type, configure));

    /// <summary>Adds the supplied indexer builder by reference without copying it.</summary>
    /// <param name="indexerBuilder">The indexer builder to add.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="indexerBuilder"/> is null.</exception>
    public TSelf Add(IndexerBuilder indexerBuilder)
    {
        if (indexerBuilder is null)
        {
            throw new ArgumentNullException(nameof(indexerBuilder));
        }

        items.Add(indexerBuilder.Write);
        return (TSelf)this;
    }

    /// <summary>Adds a method declaration with a void return type and no body.</summary>
    /// <param name="name">The method identifier. Reserved keywords are escaped.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf Method(string name) => Add(CSharp.Method(name));

    /// <summary>Immediately configures a new method builder and adds it to the declaration.</summary>
    /// <param name="name">The method identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf Method(string name, Action<MethodBuilder> configure) => Add(CSharp.Method(name, configure));

    /// <summary>Adds the supplied method builder by reference without copying it.</summary>
    /// <param name="methodBuilder">The method builder to add.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="methodBuilder"/> is null.</exception>
    public TSelf Add(MethodBuilder methodBuilder)
    {
        if (methodBuilder is null)
        {
            throw new ArgumentNullException(nameof(methodBuilder));
        }

        items.Add(methodBuilder.Write);
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

    /// <summary>Adds an empty nested struct declaration.</summary>
    /// <param name="name">The struct identifier. Reserved keywords are escaped.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf Struct(string name) => Add(CSharp.Struct(name));

    /// <summary>Immediately configures a new struct builder and adds it as a nested declaration.</summary>
    /// <param name="name">The struct identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf Struct(string name, Action<StructBuilder> configure) => Add(CSharp.Struct(name, configure));

    /// <summary>Adds the supplied struct builder by reference without copying it.</summary>
    /// <param name="structBuilder">The struct builder to add.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="structBuilder"/> is null.</exception>
    public TSelf Add(StructBuilder structBuilder)
    {
        if (structBuilder is null)
        {
            throw new ArgumentNullException(nameof(structBuilder));
        }

        items.Add(structBuilder.Write);
        return (TSelf)this;
    }

    /// <summary>Adds an empty nested interface declaration.</summary>
    /// <param name="name">The interface identifier. Reserved keywords are escaped.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf Interface(string name) => Add(CSharp.Interface(name));

    /// <summary>Immediately configures a new interface builder and adds it as a nested declaration.</summary>
    /// <param name="name">The interface identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf Interface(string name, Action<InterfaceBuilder> configure) => Add(CSharp.Interface(name, configure));

    /// <summary>Adds the supplied interface builder by reference without copying it.</summary>
    /// <param name="interfaceBuilder">The interface builder to add.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="interfaceBuilder"/> is null.</exception>
    public TSelf Add(InterfaceBuilder interfaceBuilder)
    {
        if (interfaceBuilder is null)
        {
            throw new ArgumentNullException(nameof(interfaceBuilder));
        }

        items.Add(interfaceBuilder.Write);
        return (TSelf)this;
    }

    /// <summary>Adds an empty nested record declaration.</summary>
    /// <param name="name">The record identifier. Reserved keywords are escaped.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf Record(string name) => Add(CSharp.Record(name));

    /// <summary>Immediately configures a new record builder and adds it as a nested declaration.</summary>
    /// <param name="name">The record identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf Record(string name, Action<RecordBuilder> configure) => Add(CSharp.Record(name, configure));

    /// <summary>Adds an empty nested record struct declaration.</summary>
    /// <param name="name">The record struct identifier. Reserved keywords are escaped.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf RecordStruct(string name) => Add(CSharp.RecordStruct(name));

    /// <summary>Immediately configures a new record struct builder and adds it as a nested declaration.</summary>
    /// <param name="name">The record struct identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf RecordStruct(string name, Action<RecordBuilder> configure) => Add(CSharp.RecordStruct(name, configure));

    /// <summary>Adds the supplied record builder by reference without copying it.</summary>
    /// <param name="recordBuilder">The record builder to add.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recordBuilder"/> is null.</exception>
    public TSelf Add(RecordBuilder recordBuilder)
    {
        if (recordBuilder is null)
        {
            throw new ArgumentNullException(nameof(recordBuilder));
        }

        items.Add(recordBuilder.Write);
        return (TSelf)this;
    }

    /// <summary>Adds an empty nested enum declaration.</summary>
    /// <param name="name">The enum identifier. Reserved keywords are escaped.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf Enum(string name) => Add(CSharp.Enum(name));

    /// <summary>Immediately configures a new enum builder and adds it as a nested declaration.</summary>
    /// <param name="name">The enum identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public TSelf Enum(string name, Action<EnumBuilder> configure) => Add(CSharp.Enum(name, configure));

    /// <summary>Adds the supplied enum builder by reference without copying it.</summary>
    /// <param name="enumBuilder">The enum builder to add.</param>
    /// <returns>This type builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="enumBuilder"/> is null.</exception>
    public TSelf Add(EnumBuilder enumBuilder)
    {
        if (enumBuilder is null)
        {
            throw new ArgumentNullException(nameof(enumBuilder));
        }

        items.Add(enumBuilder.Write);
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

    internal virtual bool EndsWithSemicolonWhenEmpty => false;

    internal virtual string ParameterList() => "";

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

        declaration += ParameterList();

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

        if (EndsWithSemicolonWhenEmpty && items.Count == 0)
        {
            writer.WriteLine(declaration + ";");
            return;
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
