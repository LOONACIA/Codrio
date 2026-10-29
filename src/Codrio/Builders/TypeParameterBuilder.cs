using System;
using System.Collections.Generic;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds the constraints for a generic type parameter.</summary>
/// <remarks>Builders are mutable and are not thread-safe.</remarks>
public sealed class TypeParameterBuilder
{
    internal readonly string name;
    internal readonly List<string> constraints = new();

    internal TypeParameterBuilder(string name)
    {
        this.name = Identifier.ValidateAndEscape(name, nameof(name));
    }

    /// <summary>Adds a constraint to the end of the constraint list.</summary>
    /// <param name="constraint">The constraint text, written without validation.</param>
    /// <returns>This type parameter builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="constraint"/> is null.</exception>
    public TypeParameterBuilder Constraint(string constraint)
    {
        if (constraint is null)
        {
            throw new ArgumentNullException(nameof(constraint));
        }

        constraints.Add(constraint);
        return this;
    }

    /// <summary>Adds a constraint to the end of the constraint list.</summary>
    /// <param name="type">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</param>
    /// <returns>This type parameter builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="type"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="type"/> cannot be written as a C# type name, such as an open generic, by-ref, or compiler-generated type.</exception>
    public TypeParameterBuilder Constraint(Type type) => Constraint(TypeName.Render(type, nameof(type)));

    /// <summary>Adds a constraint to the end of the constraint list.</summary>
    /// <typeparam name="T">The type, converted when this method is called to a C# type name: a <c>global::</c> qualified name, or a keyword such as <c>int</c> for types with a keyword alias.</typeparam>
    /// <returns>This type parameter builder.</returns>
    /// <exception cref="ArgumentException"><typeparamref name="T"/> cannot be written as a C# type name, such as a compiler-generated type.</exception>
    public TypeParameterBuilder Constraint<T>() => Constraint(typeof(T));

    internal string RenderConstraintClause() => constraints.Count == 0
        ? ""
        : "where " + name + " : " + string.Join(", ", constraints);

    /// <summary>Renders the type parameter's constraint clause.</summary>
    /// <returns>The where clause without a line ending, or an empty string if there are no constraints.</returns>
    public override string ToString() => RenderConstraintClause();
}
