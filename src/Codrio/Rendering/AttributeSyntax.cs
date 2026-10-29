using System;

namespace Codrio.Rendering;

internal static class AttributeSyntax
{
    internal static string Render(string name, string[] arguments)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        if (arguments is null)
        {
            throw new ArgumentNullException(nameof(arguments));
        }

        foreach (var argument in arguments)
        {
            if (argument is null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }
        }

        return arguments.Length == 0 ? "[" + name + "]" : "[" + name + "(" + string.Join(", ", arguments) + ")]";
    }
}
