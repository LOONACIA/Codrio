using System;
using Xunit;

namespace Codrio.Tests;

public class ClassBuilderTests
{
    [Theory]
    [InlineData("Foo", "class Foo\n{\n}\n")]
    [InlineData("class", "class @class\n{\n}\n")]
    [InlineData("@class", "class @class\n{\n}\n")]
    public void ToString_RendersEmptyClass(string name, string expected)
    {
        var builder = CSharp.Class(name);

        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Fact]
    public void Class_InvokesConfigureImmediatelyOnReturnedBuilder()
    {
        ClassBuilder? configured = null;
        var calls = 0;

        var builder = CSharp.Class("Foo", c =>
        {
            configured = c;
            calls++;
        });

        Assert.Same(builder, configured);
        Assert.Equal(1, calls);
        OutputAssert.Equal("class Foo\n{\n}\n", builder.ToString());
        OutputAssert.Equal("class Foo\n{\n}\n", builder.ToString());
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1Foo")]
    [InlineData("Foo.Bar")]
    [InlineData("Foo Bar")]
    [InlineData("Foo-Bar")]
    [InlineData("@@Foo")]
    public void Class_RejectsInvalidNameAtCallTime(string name)
    {
        Assert.Throws<ArgumentException>("name", () => CSharp.Class(name));
        var configured = false;
        Assert.Throws<ArgumentException>("name", () => CSharp.Class(name, _ => configured = true));
        Assert.False(configured);
    }

    [Fact]
    public void Class_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Class(null!));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Class(null!, _ => { }));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Class("Foo", null!));
    }

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected")]
    [InlineData("private")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public void Accessibility_RendersSelectedModifier(string accessibility)
    {
        var builder = CSharp.Class("Foo");
        _ = accessibility switch
        {
            "public" => builder.Public(),
            "internal" => builder.Internal(),
            "protected" => builder.Protected(),
            "private" => builder.Private(),
            "protected internal" => builder.ProtectedInternal(),
            "private protected" => builder.PrivateProtected(),
            _ => throw new ArgumentException(nameof(accessibility))
        };

        OutputAssert.Equal(accessibility + " class Foo\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void Accessibility_LastCallWins()
    {
        var builder = CSharp.Class("Foo").Public().Internal();
        OutputAssert.Equal("internal class Foo\n{\n}\n", builder.ToString());
        OutputAssert.Equal("protected class Foo\n{\n}\n", builder.Protected().ToString());
        OutputAssert.Equal("private class Foo\n{\n}\n", builder.Private().ToString());
        OutputAssert.Equal("protected internal class Foo\n{\n}\n", builder.ProtectedInternal().ToString());
        OutputAssert.Equal("private protected class Foo\n{\n}\n", builder.PrivateProtected().ToString());
        OutputAssert.Equal("public class Foo\n{\n}\n", builder.Public().ToString());
    }

    [Theory]
    [InlineData("static")]
    [InlineData("abstract")]
    [InlineData("sealed")]
    [InlineData("readonly")]
    [InlineData("partial")]
    public void Modifier_RendersWithoutAccessibility(string modifier)
    {
        var builder = CSharp.Class("Foo");
        _ = modifier switch
        {
            "static" => builder.Static(),
            "abstract" => builder.Abstract(),
            "sealed" => builder.Sealed(),
            "readonly" => builder.ReadOnly(),
            "partial" => builder.Partial(),
            _ => throw new ArgumentException(nameof(modifier))
        };

        OutputAssert.Equal(modifier + " class Foo\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void Modifiers_RenderInFixedOrderWithoutDuplicates()
    {
        var builder = CSharp.Class("Foo").Partial().ReadOnly().Sealed().Abstract().Static().Public();
        const string expected = "public static abstract sealed readonly partial class Foo\n{\n}\n";

        OutputAssert.Equal(expected, builder.ToString());
        builder.Static().Abstract().Sealed().ReadOnly().Partial();
        OutputAssert.Equal(expected, builder.ToString());
    }

    [Fact]
    public void NestedClasses_RenderLambdaAndAddItemsInOrder()
    {
        var builder = CSharp.Class("Outer")
            .Class("First")
            .Class("Second", c => c.Private())
            .Add(CSharp.Class("Third").Public());

        OutputAssert.Equal(
            "class Outer\n{\n    class First\n    {\n    }\n\n    private class Second\n    {\n    }\n\n    public class Third\n    {\n    }\n}\n",
            builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Fact]
    public void NestedClass_ConfiguresImmediatelyOnceAndRendersItsOwnFragment()
    {
        ClassBuilder? nested = null;
        var calls = 0;
        var builder = CSharp.Class("Outer").Class("Inner", c =>
        {
            nested = c;
            calls++;
            c.Private().Class("Deep");
        });
        const string expected = "class Outer\n{\n    private class Inner\n    {\n        class Deep\n        {\n        }\n    }\n}\n";

        Assert.NotNull(nested);
        Assert.Equal(1, calls);
        OutputAssert.Equal("private class Inner\n{\n    class Deep\n    {\n    }\n}\n", nested.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Fact]
    public void NestedClassesAndRaw_PreserveOrderWithOneBlankLine()
    {
        var builder = CSharp.Class("Outer", c => c.Partial().Static().Public()
            .Class("Inner", i => i.Private())
            .Raw("// raw")
            .Class("Last")
            .Raw("// end"));

        OutputAssert.Equal(
            "public static partial class Outer\n{\n    private class Inner\n    {\n    }\n\n    // raw\n\n    class Last\n    {\n    }\n\n    // end\n}\n",
            builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Fact]
    public void Raw_IndentsEachLineAndNormalizesCrLf()
    {
        var builder = CSharp.Class("Outer")
            .Class("Inner", c => c.Raw("// first\r\n\r\n// second\n// third"));

        OutputAssert.Equal(
            "class Outer\n{\n    class Inner\n    {\n        // first\n\n        // second\n        // third\n    }\n}\n",
            builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Fact]
    public void Add_ReflectsLaterChangesToNestedBuilder()
    {
        var nested = CSharp.Class("Inner");
        var builder = CSharp.Class("Outer").Add(nested);
        OutputAssert.Equal("class Outer\n{\n    class Inner\n    {\n    }\n}\n", builder.ToString());

        nested.Private().Partial().Raw("// changed");

        OutputAssert.Equal(
            "class Outer\n{\n    private partial class Inner\n    {\n        // changed\n    }\n}\n",
            builder.ToString());
        OutputAssert.Equal("private partial class Inner\n{\n    // changed\n}\n", nested.ToString());
    }

    [Fact]
    public void FluentMethods_ReturnSameClassBuilder()
    {
        var builder = CSharp.Class("Foo");

        Assert.Same(builder, builder.Public());
        Assert.Same(builder, builder.Internal());
        Assert.Same(builder, builder.Protected());
        Assert.Same(builder, builder.Private());
        Assert.Same(builder, builder.ProtectedInternal());
        Assert.Same(builder, builder.PrivateProtected());
        Assert.Same(builder, builder.Static());
        Assert.Same(builder, builder.Abstract());
        Assert.Same(builder, builder.Sealed());
        Assert.Same(builder, builder.ReadOnly());
        Assert.Same(builder, builder.Partial());
        Assert.Same(builder, builder.Class("First"));
        Assert.Same(builder, builder.Class("Second", _ => { }));
        Assert.Same(builder, builder.Add(CSharp.Class("Third")));
        Assert.Same(builder, builder.Raw("// raw"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("1Foo")]
    [InlineData("Foo.Bar")]
    [InlineData("Foo Bar")]
    [InlineData("Foo-Bar")]
    [InlineData("@@Foo")]
    public void NestedClass_RejectsInvalidNameBeforeConfiguring(string name)
    {
        var builder = CSharp.Class("Outer");
        var configured = false;

        Assert.Throws<ArgumentException>("name", () => builder.Class(name));
        Assert.Throws<ArgumentException>("name", () => builder.Class(name, _ => configured = true));
        Assert.False(configured);
        OutputAssert.Equal("class Outer\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void NestedClass_EscapesKeywordName()
    {
        var builder = CSharp.Class("Outer").Class("class");

        OutputAssert.Equal("class Outer\n{\n    class @class\n    {\n    }\n}\n", builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Fact]
    public void BodyItems_RejectNullArguments()
    {
        var builder = CSharp.Class("Outer");
        var configured = false;

        Assert.Throws<ArgumentNullException>("name", () => builder.Class(null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Class(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Class("Inner", null!));
        Assert.Throws<ArgumentNullException>("classBuilder", () => builder.Add((ClassBuilder)null!));
        Assert.Throws<ArgumentNullException>("code", () => builder.Raw(null!));
        Assert.False(configured);
        OutputAssert.Equal("class Outer\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void NestedAccessibilities_CompileInPublicPartialClass()
    {
        var builder = CSharp.Class("Outer").Public().Partial()
            .Class("PublicNested", c => c.Public())
            .Class("InternalNested", c => c.Internal())
            .Class("ProtectedNested", c => c.Protected())
            .Class("PrivateNested", c => c.Private())
            .Class("ProtectedInternalNested", c => c.ProtectedInternal())
            .Class("PrivateProtectedNested", c => c.PrivateProtected())
            .Raw("// raw");

        OutputAssert.Equal(
            "public partial class Outer\n{\n    public class PublicNested\n    {\n    }\n\n    internal class InternalNested\n    {\n    }\n\n    protected class ProtectedNested\n    {\n    }\n\n    private class PrivateNested\n    {\n    }\n\n    protected internal class ProtectedInternalNested\n    {\n    }\n\n    private protected class PrivateProtectedNested\n    {\n    }\n\n    // raw\n}\n",
            builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Fact]
    public void FileWithValidModifierCombinations_Compiles()
    {
        var builder = CSharp.File()
            .Class("StaticClass", c => c.Partial().Static().Public())
            .Class("SealedClass", c => c.Sealed().Internal())
            .Class("AbstractClass", c => c.Partial().Abstract().Public());

        OutputAssert.Equal(
            "public static partial class StaticClass\n{\n}\n\ninternal sealed class SealedClass\n{\n}\n\npublic abstract partial class AbstractClass\n{\n}\n",
            builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
    }
}
