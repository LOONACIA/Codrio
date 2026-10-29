using System;
using System.Collections.Generic;
using System.Linq;
using Codrio.Rendering;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Codrio.Tests;

public class IdentifierTests
{
    public static IEnumerable<object[]> ReservedKeywords =>
        SyntaxFacts.GetReservedKeywordKinds().Select(kind => new object[] { SyntaxFacts.GetText(kind) });

    public static IEnumerable<object[]> ContextualKeywords =>
        SyntaxFacts.GetContextualKeywordKinds().Select(kind => new object[] { SyntaxFacts.GetText(kind) });

    [Theory]
    [MemberData(nameof(ReservedKeywords))]
    public void ValidateAndEscape_EscapesReservedKeywords(string keyword)
    {
        OutputAssert.Equal("@" + keyword, Identifier.ValidateAndEscape(keyword, "name"));
    }

    [Theory]
    [MemberData(nameof(ContextualKeywords))]
    public void ValidateAndEscape_PreservesContextualKeywords(string keyword)
    {
        OutputAssert.Equal(keyword, Identifier.ValidateAndEscape(keyword, "name"));
    }

    [Theory]
    [InlineData("Foo")]
    [InlineData("Class")]
    [InlineData("_")]
    [InlineData("_1")]
    [InlineData("名前")]
    [InlineData("é")]
    [InlineData("\u01c5")]
    [InlineData("\u02b0")]
    [InlineData("\u2160")]
    [InlineData("a\u0661")]
    [InlineData("a\u203f")]
    [InlineData("a\u0301")]
    [InlineData("a\u0903")]
    [InlineData("a\u200c")]
    [InlineData("1a")]
    [InlineData("a-b")]
    [InlineData("a b")]
    [InlineData("a.b")]
    [InlineData("")]
    [InlineData("\u0661a")]
    [InlineData("\u203fa")]
    [InlineData("\u0301a")]
    [InlineData("\u0903a")]
    [InlineData("\u200ca")]
    [InlineData("a\t")]
    [InlineData("a\n")]
    [InlineData("a\0")]
    [InlineData("a😀")]
    [InlineData("\U00010400")]
    public void ValidateAndEscape_MatchesRoslynIdentifierValidation(string name)
    {
        if (SyntaxFacts.IsValidIdentifier(name))
        {
            OutputAssert.Equal(name, Identifier.ValidateAndEscape(name, "typeName"));
        }
        else
        {
            var exception = Assert.Throws<ArgumentException>(() => Identifier.ValidateAndEscape(name, "typeName"));
            Assert.Equal("typeName", exception.ParamName);
        }
    }

    [Theory]
    [InlineData("@class")]
    [InlineData("@foo")]
    [InlineData("@名前")]
    public void ValidateAndEscape_PreservesEscapedNames(string name)
    {
        OutputAssert.Equal(name, Identifier.ValidateAndEscape(name, "name"));
    }

    [Theory]
    [InlineData("@")]
    [InlineData("@1")]
    [InlineData("@@foo")]
    [InlineData("@a-b")]
    public void ValidateAndEscape_RejectsInvalidEscapedNames(string name)
    {
        var exception = Assert.Throws<ArgumentException>(() => Identifier.ValidateAndEscape(name, "memberName"));

        Assert.Equal("memberName", exception.ParamName);
    }

    [Fact]
    public void ValidateAndEscape_RejectsNullWithParameterName()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => Identifier.ValidateAndEscape(null!, "parameterName"));

        Assert.Equal("parameterName", exception.ParamName);
    }
}
