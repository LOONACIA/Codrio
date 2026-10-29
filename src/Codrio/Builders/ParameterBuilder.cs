using System;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds a parameter declaration with modifiers and an optional default value.</summary>
/// <remarks>Builders are mutable and are not thread-safe. Modifier combinations are not validated.</remarks>
public sealed class ParameterBuilder
{
    internal readonly string type;
    internal readonly string name;
    internal bool isRef;
    internal bool isOut;
    internal bool isIn;
    internal bool isParams;
    internal bool isThis;
    internal string? defaultExpression;

    internal ParameterBuilder(string type, string name)
    {
        this.type = type ?? throw new ArgumentNullException(nameof(type));
        this.name = Identifier.ValidateAndEscape(name, nameof(name));
    }

    /// <summary>Includes the ref modifier once.</summary>
    /// <returns>This parameter builder.</returns>
    public ParameterBuilder Ref()
    {
        isRef = true;
        return this;
    }

    /// <summary>Includes the out modifier once.</summary>
    /// <returns>This parameter builder.</returns>
    public ParameterBuilder Out()
    {
        isOut = true;
        return this;
    }

    /// <summary>Includes the in modifier once.</summary>
    /// <returns>This parameter builder.</returns>
    public ParameterBuilder In()
    {
        isIn = true;
        return this;
    }

    /// <summary>Includes the params modifier once.</summary>
    /// <returns>This parameter builder.</returns>
    public ParameterBuilder Params()
    {
        isParams = true;
        return this;
    }

    /// <summary>Includes the this modifier once.</summary>
    /// <returns>This parameter builder.</returns>
    public ParameterBuilder This()
    {
        isThis = true;
        return this;
    }

    /// <summary>Sets the default value expression, replacing any previous value.</summary>
    /// <param name="expression">The default value expression, written without validation.</param>
    /// <returns>This parameter builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="expression"/> is null.</exception>
    public ParameterBuilder Default(string expression)
    {
        defaultExpression = expression ?? throw new ArgumentNullException(nameof(expression));
        return this;
    }

    internal string RenderParameter()
    {
        var declaration = "";
        if (isThis)
        {
            declaration += "this ";
        }

        if (isRef)
        {
            declaration += "ref ";
        }

        if (isOut)
        {
            declaration += "out ";
        }

        if (isIn)
        {
            declaration += "in ";
        }

        if (isParams)
        {
            declaration += "params ";
        }

        declaration += type + " " + name;
        if (defaultExpression is not null)
        {
            declaration += " = " + defaultExpression;
        }

        return declaration;
    }

    /// <summary>Renders the parameter declaration.</summary>
    /// <returns>The parameter declaration without a line ending.</returns>
    public override string ToString() => RenderParameter();
}
