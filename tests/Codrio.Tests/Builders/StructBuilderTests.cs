using System;
using Xunit;

namespace Codrio.Tests;

public class StructBuilderTests
{
    [Theory]
    [InlineData("Point", "struct Point\n{\n}\n")]
    [InlineData("struct", "struct @struct\n{\n}\n")]
    [InlineData("@struct", "struct @struct\n{\n}\n")]
    public void ToString_RendersEmptyStruct(string name, string expected)
    {
        var builder = CSharp.Struct(name);

        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Fact]
    public void Struct_InvokesConfigureImmediatelyOnceOnReturnedBuilder()
    {
        StructBuilder? configured = null;
        var calls = 0;
        var builder = CSharp.Struct("Point", s =>
        {
            configured = s;
            calls++;
            s.Partial().ReadOnly().Public();
        });

        Assert.Same(builder, configured);
        Assert.Equal(1, calls);
        OutputAssert.Equal("public readonly partial struct Point\n{\n}\n", builder.ToString());
        builder.ReadOnly().Partial();
        OutputAssert.Equal("public readonly partial struct Point\n{\n}\n", builder.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Accessibility_LastCallWins()
    {
        var builder = CSharp.Struct("Point").Public().Internal();

        OutputAssert.Equal("internal struct Point\n{\n}\n", builder.ToString());
        OutputAssert.Equal("protected struct Point\n{\n}\n", builder.Protected().ToString());
        OutputAssert.Equal("private struct Point\n{\n}\n", builder.Private().ToString());
        OutputAssert.Equal("protected internal struct Point\n{\n}\n", builder.ProtectedInternal().ToString());
        OutputAssert.Equal("private protected struct Point\n{\n}\n", builder.PrivateProtected().ToString());
        OutputAssert.Equal("public struct Point\n{\n}\n", builder.Public().ToString());
    }

    [Fact]
    public void GenericStruct_RendersBaseInterfacesAndConstraintsAndCompiles()
    {
        var file = CSharp.File().Raw("interface IMarker { }").Raw("interface IOther { }")
            .Struct("Point", s => s.TypeParameter("T", t => t.Constraint("class").Constraint("new()"))
                .TypeParameter("U").BaseType("IMarker").BaseType("IOther").Partial().ReadOnly());

        OutputAssert.Equal(
            "interface IMarker { }\n\ninterface IOther { }\n\nreadonly partial struct Point<T, U> : IMarker, IOther where T : class, new()\n{\n}\n",
            file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }

    [Fact]
    public void FileStructMethods_KeepInstancesAndPreserveMixedItemOrder()
    {
        StructBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Struct("Added");
        var file = CSharp.File().Class("Before").Struct("Empty").Raw("// middle")
            .Struct("Configured", s =>
            {
                configured = s;
                calls++;
            }).Add(added);

        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, file.items[3].Target);
        Assert.Same(added, file.items[4].Target);
        configured.Public();
        added.ReadOnly();
        const string expected = "class Before\n{\n}\n\nstruct Empty\n{\n}\n\n// middle\n\npublic struct Configured\n{\n}\n\nreadonly struct Added\n{\n}\n";
        OutputAssert.Equal(expected, file.ToString());
        OutputAssert.Equal(expected, file.ToString());
        Assert.Equal(1, calls);
        CompilationAssert.NoErrors(file.ToString());
    }

    [Theory]
    [InlineData(false, "namespace Example\n{\n    class Before\n    {\n    }\n\n    struct Empty\n    {\n    }\n\n    public struct Configured\n    {\n    }\n\n    readonly struct Added\n    {\n    }\n}\n")]
    [InlineData(true, "namespace Example;\n\nclass Before\n{\n}\n\nstruct Empty\n{\n}\n\npublic struct Configured\n{\n}\n\nreadonly struct Added\n{\n}\n")]
    public void NamespaceStructMethods_KeepInstancesAndPreserveOrder(bool fileScoped, string expected)
    {
        StructBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Struct("Added");
        var builder = fileScoped ? CSharp.FileScopedNamespace("Example") : CSharp.Namespace("Example");
        builder.Class("Before").Struct("Empty").Struct("Configured", s =>
        {
            configured = s;
            calls++;
        }).Add(added);

        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, builder.items[2].Target);
        Assert.Same(added, builder.items[3].Target);
        configured.Public();
        added.ReadOnly();
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
        CompilationAssert.NoErrors(CSharp.File().Add(builder).ToString());
    }

    [Fact]
    public void NestedStructsInClass_ConfigureImmediatelyAndKeepAddedInstance()
    {
        StructBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Struct("Added");
        var builder = CSharp.Class("Outer").Struct("Empty").Struct("Configured", s =>
        {
            configured = s;
            calls++;
        }).Add(added);

        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, builder.items[1].Target);
        Assert.Same(added, builder.items[2].Target);
        configured.Private();
        added.Public().Raw("// changed");
        const string expected = "class Outer\n{\n    struct Empty\n    {\n    }\n\n    private struct Configured\n    {\n    }\n\n    public struct Added\n    {\n        // changed\n    }\n}\n";
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
        CompilationAssert.NoErrors(CSharp.File().Add(builder).ToString());
    }

    [Fact]
    public void StructBody_PreservesNestedClassStructAndRawOrder()
    {
        StructBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Struct("AddedStruct");
        var builder = CSharp.Struct("Outer").ReadOnly()
            .Class("First").Struct("struct").Raw("// first\r\n// second")
            .Class("ConfiguredClass", c => c.Private())
            .Struct("ConfiguredStruct", s =>
            {
                configured = s;
                calls++;
            }).Add(CSharp.Class("AddedClass")).Add(added);

        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, builder.items[4].Target);
        Assert.Same(added, builder.items[6].Target);
        configured.Public();
        added.ReadOnly();
        const string expected = "readonly struct Outer\n{\n    class First\n    {\n    }\n\n    struct @struct\n    {\n    }\n\n    // first\n    // second\n\n    private class ConfiguredClass\n    {\n    }\n\n    public struct ConfiguredStruct\n    {\n    }\n\n    class AddedClass\n    {\n    }\n\n    readonly struct AddedStruct\n    {\n    }\n}\n";
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
        CompilationAssert.NoErrors(CSharp.File().Add(builder).ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("1Point")]
    [InlineData("Point.Value")]
    [InlineData("Point Value")]
    [InlineData("Point-Value")]
    [InlineData("@@Point")]
    public void InvalidNames_ThrowAtCallTimeBeforeConfiguringOrAdding(string name)
    {
        var file = CSharp.File();
        var ns = CSharp.Namespace("Example");
        var outerClass = CSharp.Class("Outer");
        var outerStruct = CSharp.Struct("Outer");
        var configured = false;

        Assert.Throws<ArgumentException>("name", () => CSharp.Struct(name));
        Assert.Throws<ArgumentException>("name", () => CSharp.Struct(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => file.Struct(name));
        Assert.Throws<ArgumentException>("name", () => file.Struct(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => ns.Struct(name));
        Assert.Throws<ArgumentException>("name", () => ns.Struct(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => outerClass.Struct(name));
        Assert.Throws<ArgumentException>("name", () => outerClass.Struct(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => outerStruct.Struct(name));
        Assert.Throws<ArgumentException>("name", () => outerStruct.Struct(name, _ => configured = true));
        Assert.False(configured);
        OutputAssert.Equal("", file.ToString());
        OutputAssert.Equal("namespace Example\n{\n}\n", ns.ToString());
        OutputAssert.Equal("class Outer\n{\n}\n", outerClass.ToString());
        OutputAssert.Equal("struct Outer\n{\n}\n", outerStruct.ToString());
    }

    [Fact]
    public void NullArguments_ThrowWithParameterNameBeforeConfiguringOrAdding()
    {
        var file = CSharp.File();
        var ns = CSharp.Namespace("Example");
        var outerClass = CSharp.Class("Outer");
        var outerStruct = CSharp.Struct("Outer");
        var configured = false;

        Assert.Throws<ArgumentNullException>("name", () => CSharp.Struct(null!));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Struct(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Struct("Point", null!));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Struct(null!, null!));
        Assert.Throws<ArgumentNullException>("name", () => file.Struct(null!));
        Assert.Throws<ArgumentNullException>("name", () => file.Struct(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => file.Struct("Point", null!));
        Assert.Throws<ArgumentNullException>("configure", () => file.Struct(null!, null!));
        Assert.Throws<ArgumentNullException>("structBuilder", () => file.Add((StructBuilder)null!));
        Assert.Throws<ArgumentNullException>("name", () => ns.Struct(null!));
        Assert.Throws<ArgumentNullException>("name", () => ns.Struct(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => ns.Struct("Point", null!));
        Assert.Throws<ArgumentNullException>("configure", () => ns.Struct(null!, null!));
        Assert.Throws<ArgumentNullException>("structBuilder", () => ns.Add((StructBuilder)null!));
        Assert.Throws<ArgumentNullException>("name", () => outerClass.Struct(null!));
        Assert.Throws<ArgumentNullException>("name", () => outerClass.Struct(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => outerClass.Struct("Point", null!));
        Assert.Throws<ArgumentNullException>("configure", () => outerClass.Struct(null!, null!));
        Assert.Throws<ArgumentNullException>("structBuilder", () => outerClass.Add((StructBuilder)null!));
        Assert.Throws<ArgumentNullException>("name", () => outerStruct.Struct(null!));
        Assert.Throws<ArgumentNullException>("name", () => outerStruct.Struct(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => outerStruct.Struct("Point", null!));
        Assert.Throws<ArgumentNullException>("configure", () => outerStruct.Struct(null!, null!));
        Assert.Throws<ArgumentNullException>("structBuilder", () => outerStruct.Add((StructBuilder)null!));
        Assert.False(configured);
        OutputAssert.Equal("", file.ToString());
        OutputAssert.Equal("namespace Example\n{\n}\n", ns.ToString());
        OutputAssert.Equal("class Outer\n{\n}\n", outerClass.ToString());
        OutputAssert.Equal("struct Outer\n{\n}\n", outerStruct.ToString());
    }

    [Fact]
    public void FluentMethods_ReturnSameStructBuilder()
    {
        var builder = CSharp.Struct("Point");

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
        Assert.Same(builder, builder.BaseType("IMarker"));
        Assert.Same(builder, builder.TypeParameter("T"));
        Assert.Same(builder, builder.TypeParameter("U", t => t.Constraint("class")));
        Assert.Same(builder, builder.Class("FirstClass"));
        Assert.Same(builder, builder.Class("SecondClass", _ => { }));
        Assert.Same(builder, builder.Add(CSharp.Class("ThirdClass")));
        Assert.Same(builder, builder.Struct("FirstStruct"));
        Assert.Same(builder, builder.Struct("SecondStruct", _ => { }));
        Assert.Same(builder, builder.Add(CSharp.Struct("ThirdStruct")));
        Assert.Same(builder, builder.Raw("// raw"));
    }

    [Fact]
    public void StructEntryPoints_ReturnSameParentBuilder()
    {
        var file = CSharp.File();
        var ns = CSharp.Namespace("Example");
        var fileScoped = CSharp.FileScopedNamespace("Example");
        var outer = CSharp.Class("Outer");

        Assert.Same(file, file.Struct("First"));
        Assert.Same(file, file.Struct("Second", _ => { }));
        Assert.Same(file, file.Add(CSharp.Struct("Third")));
        Assert.Same(ns, ns.Struct("First"));
        Assert.Same(ns, ns.Struct("Second", _ => { }));
        Assert.Same(ns, ns.Add(CSharp.Struct("Third")));
        Assert.Same(fileScoped, fileScoped.Struct("First"));
        Assert.Same(fileScoped, fileScoped.Struct("Second", _ => { }));
        Assert.Same(fileScoped, fileScoped.Add(CSharp.Struct("Third")));
        Assert.Same(outer, outer.Struct("First"));
        Assert.Same(outer, outer.Struct("Second", _ => { }));
        Assert.Same(outer, outer.Add(CSharp.Struct("Third")));
    }

    [Fact]
    public void CompleteFile_RendersHeadersNamespacesAndStructsAndCompiles()
    {
        var file = CSharp.File().AutoGeneratedHeader().NullableEnable().Using("System")
            .Namespace("Example", n => n.Struct("Point", s => s.Partial().ReadOnly().Public().Raw("// raw"))
                .Class("Outer", c => c.Struct("Nested", s => s.Public())))
            .Struct("TopLevel");
        const string expected = "// <auto-generated/>\n#nullable enable\n\nusing System;\n\nnamespace Example\n{\n    public readonly partial struct Point\n    {\n        // raw\n    }\n\n    class Outer\n    {\n        public struct Nested\n        {\n        }\n    }\n}\n\nstruct TopLevel\n{\n}\n";

        OutputAssert.Equal(expected, file.ToString());
        OutputAssert.Equal(expected, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }
}
