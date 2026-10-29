using System;
using System.Collections.Generic;
using System.Globalization;

namespace Codrio.Rendering;

internal static class Identifier
{
    private static readonly HashSet<string> ReservedKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum",
        "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto",
        "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
        "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
        "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
        "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
        "__arglist", "__makeref", "__reftype", "__refvalue"
    };

    internal static string ValidateAndEscape(string name, string paramName)
    {
        if (name is null)
        {
            throw new ArgumentNullException(paramName);
        }

        var start = name.Length > 0 && name[0] == '@' ? 1 : 0;
        if (start == name.Length || !IsStartCharacter(name[start]))
        {
            throw new ArgumentException("The name must be a valid C# identifier.", paramName);
        }

        for (var i = start + 1; i < name.Length; i++)
        {
            if (!IsPartCharacter(name[i]))
            {
                throw new ArgumentException("The name must be a valid C# identifier.", paramName);
            }
        }

        return ReservedKeywords.Contains(name) ? "@" + name : name;
    }

    private static bool IsStartCharacter(char character) =>
        character == '_' || char.GetUnicodeCategory(character) is
            UnicodeCategory.UppercaseLetter or
            UnicodeCategory.LowercaseLetter or
            UnicodeCategory.TitlecaseLetter or
            UnicodeCategory.ModifierLetter or
            UnicodeCategory.OtherLetter or
            UnicodeCategory.LetterNumber;

    private static bool IsPartCharacter(char character) =>
        IsStartCharacter(character) || char.GetUnicodeCategory(character) is
            UnicodeCategory.DecimalDigitNumber or
            UnicodeCategory.ConnectorPunctuation or
            UnicodeCategory.NonSpacingMark or
            UnicodeCategory.SpacingCombiningMark or
            UnicodeCategory.Format;
}
