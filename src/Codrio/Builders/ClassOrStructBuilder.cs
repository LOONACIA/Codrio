using System;
using System.Collections.Generic;

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

    internal override string ParameterList() => primaryParameters.Count == 0
        ? ""
        : "(" + string.Join(", ", primaryParameters.ConvertAll(parameter => parameter.RenderParameter())) + ")";
}
