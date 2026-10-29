using System;
using Codrio.Rendering;

namespace Codrio;

/// <summary>Builds a field-like event declaration.</summary>
/// <remarks>Builders are mutable and are not thread-safe. Modifier combinations are not validated. Only field-like events are supported.</remarks>
public sealed class EventBuilder : MemberBuilder<EventBuilder>
{
    internal readonly string type;
    internal readonly string name;

    internal EventBuilder(string type, string name)
    {
        this.type = type ?? throw new ArgumentNullException(nameof(type));
        this.name = Identifier.ValidateAndEscape(name, nameof(name));
    }

    internal override void Write(CodeWriter writer)
    {
        WritePreamble(writer);
        writer.WriteLine(RenderModifiers() + "event " + type + " " + name + ";");
    }
}
