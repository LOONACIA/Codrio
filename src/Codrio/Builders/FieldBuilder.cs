using System;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds a field declaration.</summary>
/// <remarks>Builders are mutable and are not thread-safe. Modifier combinations are not validated.</remarks>
public sealed class FieldBuilder : MemberBuilder<FieldBuilder>
{
    internal readonly string type;
    internal readonly string name;
    internal bool isConst;
    internal string? expression;

    internal FieldBuilder(string type, string name)
    {
        this.type = type ?? throw new ArgumentNullException(nameof(type));
        this.name = Identifier.ValidateAndEscape(name, nameof(name));
    }

    /// <summary>Includes the const modifier once, immediately before the field type.</summary>
    /// <returns>This field builder.</returns>
    public FieldBuilder Const()
    {
        isConst = true;
        return this;
    }

    /// <summary>Sets the initializer expression, replacing any previous expression.</summary>
    /// <param name="expression">The initializer expression, written without validation.</param>
    /// <returns>This field builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is null.</exception>
    public FieldBuilder Initializer(string expression)
    {
        this.expression = expression ?? throw new ArgumentNullException(nameof(expression));
        return this;
    }

    internal override void Write(CodeWriter writer)
    {
        var declaration = RenderModifiers();
        if (isConst)
        {
            declaration += "const ";
        }

        declaration += type + " " + name;
        if (expression is not null)
        {
            declaration += " = " + expression;
        }

        writer.WriteLine(declaration + ";");
    }
}
