using Codrio.Rendering;

namespace Codrio;

/// <summary>Provides accessibility and common modifiers for a member declaration.</summary>
/// <typeparam name="TSelf">The concrete builder type returned by fluent methods.</typeparam>
/// <remarks>Builders are mutable and are not thread-safe. Modifier combinations are not validated.</remarks>
public abstract class MemberBuilder<TSelf> where TSelf : MemberBuilder<TSelf>
{
    internal string? accessibility;
    internal bool isStatic;
    internal bool isNew;
    internal bool isVirtual;
    internal bool isAbstract;
    internal bool isSealed;
    internal bool isOverride;
    internal bool isReadOnly;

    internal MemberBuilder()
    {
    }

    /// <summary>Sets public accessibility, replacing any previous accessibility.</summary>
    /// <returns>This member builder.</returns>
    public TSelf Public()
    {
        accessibility = "public";
        return (TSelf)this;
    }

    /// <summary>Sets internal accessibility, replacing any previous accessibility.</summary>
    /// <returns>This member builder.</returns>
    public TSelf Internal()
    {
        accessibility = "internal";
        return (TSelf)this;
    }

    /// <summary>Sets protected accessibility, replacing any previous accessibility.</summary>
    /// <returns>This member builder.</returns>
    public TSelf Protected()
    {
        accessibility = "protected";
        return (TSelf)this;
    }

    /// <summary>Sets private accessibility, replacing any previous accessibility.</summary>
    /// <returns>This member builder.</returns>
    public TSelf Private()
    {
        accessibility = "private";
        return (TSelf)this;
    }

    /// <summary>Sets protected internal accessibility, replacing any previous accessibility.</summary>
    /// <returns>This member builder.</returns>
    public TSelf ProtectedInternal()
    {
        accessibility = "protected internal";
        return (TSelf)this;
    }

    /// <summary>Sets private protected accessibility, replacing any previous accessibility.</summary>
    /// <returns>This member builder.</returns>
    public TSelf PrivateProtected()
    {
        accessibility = "private protected";
        return (TSelf)this;
    }

    /// <summary>Includes the static modifier once.</summary>
    /// <returns>This member builder.</returns>
    public TSelf Static()
    {
        isStatic = true;
        return (TSelf)this;
    }

    /// <summary>Includes the new modifier once.</summary>
    /// <returns>This member builder.</returns>
    public TSelf New()
    {
        isNew = true;
        return (TSelf)this;
    }

    /// <summary>Includes the virtual modifier once.</summary>
    /// <returns>This member builder.</returns>
    public TSelf Virtual()
    {
        isVirtual = true;
        return (TSelf)this;
    }

    /// <summary>Includes the abstract modifier once.</summary>
    /// <returns>This member builder.</returns>
    public TSelf Abstract()
    {
        isAbstract = true;
        return (TSelf)this;
    }

    /// <summary>Includes the sealed modifier once.</summary>
    /// <returns>This member builder.</returns>
    public TSelf Sealed()
    {
        isSealed = true;
        return (TSelf)this;
    }

    /// <summary>Includes the override modifier once.</summary>
    /// <returns>This member builder.</returns>
    public TSelf Override()
    {
        isOverride = true;
        return (TSelf)this;
    }

    /// <summary>Includes the readonly modifier once.</summary>
    /// <returns>This member builder.</returns>
    public TSelf ReadOnly()
    {
        isReadOnly = true;
        return (TSelf)this;
    }

    internal string RenderModifiers()
    {
        var modifiers = accessibility is null ? "" : accessibility + " ";
        if (isStatic)
        {
            modifiers += "static ";
        }

        if (isNew)
        {
            modifiers += "new ";
        }

        if (isVirtual)
        {
            modifiers += "virtual ";
        }

        if (isAbstract)
        {
            modifiers += "abstract ";
        }

        if (isSealed)
        {
            modifiers += "sealed ";
        }

        if (isOverride)
        {
            modifiers += "override ";
        }

        if (isReadOnly)
        {
            modifiers += "readonly ";
        }

        return modifiers;
    }

    internal abstract void Write(CodeWriter writer);

    /// <summary>Renders the member declaration.</summary>
    /// <returns>The member source with LF line endings.</returns>
    public override string ToString()
    {
        var writer = new CodeWriter();
        Write(writer);
        return writer.ToString();
    }
}
