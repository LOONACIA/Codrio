using Codrio.Rendering;

namespace Codrio;

/// <summary>Provides the name and accessibility of a type declaration.</summary>
/// <typeparam name="TSelf">The concrete builder type returned by fluent methods.</typeparam>
/// <remarks>Builders are mutable and are not thread-safe.</remarks>
public abstract class TypeBuilder<TSelf> where TSelf : TypeBuilder<TSelf>
{
    internal readonly string name;
    internal string? accessibility;

    internal TypeBuilder(string name)
    {
        this.name = Identifier.ValidateAndEscape(name, nameof(name));
    }

    /// <summary>Sets public accessibility, replacing any previous accessibility.</summary>
    /// <returns>This type builder.</returns>
    public TSelf Public()
    {
        accessibility = "public";
        return (TSelf)this;
    }

    /// <summary>Sets internal accessibility, replacing any previous accessibility.</summary>
    /// <returns>This type builder.</returns>
    public TSelf Internal()
    {
        accessibility = "internal";
        return (TSelf)this;
    }

    /// <summary>Sets protected accessibility, replacing any previous accessibility.</summary>
    /// <returns>This type builder.</returns>
    public TSelf Protected()
    {
        accessibility = "protected";
        return (TSelf)this;
    }

    /// <summary>Sets private accessibility, replacing any previous accessibility.</summary>
    /// <returns>This type builder.</returns>
    public TSelf Private()
    {
        accessibility = "private";
        return (TSelf)this;
    }

    /// <summary>Sets protected internal accessibility, replacing any previous accessibility.</summary>
    /// <returns>This type builder.</returns>
    public TSelf ProtectedInternal()
    {
        accessibility = "protected internal";
        return (TSelf)this;
    }

    /// <summary>Sets private protected accessibility, replacing any previous accessibility.</summary>
    /// <returns>This type builder.</returns>
    public TSelf PrivateProtected()
    {
        accessibility = "private protected";
        return (TSelf)this;
    }

    internal abstract void Write(CodeWriter writer);

    /// <summary>Renders the type declaration.</summary>
    /// <returns>The type source with Allman braces and LF line endings.</returns>
    public override string ToString()
    {
        var writer = new CodeWriter();
        Write(writer);
        return writer.ToString();
    }
}
