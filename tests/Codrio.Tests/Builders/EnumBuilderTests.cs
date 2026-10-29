using System;
using System.Linq;
using Xunit;

namespace Codrio.Tests;

public class EnumBuilderTests
{
    [Theory]
    [InlineData("Empty", "enum Empty\n{\n}\n")]
    [InlineData("class", "enum @class\n{\n}\n")]
    [InlineData("@class", "enum @class\n{\n}\n")]
    public void ToString_RendersEmptyEnum(string name, string expected)
    {
        var builder = CSharp.Enum(name);

        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Fact]
    public void Members_RenderInOrderWithCommasOnlyBetweenMembers()
    {
        var builder = CSharp.Enum("Color").Member("Red");

        OutputAssert.Equal("enum Color\n{\n    Red\n}\n", builder.ToString());
        builder.Member("Green", "2").Member("Blue");
        OutputAssert.Equal("enum Color\n{\n    Red,\n    Green = 2,\n    Blue\n}\n", builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
        OutputAssert.Equal("enum Color\n{\n    Red = 1\n}\n", CSharp.Enum("Color").Member("Red", "1").ToString());
    }

    [Fact]
    public void ValuesAndUnderlyingType_AreWrittenVerbatimAndLastTypeWins()
    {
        var builder = CSharp.Enum("Color").UnderlyingType("long").UnderlyingType("byte")
            .Member("Red", "1 << 2").Member("Green", "8").Member("Blue", "Red | Green")
            .Member("class", "16").Member("event");

        OutputAssert.Equal("enum Color : byte\n{\n    Red = 1 << 2,\n    Green = 8,\n    Blue = Red | Green,\n    @class = 16,\n    @event\n}\n", builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
        builder.UnderlyingType("global::System.Int32");
        OutputAssert.Equal("enum Color : global::System.Int32\n{\n    Red = 1 << 2,\n    Green = 8,\n    Blue = Red | Green,\n    @class = 16,\n    @event\n}\n", builder.ToString());
    }

    [Fact]
    public void UnvalidatedTextAndDuplicateMembers_DoNotThrowDuringRendering()
    {
        var builder = CSharp.Enum("E").UnderlyingType("").Member("A", "").Member("A", "not an expression");

        OutputAssert.Equal("enum E : \n{\n    A = ,\n    A = not an expression\n}\n", builder.ToString());
    }

    [Fact]
    public void Factory_InvokesConfigureImmediatelyOnceOnReturnedBuilder()
    {
        EnumBuilder? configured = null;
        var calls = 0;
        var builder = CSharp.Enum("Color", e =>
        {
            configured = e;
            calls++;
            e.Public().UnderlyingType("byte").Member("Red").Member("Green", "2").Member("Blue");
        });

        Assert.Same(builder, configured);
        Assert.Equal(1, calls);
        const string expected = "public enum Color : byte\n{\n    Red,\n    Green = 2,\n    Blue\n}\n";
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Fact]
    public void Accessibility_ReplacesPreviousValueAndReturnsSameBuilder()
    {
        var builder = CSharp.Enum("E");

        Assert.Same(builder, builder.Public());
        OutputAssert.Equal("public enum E\n{\n}\n", builder.ToString());
        Assert.Same(builder, builder.Internal());
        OutputAssert.Equal("internal enum E\n{\n}\n", builder.ToString());
        Assert.Same(builder, builder.Protected());
        OutputAssert.Equal("protected enum E\n{\n}\n", builder.ToString());
        Assert.Same(builder, builder.Private());
        OutputAssert.Equal("private enum E\n{\n}\n", builder.ToString());
        Assert.Same(builder, builder.ProtectedInternal());
        OutputAssert.Equal("protected internal enum E\n{\n}\n", builder.ToString());
        Assert.Same(builder, builder.PrivateProtected());
        OutputAssert.Equal("private protected enum E\n{\n}\n", builder.ToString());
        Assert.Same(builder, builder.UnderlyingType("int"));
        Assert.Same(builder, builder.Member("A"));
        Assert.Same(builder, builder.Member("B", "2"));
        CompilationAssert.NoErrors(CSharp.File().Class("Outer", c => c.Add(builder)).ToString());
    }

    [Fact]
    public void FileEntryPoints_PreserveOrderReferencesAndParent()
    {
        EnumBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Enum("Added");
        var file = CSharp.File();

        Assert.Same(file, file.Enum("Empty"));
        Assert.Same(file, file.Enum("Configured", e => { configured = e; calls++; }));
        Assert.Same(file, file.Add(added));
        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, file.items[1].Target);
        Assert.Same(added, file.items[2].Target);
        configured.Public();
        added.Member("Changed");
        const string expected = "enum Empty\n{\n}\n\npublic enum Configured\n{\n}\n\nenum Added\n{\n    Changed\n}\n";
        OutputAssert.Equal(expected, file.ToString());
        OutputAssert.Equal(expected, file.ToString());
        Assert.Equal(1, calls);
        CompilationAssert.NoErrors(file.ToString());
    }

    [Theory]
    [InlineData(false, "namespace Example\n{\n    enum Empty\n    {\n    }\n\n    public enum Configured\n    {\n    }\n\n    enum Added\n    {\n        Changed\n    }\n}\n")]
    [InlineData(true, "namespace Example;\n\nenum Empty\n{\n}\n\npublic enum Configured\n{\n}\n\nenum Added\n{\n    Changed\n}\n")]
    public void NamespaceEntryPoints_PreserveOrderReferencesAndParent(bool fileScoped, string expected)
    {
        EnumBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Enum("Added");
        var ns = fileScoped ? CSharp.FileScopedNamespace("Example") : CSharp.Namespace("Example");

        Assert.Same(ns, ns.Enum("Empty"));
        Assert.Same(ns, ns.Enum("Configured", e => { configured = e; calls++; }));
        Assert.Same(ns, ns.Add(added));
        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, ns.items[1].Target);
        Assert.Same(added, ns.items[2].Target);
        configured.Public();
        added.Member("Changed");
        OutputAssert.Equal(expected, ns.ToString());
        OutputAssert.Equal(expected, ns.ToString());
        Assert.Equal(1, calls);
        CompilationAssert.NoErrors(CSharp.File().Add(ns).ToString());
    }

    [Fact]
    public void NestedEntryPoints_WorkInEveryDeclarationKind()
    {
        CheckNested(CSharp.Class("Outer"), "class");
        CheckNested(CSharp.Struct("Outer"), "struct");
        CheckNested(CSharp.Interface("Outer"), "interface");
        CheckNested(CSharp.Record("Outer"), "record");
        CheckNested(CSharp.RecordStruct("Outer"), "record struct");
    }

    private static void CheckNested<TSelf>(TypeDeclarationBuilder<TSelf> outer, string keyword)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        EnumBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Enum("Added");

        Assert.Same(outer, outer.Enum("Empty"));
        Assert.Same(outer, outer.Enum("Configured", e => { configured = e; calls++; }));
        Assert.Same(outer, outer.Add(added));
        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, outer.items[1].Target);
        Assert.Same(added, outer.items[2].Target);
        configured.Public();
        added.Member("Changed");
        var expected = keyword + " Outer\n{\n    enum Empty\n    {\n    }\n\n    public enum Configured\n    {\n    }\n\n    enum Added\n    {\n        Changed\n    }\n}\n";
        OutputAssert.Equal(expected, outer.ToString());
        OutputAssert.Equal(expected, outer.ToString());
        Assert.Equal(1, calls);
        CompilationAssert.NoErrors(outer.ToString());
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
        var file = CSharp.File();
        var ns = CSharp.Namespace("Example");
        var outer = CSharp.Class("Outer");
        var builder = CSharp.Enum("E");
        var configured = false;

        Assert.Throws<ArgumentException>("name", () => CSharp.Enum(name));
        Assert.Throws<ArgumentException>("name", () => CSharp.Enum(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => file.Enum(name));
        Assert.Throws<ArgumentException>("name", () => file.Enum(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => ns.Enum(name));
        Assert.Throws<ArgumentException>("name", () => ns.Enum(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => outer.Enum(name));
        Assert.Throws<ArgumentException>("name", () => outer.Enum(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => builder.Member(name));
        Assert.Throws<ArgumentException>("name", () => builder.Member(name, "1"));
        Assert.False(configured);
        OutputAssert.Equal("", file.ToString());
        OutputAssert.Equal("namespace Example\n{\n}\n", ns.ToString());
        OutputAssert.Equal("class Outer\n{\n}\n", outer.ToString());
        OutputAssert.Equal("enum E\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void NullArguments_ThrowWithParameterNameBeforeConfiguringOrAdding()
    {
        var file = CSharp.File();
        var ns = CSharp.Namespace("Example");
        var outer = CSharp.Class("Outer");
        var builder = CSharp.Enum("E").UnderlyingType("byte");
        var configured = false;

        Assert.Throws<ArgumentNullException>("name", () => CSharp.Enum(null!));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Enum(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Enum("E", null!));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Enum(null!, null!));
        Assert.Throws<ArgumentNullException>("name", () => file.Enum(null!));
        Assert.Throws<ArgumentNullException>("name", () => file.Enum(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => file.Enum("E", null!));
        Assert.Throws<ArgumentNullException>("enumBuilder", () => file.Add((EnumBuilder)null!));
        Assert.Throws<ArgumentNullException>("name", () => ns.Enum(null!));
        Assert.Throws<ArgumentNullException>("name", () => ns.Enum(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => ns.Enum("E", null!));
        Assert.Throws<ArgumentNullException>("enumBuilder", () => ns.Add((EnumBuilder)null!));
        Assert.Throws<ArgumentNullException>("name", () => outer.Enum(null!));
        Assert.Throws<ArgumentNullException>("name", () => outer.Enum(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => outer.Enum("E", null!));
        Assert.Throws<ArgumentNullException>("enumBuilder", () => outer.Add((EnumBuilder)null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Member(null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Member(null!, "1"));
        Assert.Throws<ArgumentNullException>("value", () => builder.Member("A", null!));
        Assert.Throws<ArgumentNullException>("type", () => builder.UnderlyingType(null!));
        Assert.False(configured);
        OutputAssert.Equal("", file.ToString());
        OutputAssert.Equal("namespace Example\n{\n}\n", ns.ToString());
        OutputAssert.Equal("class Outer\n{\n}\n", outer.ToString());
        OutputAssert.Equal("enum E : byte\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void PublicApi_DoesNotExposeUnsupportedDeclarationMethods()
    {
        var names = typeof(EnumBuilder).GetMethods().Select(method => method.Name).ToArray();
        var unsupported = new[]
        {
            "Static", "Abstract", "Sealed", "ReadOnly", "Partial", "BaseType", "TypeParameter",
            "PrimaryParameter", "Class", "Struct", "Interface", "Record", "RecordStruct", "Enum", "Raw", "Add"
        };

        foreach (var name in unsupported)
        {
            Assert.DoesNotContain(name, names);
        }
    }

    [Fact]
    public void CompleteFile_WithUnderlyingTypeExpressionsKeywordsAndNestedEnums_Compiles()
    {
        var file = CSharp.File().Namespace("Example", n => n
            .Enum("class", e => e.Public().UnderlyingType("byte").Member("Red", "1 << 2")
                .Member("Green", "8").Member("class", "Red | Green"))
            .Class("Container", c => c.Enum("Nested", e => e.Private().Member("Value")))
            .Struct("Value", s => s.Enum("Nested"))
            .Interface("IContainer", i => i.Enum("Nested"))
            .Record("Data", r => r.Enum("Nested"))
            .RecordStruct("Point", r => r.Add(CSharp.Enum("Nested"))));
        const string expected = "namespace Example\n{\n    public enum @class : byte\n    {\n        Red = 1 << 2,\n        Green = 8,\n        @class = Red | Green\n    }\n\n    class Container\n    {\n        private enum Nested\n        {\n            Value\n        }\n    }\n\n    struct Value\n    {\n        enum Nested\n        {\n        }\n    }\n\n    interface IContainer\n    {\n        enum Nested\n        {\n        }\n    }\n\n    record Data\n    {\n        enum Nested\n        {\n        }\n    }\n\n    record struct Point\n    {\n        enum Nested\n        {\n        }\n    }\n}\n";

        OutputAssert.Equal(expected, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }
}
