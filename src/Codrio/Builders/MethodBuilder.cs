using System;
using System.Collections.Generic;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds a method declaration.</summary>
/// <remarks>Builders are mutable and are not thread-safe. Modifier combinations are not validated.</remarks>
public sealed class MethodBuilder : MemberBuilder<MethodBuilder>
{
    internal readonly string name;
    internal string returnType = "void";
    internal bool isAsync;
    internal bool isPartial;
    internal BodyBuilder? body;
    internal string? expression;
    internal readonly List<ParameterBuilder> parameters = new();
    internal readonly List<TypeParameterBuilder> typeParameters = new();

    internal MethodBuilder(string name)
    {
        this.name = Identifier.ValidateAndEscape(name, nameof(name));
    }

    /// <summary>Sets the return type, replacing any previous return type. The default is void.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <returns>This method builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is null.</exception>
    public MethodBuilder Returns(string type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        returnType = type;
        return this;
    }

    /// <summary>Adds a parameter to the end of the parameter list.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <returns>This method builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public MethodBuilder Parameter(string type, string name)
    {
        parameters.Add(new ParameterBuilder(type, name));
        return this;
    }

    /// <summary>Immediately configures a new parameter builder and adds it to the method.</summary>
    /// <param name="type">The type reference, written without validation.</param>
    /// <param name="name">The parameter identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This method builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/>, <paramref name="name"/>, or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public MethodBuilder Parameter(string type, string name, Action<ParameterBuilder> configure)
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

    /// <summary>Adds an unconstrained type parameter.</summary>
    /// <param name="name">The type parameter identifier. Reserved keywords are escaped.</param>
    /// <returns>This method builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public MethodBuilder TypeParameter(string name)
    {
        typeParameters.Add(new TypeParameterBuilder(name));
        return this;
    }

    /// <summary>Immediately configures a new type parameter builder and adds it to the declaration.</summary>
    /// <param name="name">The type parameter identifier. Reserved keywords are escaped.</param>
    /// <param name="configure">The action to run on the new builder.</param>
    /// <returns>This method builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid C# identifier.</exception>
    public MethodBuilder TypeParameter(string name, Action<TypeParameterBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new TypeParameterBuilder(name);
        configure(builder);
        typeParameters.Add(builder);
        return this;
    }

    /// <summary>Sets an empty block body, replacing any previous block or expression body.</summary>
    /// <returns>This method builder.</returns>
    public MethodBuilder Body()
    {
        body = new BodyBuilder();
        expression = null;
        return this;
    }

    /// <summary>Immediately configures a new body builder, replacing any previous block or expression body.</summary>
    /// <param name="configure">The action to run once on the new builder.</param>
    /// <returns>This method builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is null.</exception>
    public MethodBuilder Body(Action<BodyBuilder> configure)
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
    /// <returns>This method builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is null.</exception>
    public MethodBuilder Arrow(string expression)
    {
        if (expression is null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        this.expression = expression;
        body = null;
        return this;
    }

    /// <summary>Includes the async modifier once, after common modifiers and before partial.</summary>
    /// <returns>This method builder.</returns>
    public MethodBuilder Async()
    {
        isAsync = true;
        return this;
    }

    /// <summary>Includes the partial modifier once, immediately before the return type.</summary>
    /// <returns>This method builder.</returns>
    public MethodBuilder Partial()
    {
        isPartial = true;
        return this;
    }

    internal override void Write(CodeWriter writer)
    {
        var declaration = RenderModifiers();
        if (isAsync)
        {
            declaration += "async ";
        }

        if (isPartial)
        {
            declaration += "partial ";
        }

        declaration += returnType + " " + name;
        if (typeParameters.Count > 0)
        {
            declaration += "<" + string.Join(", ", typeParameters.ConvertAll(parameter => parameter.name)) + ">";
        }

        declaration += "(" + string.Join(", ", parameters.ConvertAll(parameter => parameter.RenderParameter())) + ")";
        foreach (var parameter in typeParameters)
        {
            var clause = parameter.RenderConstraintClause();
            if (clause.Length > 0)
            {
                declaration += " " + clause;
            }
        }

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
