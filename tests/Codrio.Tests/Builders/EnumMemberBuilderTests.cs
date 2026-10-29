using System;
using Xunit;

namespace Codrio.Tests;

public class EnumMemberBuilderTests
{
    [Fact]
    public void Documentation_PrecedesMemberWithoutAddingBlankLines()
    {
        var builder = CSharp.Enum("Color", e => e.Member("Red", m => m.Doc(d => d.Summary("Red."))).Member("Green"));

        OutputAssert.Equal("enum Color\n{\n    /// <summary>Red.</summary>\n    Red,\n    Green\n}\n", builder.ToString());
    }

    [Fact]
    public void DocumentationAndAttributes_KeepCommasOnFirstAndLastDeclarations()
    {
        EnumMemberBuilder? first = null;
        EnumMemberBuilder? last = null;
        var builder = CSharp.Enum("Color")
            .Member("Red", "1", m => first = m.Attribute("Obsolete").Doc(d => d.Summary("Red.")))
            .Member("Green", "2")
            .Member("Blue", "4", m => last = m.Doc(d => d.Summary("Blue.")).Attribute("Obsolete"));

        OutputAssert.Equal("enum Color\n{\n    /// <summary>Red.</summary>\n    [Obsolete]\n    Red = 1,\n"
            + "    Green = 2,\n    /// <summary>Blue.</summary>\n    [Obsolete]\n    Blue = 4\n}\n", builder.ToString());
        Assert.NotNull(first);
        Assert.NotNull(last);
        OutputAssert.Equal("/// <summary>Red.</summary>\n[Obsolete]\nRed = 1\n", first.ToString());
        OutputAssert.Equal("/// <summary>Blue.</summary>\n[Obsolete]\nBlue = 4\n", last.ToString());
    }

    [Fact]
    public void Doc_InvokesConfigureImmediatelyOnceAndReturnsSameBuilder()
    {
        EnumMemberBuilder? member = null;
        DocBuilder? captured = null;
        var builder = CSharp.Enum("Color").Member("Red", m => member = m);
        var calls = 0;
        Assert.NotNull(member);

        EnumMemberBuilder returned = member.Doc(d =>
        {
            calls++;
            captured = d.Summary("Red.");
        });

        Assert.Same(member, returned);
        Assert.Equal(1, calls);
        Assert.NotNull(captured);
        OutputAssert.Equal("/// <summary>Red.</summary>\n", captured.ToString());
        OutputAssert.Equal("/// <summary>Red.</summary>\nRed\n", member.ToString());
        OutputAssert.Equal("enum Color\n{\n    /// <summary>Red.</summary>\n    Red\n}\n", builder.ToString());
        OutputAssert.Equal("enum Color\n{\n    /// <summary>Red.</summary>\n    Red\n}\n", builder.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Doc_ReplacesDocumentationWithANewBuilderAndCanClearIt()
    {
        DocBuilder? first = null;
        DocBuilder? second = null;
        EnumMemberBuilder? member = null;
        var builder = CSharp.Enum("Color").Member("Red", m => member = m.Attribute("Obsolete")
            .Doc(d => first = d.Summary("First.")).Doc(d => second = d.Summary("Last.")));

        Assert.NotSame(first, second);
        Assert.NotNull(member);
        OutputAssert.Equal("/// <summary>Last.</summary>\n[Obsolete]\nRed\n", member.ToString());
        OutputAssert.Equal("enum Color\n{\n    /// <summary>Last.</summary>\n    [Obsolete]\n    Red\n}\n", builder.ToString());
        member.Doc(_ => { });
        OutputAssert.Equal("[Obsolete]\nRed\n", member.ToString());
        OutputAssert.Equal("enum Color\n{\n    [Obsolete]\n    Red\n}\n", builder.ToString());
    }

    [Fact]
    public void Doc_NullConfigureThrowsWithoutReplacingDocumentation()
    {
        CSharp.Enum("Color").Member("Red", m =>
        {
            m.Doc(d => d.Summary("Existing."));
            Assert.Throws<ArgumentNullException>("configure", () => m.Doc(null!));
            OutputAssert.Equal("/// <summary>Existing.</summary>\nRed\n", m.ToString());
        });
    }

    [Theory]
    [InlineData(false, false, "Red\n")]
    [InlineData(true, false, "Red = 1\n")]
    [InlineData(false, true, "[Obsolete]\nRed\n")]
    [InlineData(true, true, "[Obsolete]\nRed = 1\n")]
    public void ToString_RendersCapturedMemberWithoutComma(bool withValue, bool withAttribute, string expected)
    {
        EnumMemberBuilder? member = null;
        var builder = CSharp.Enum("Color");
        Action<EnumMemberBuilder> configure = m =>
        {
            member = m;
            if (withAttribute)
            {
                Assert.Same(m, m.Attribute("Obsolete"));
            }
        };

        if (withValue)
        {
            Assert.Same(builder, builder.Member("Red", "1", configure));
        }
        else
        {
            Assert.Same(builder, builder.Member("Red", configure));
        }

        builder.Member("Blue");
        Assert.NotNull(member);
        OutputAssert.Equal(expected, member.ToString());
        OutputAssert.Equal(expected, member.ToString());
    }

    [Theory]
    [InlineData(false, "enum Color\n{\n    [Obsolete]\n    Red\n}\n")]
    [InlineData(true, "enum Color\n{\n    [Obsolete]\n    Red = 1\n}\n")]
    public void Member_ConfiguresImmediatelyOnceAndSharesInstance(bool withValue, string expected)
    {
        EnumMemberBuilder? member = null;
        var calls = 0;
        var builder = CSharp.Enum("Color");
        Action<EnumMemberBuilder> configure = m => { member = m; calls++; };

        if (withValue)
        {
            builder.Member("Red", "1", configure);
        }
        else
        {
            builder.Member("Red", configure);
        }

        Assert.Equal(1, calls);
        Assert.NotNull(member);
        member.Attribute("Obsolete");
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Attributes_RenderWithTypeAttributesAndUnconfiguredMembers()
    {
        var builder = CSharp.Enum("Access", e => e.Attribute("Flags")
            .Member("Read", "1", m => m.Attribute("Description", "\"read\""))
            .Member("Write", "2")
            .Member("Legacy", m => m.Attribute("Obsolete")));

        OutputAssert.Equal("[Flags]\nenum Access\n{\n    [Description(\"read\")]\n    Read = 1,\n    Write = 2,\n    [Obsolete]\n    Legacy\n}\n", builder.ToString());
    }

    [Fact]
    public void Attributes_OnFirstMiddleAndLastMembersKeepCommasOnDeclarations()
    {
        var builder = CSharp.Enum("E")
            .Member("First", m => m.Attribute("FirstAttribute"))
            .Member("Middle", "2", m => m.Attribute("SecondAttribute").Attribute("ThirdAttribute").Attribute("SecondAttribute"))
            .Member("Last", m => m.Attribute("LastAttribute"));

        OutputAssert.Equal("enum E\n{\n    [FirstAttribute]\n    First,\n    [SecondAttribute]\n    [ThirdAttribute]\n    [SecondAttribute]\n    Middle = 2,\n    [LastAttribute]\n    Last\n}\n", builder.ToString());
    }

    [Fact]
    public void Members_EscapeKeywordsAndWriteValuesVerbatim()
    {
        var builder = CSharp.Enum("E")
            .Member("class", m => m.Attribute("Obsolete"))
            .Member("event", "1 << 2", m => m.Attribute("Description", "\"event\""))
            .Member("Empty", "", _ => { })
            .Member("Invalid", "not an expression", _ => { });

        OutputAssert.Equal("enum E\n{\n    [Obsolete]\n    @class,\n    [Description(\"event\")]\n    @event = 1 << 2,\n    Empty = ,\n    Invalid = not an expression\n}\n", builder.ToString());
    }

    [Fact]
    public void Attributes_KeepRawNamesAndArgumentsAndCaptureArrayAtCallTime()
    {
        var arguments = new[] { "\"message\"", "true" };
        var builder = CSharp.Enum("E").Member("A", m => m
            .Attribute("field: Obsolete", arguments)
            .Attribute("", Array.Empty<string>()));
        arguments[0] = "\"changed\"";

        OutputAssert.Equal("enum E\n{\n    [field: Obsolete(\"message\", true)]\n    []\n    A\n}\n", builder.ToString());
    }

    [Fact]
    public void NestedEnumInBlockNamespace_IndentsAttributesAndMembers()
    {
        var file = CSharp.File().Namespace("Example", n => n.Class("Outer", c => c
            .Enum("E", e => e.Attribute("System.Flags")
                .Member("A", "1", m => m.Attribute("System.Obsolete"))
                .Member("B", "2"))));

        OutputAssert.Equal("namespace Example\n{\n    class Outer\n    {\n        [System.Flags]\n        enum E\n        {\n            [System.Obsolete]\n            A = 1,\n            B = 2\n        }\n    }\n}\n", file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("1Value")]
    [InlineData("Value.Item")]
    [InlineData("Value Item")]
    [InlineData("Value-Item")]
    [InlineData("@@Value")]
    public void InvalidNames_ThrowAtCallTimeBeforeConfiguringOrAdding(string name)
    {
        var builder = CSharp.Enum("E");
        var configured = false;

        Assert.Throws<ArgumentException>("name", () => builder.Member(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => builder.Member(name, "1", _ => configured = true));
        Assert.False(configured);
        OutputAssert.Equal("enum E\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void NullArguments_ThrowInOrderBeforeConfiguringOrAdding()
    {
        var builder = CSharp.Enum("E");
        var configured = false;

        Assert.Throws<ArgumentNullException>("name", () => builder.Member(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("name", () => builder.Member(null!, "1", _ => configured = true));
        Assert.Throws<ArgumentNullException>("value", () => builder.Member("A", null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("value", () => builder.Member(null!, null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("value", () => builder.Member("invalid name", null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Member("A", (Action<EnumMemberBuilder>)null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Member(null!, (Action<EnumMemberBuilder>)null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Member("invalid name", (Action<EnumMemberBuilder>)null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Member("A", "1", null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Member(null!, null!, null!));
        Assert.False(configured);
        OutputAssert.Equal("enum E\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void Attribute_NullArgumentsThrowWithoutAddingAttribute()
    {
        var builder = CSharp.Enum("E").Member("A", m =>
        {
            Assert.Throws<ArgumentNullException>("name", () => m.Attribute(null!));
            Assert.Throws<ArgumentNullException>("arguments", () => m.Attribute("Obsolete", null!));
            Assert.Throws<ArgumentNullException>("arguments", () => m.Attribute("Obsolete", "\"message\"", null!));
            OutputAssert.Equal("A\n", m.ToString());
        });

        OutputAssert.Equal("enum E\n{\n    A\n}\n", builder.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Configure_ThrowingDoesNotAddMember(bool withValue)
    {
        var builder = CSharp.Enum("E");
        Action<EnumMemberBuilder> configure = m =>
        {
            m.Attribute("Obsolete");
            throw new InvalidOperationException();
        };

        Assert.Throws<InvalidOperationException>(() => withValue
            ? builder.Member("A", "1", configure)
            : builder.Member("A", configure));
        OutputAssert.Equal("enum E\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void CompleteFile_WithFlagsAndAttributedMembers_Compiles()
    {
        var file = CSharp.File().Using("System").Namespace("Example", n => n
            .Enum("Access", e => e.Public().Attribute("Flags").UnderlyingType("byte")
                .Member("Read", "1", m => m.Attribute("System.ComponentModel.Description", "\"read\""))
                .Member("Write", "2")
                .Member("Legacy", "4", m => m.Attribute("Obsolete", "\"Use Write.\""))
                .Member("class", "8", m => m.Attribute("System.ComponentModel.Description", "\"keyword\""))));

        OutputAssert.Equal("using System;\n\nnamespace Example\n{\n    [Flags]\n    public enum Access : byte\n    {\n        [System.ComponentModel.Description(\"read\")]\n        Read = 1,\n        Write = 2,\n        [Obsolete(\"Use Write.\")]\n        Legacy = 4,\n        [System.ComponentModel.Description(\"keyword\")]\n        @class = 8\n    }\n}\n", file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }
}
