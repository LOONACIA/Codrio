using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Codrio.Rendering;

internal static class TypeName
{
    private static readonly Dictionary<Type, string> KeywordAliases = new()
    {
        { typeof(bool), "bool" },
        { typeof(byte), "byte" },
        { typeof(sbyte), "sbyte" },
        { typeof(char), "char" },
        { typeof(decimal), "decimal" },
        { typeof(double), "double" },
        { typeof(float), "float" },
        { typeof(int), "int" },
        { typeof(uint), "uint" },
        { typeof(long), "long" },
        { typeof(ulong), "ulong" },
        { typeof(short), "short" },
        { typeof(ushort), "ushort" },
        { typeof(object), "object" },
        { typeof(string), "string" },
        { typeof(void), "void" }
    };

    internal static string Render(Type type, string paramName)
    {
        if (type is null)
        {
            throw new ArgumentNullException(paramName);
        }

        if (type.IsByRef || type.ContainsGenericParameters)
        {
            throw new ArgumentException("The type cannot be written as a C# type name.", paramName);
        }

        if (KeywordAliases.TryGetValue(type, out var alias))
        {
            return alias;
        }

        if (type.IsArray)
        {
            var suffix = new StringBuilder();
            do
            {
                var rank = type.GetArrayRank();
                var elementType = type.GetElementType()!;
                if (rank == 1 && type != elementType.MakeArrayType())
                {
                    throw new ArgumentException("The type cannot be written as a C# type name.", paramName);
                }

                suffix.Append('[').Append(',', rank - 1).Append(']');
                type = elementType;
            }
            while (type.IsArray);

            return Render(type, paramName) + suffix;
        }

        if (type.IsPointer)
        {
            return Render(type.GetElementType()!, paramName) + "*";
        }

        var path = new Stack<Type>();
        for (Type? level = type; level is not null; level = level.DeclaringType)
        {
            path.Push(level);
        }

        var result = new StringBuilder("global::");
        var ns = path.Peek().Namespace;
        if (!string.IsNullOrEmpty(ns))
        {
            foreach (var part in ns!.Split('.'))
            {
                result.Append(Identifier.ValidateAndEscape(part, paramName)).Append('.');
            }
        }

        var arguments = type.GetGenericArguments();
        var argumentIndex = 0;
        while (path.Count > 0)
        {
            var name = path.Pop().Name;
            var arityStart = name.IndexOf('`');
            var arity = 0;
            if (arityStart >= 0)
            {
                if (!int.TryParse(name.Substring(arityStart + 1), NumberStyles.None, CultureInfo.InvariantCulture, out arity)
                    || arity <= 0 || arity > arguments.Length - argumentIndex)
                {
                    throw new ArgumentException("The type cannot be written as a C# type name.", paramName);
                }

                name = name.Substring(0, arityStart);
            }

            result.Append(Identifier.ValidateAndEscape(name, paramName));
            if (arity > 0)
            {
                result.Append('<');
                for (var i = 0; i < arity; i++)
                {
                    if (i > 0)
                    {
                        result.Append(", ");
                    }

                    result.Append(Render(arguments[argumentIndex++], paramName));
                }

                result.Append('>');
            }

            if (path.Count > 0)
            {
                result.Append('.');
            }
        }

        return result.ToString();
    }
}
