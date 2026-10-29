using System;
using Xunit;

namespace Codrio.Tests;

public class InterfaceBuilderTests
{
    [Theory]
    [InlineData("IRepository", "interface IRepository\n{\n}\n")]
    [InlineData("interface", "interface @interface\n{\n}\n")]
    [InlineData("@interface", "interface @interface\n{\n}\n")]
    public void ToString_RendersEmptyInterface(string name, string expected)
    {
        var builder = CSharp.Interface(name);

        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Fact]
    public void Interface_InvokesConfigureImmediatelyOnceOnReturnedBuilder()
    {
        InterfaceBuilder? configured = null;
        var calls = 0;
        var builder = CSharp.Interface("IRepository", s =>
        {
            configured = s;
            calls++;
            s.Partial().Public();
        });

        Assert.Same(builder, configured);
        Assert.Equal(1, calls);
        OutputAssert.Equal("public partial interface IRepository\n{\n}\n", builder.ToString());
        builder.Partial();
        OutputAssert.Equal("public partial interface IRepository\n{\n}\n", builder.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Modifiers_RenderInFixedOrderWithoutValidatingCombinations()
    {
        var builder = CSharp.Interface("IRepository").Partial().ReadOnly().Sealed().Abstract().Static().Public();

        OutputAssert.Equal("public static abstract sealed readonly partial interface IRepository\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void Accessibility_LastCallWins()
    {
        var builder = CSharp.Interface("IRepository").Public().Internal();

        OutputAssert.Equal("internal interface IRepository\n{\n}\n", builder.ToString());
        OutputAssert.Equal("protected interface IRepository\n{\n}\n", builder.Protected().ToString());
        OutputAssert.Equal("private interface IRepository\n{\n}\n", builder.Private().ToString());
        OutputAssert.Equal("protected internal interface IRepository\n{\n}\n", builder.ProtectedInternal().ToString());
        OutputAssert.Equal("private protected interface IRepository\n{\n}\n", builder.PrivateProtected().ToString());
        OutputAssert.Equal("public interface IRepository\n{\n}\n", builder.Public().ToString());
    }

    [Fact]
    public void GenericInterface_RendersBaseInterfacesAndConstraintsAndCompiles()
    {
        var file = CSharp.File().Interface("IMarker").Interface("IOther")
            .Interface("IRepository", s => s.TypeParameter("T", t => t.Constraint("class").Constraint("new()"))
                .TypeParameter("U").BaseType("IMarker").BaseType("IOther").Partial());

        OutputAssert.Equal(
            "interface IMarker\n{\n}\n\ninterface IOther\n{\n}\n\npartial interface IRepository<T, U> : IMarker, IOther where T : class, new()\n{\n}\n",
            file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }

    [Fact]
    public void FileInterfaceMethods_KeepInstancesAndPreserveMixedItemOrder()
    {
        InterfaceBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Interface("Added");
        var file = CSharp.File().Class("Before").Interface("Empty").Raw("// middle")
            .Interface("Configured", s =>
            {
                configured = s;
                calls++;
            }).Add(added);

        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, file.items[3].Target);
        Assert.Same(added, file.items[4].Target);
        configured.Public();
        added.Partial();
        const string expected = "class Before\n{\n}\n\ninterface Empty\n{\n}\n\n// middle\n\npublic interface Configured\n{\n}\n\npartial interface Added\n{\n}\n";
        OutputAssert.Equal(expected, file.ToString());
        OutputAssert.Equal(expected, file.ToString());
        Assert.Equal(1, calls);
        CompilationAssert.NoErrors(file.ToString());
    }

    [Theory]
    [InlineData(false, "namespace Example\n{\n    class Before\n    {\n    }\n\n    interface Empty\n    {\n    }\n\n    public interface Configured\n    {\n    }\n\n    partial interface Added\n    {\n    }\n}\n")]
    [InlineData(true, "namespace Example;\n\nclass Before\n{\n}\n\ninterface Empty\n{\n}\n\npublic interface Configured\n{\n}\n\npartial interface Added\n{\n}\n")]
    public void NamespaceInterfaceMethods_KeepInstancesAndPreserveOrder(bool fileScoped, string expected)
    {
        InterfaceBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Interface("Added");
        var builder = fileScoped ? CSharp.FileScopedNamespace("Example") : CSharp.Namespace("Example");
        builder.Class("Before").Interface("Empty").Interface("Configured", s =>
        {
            configured = s;
            calls++;
        }).Add(added);

        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, builder.items[2].Target);
        Assert.Same(added, builder.items[3].Target);
        configured.Public();
        added.Partial();
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
        CompilationAssert.NoErrors(CSharp.File().Add(builder).ToString());
    }

    [Fact]
    public void NestedInterfacesInClass_ConfigureImmediatelyAndKeepAddedInstance()
    {
        InterfaceBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Interface("Added");
        var builder = CSharp.Class("Outer").Interface("Empty").Interface("Configured", s =>
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
        const string expected = "class Outer\n{\n    interface Empty\n    {\n    }\n\n    private interface Configured\n    {\n    }\n\n    public interface Added\n    {\n        // changed\n    }\n}\n";
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
        CompilationAssert.NoErrors(CSharp.File().Add(builder).ToString());
    }

    [Fact]
    public void NestedInterfacesInStruct_ConfigureImmediatelyAndKeepAddedInstance()
    {
        InterfaceBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Interface("Added");
        var builder = CSharp.Struct("Outer").Interface("Empty").Interface("Configured", s =>
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
        const string expected = "struct Outer\n{\n    interface Empty\n    {\n    }\n\n    private interface Configured\n    {\n    }\n\n    public interface Added\n    {\n        // changed\n    }\n}\n";
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
        CompilationAssert.NoErrors(CSharp.File().Add(builder).ToString());
    }

    [Fact]
    public void InterfaceBody_PreservesNestedClassStructInterfaceAndRawOrder()
    {
        InterfaceBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Interface("AddedInterface");
        var builder = CSharp.Interface("Outer").Partial()
            .Class("First").Interface("interface").Raw("// first\r\n// second")
            .Class("ConfiguredClass", c => c.Private())
            .Interface("ConfiguredInterface", s =>
            {
                configured = s;
                calls++;
            }).Add(CSharp.Class("AddedClass")).Add(added)
            .Struct("EmptyStruct").Struct("ConfiguredStruct", s => s.ReadOnly())
            .Add(CSharp.Struct("AddedStruct"));

        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, builder.items[4].Target);
        Assert.Same(added, builder.items[6].Target);
        configured.Public();
        added.Partial();
        const string expected = "partial interface Outer\n{\n    class First\n    {\n    }\n\n    interface @interface\n    {\n    }\n\n    // first\n    // second\n\n    private class ConfiguredClass\n    {\n    }\n\n    public interface ConfiguredInterface\n    {\n    }\n\n    class AddedClass\n    {\n    }\n\n    partial interface AddedInterface\n    {\n    }\n\n    struct EmptyStruct\n    {\n    }\n\n    readonly struct ConfiguredStruct\n    {\n    }\n\n    struct AddedStruct\n    {\n    }\n}\n";
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
        CompilationAssert.NoErrors(CSharp.File().Add(builder).ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("1IRepository")]
    [InlineData("IRepository.Value")]
    [InlineData("IRepository Value")]
    [InlineData("IRepository-Value")]
    [InlineData("@@IRepository")]
    public void InvalidNames_ThrowAtCallTimeBeforeConfiguringOrAdding(string name)
    {
        var file = CSharp.File();
        var ns = CSharp.Namespace("Example");
        var outerClass = CSharp.Class("Outer");
        var outerStruct = CSharp.Struct("Outer");
        var outerInterface = CSharp.Interface("Outer");
        var configured = false;

        Assert.Throws<ArgumentException>("name", () => CSharp.Interface(name));
        Assert.Throws<ArgumentException>("name", () => CSharp.Interface(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => file.Interface(name));
        Assert.Throws<ArgumentException>("name", () => file.Interface(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => ns.Interface(name));
        Assert.Throws<ArgumentException>("name", () => ns.Interface(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => outerClass.Interface(name));
        Assert.Throws<ArgumentException>("name", () => outerStruct.Interface(name));
        Assert.Throws<ArgumentException>("name", () => outerClass.Interface(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => outerStruct.Interface(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => outerInterface.Interface(name));
        Assert.Throws<ArgumentException>("name", () => outerInterface.Interface(name, _ => configured = true));
        Assert.False(configured);
        OutputAssert.Equal("", file.ToString());
        OutputAssert.Equal("namespace Example\n{\n}\n", ns.ToString());
        OutputAssert.Equal("class Outer\n{\n}\n", outerClass.ToString());
        OutputAssert.Equal("struct Outer\n{\n}\n", outerStruct.ToString());
        OutputAssert.Equal("interface Outer\n{\n}\n", outerInterface.ToString());
    }

    [Fact]
    public void NullArguments_ThrowWithParameterNameBeforeConfiguringOrAdding()
    {
        var file = CSharp.File();
        var ns = CSharp.Namespace("Example");
        var outerClass = CSharp.Class("Outer");
        var outerStruct = CSharp.Struct("Outer");
        var outerInterface = CSharp.Interface("Outer");
        var configured = false;

        Assert.Throws<ArgumentNullException>("name", () => CSharp.Interface(null!));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Interface(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Interface("IRepository", null!));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Interface(null!, null!));
        Assert.Throws<ArgumentNullException>("name", () => file.Interface(null!));
        Assert.Throws<ArgumentNullException>("name", () => file.Interface(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => file.Interface("IRepository", null!));
        Assert.Throws<ArgumentNullException>("configure", () => file.Interface(null!, null!));
        Assert.Throws<ArgumentNullException>("interfaceBuilder", () => file.Add((InterfaceBuilder)null!));
        Assert.Throws<ArgumentNullException>("name", () => ns.Interface(null!));
        Assert.Throws<ArgumentNullException>("name", () => ns.Interface(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => ns.Interface("IRepository", null!));
        Assert.Throws<ArgumentNullException>("configure", () => ns.Interface(null!, null!));
        Assert.Throws<ArgumentNullException>("interfaceBuilder", () => ns.Add((InterfaceBuilder)null!));
        Assert.Throws<ArgumentNullException>("name", () => outerClass.Interface(null!));
        Assert.Throws<ArgumentNullException>("name", () => outerStruct.Interface(null!));
        Assert.Throws<ArgumentNullException>("name", () => outerClass.Interface(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("name", () => outerStruct.Interface(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => outerClass.Interface("IRepository", null!));
        Assert.Throws<ArgumentNullException>("configure", () => outerStruct.Interface("IRepository", null!));
        Assert.Throws<ArgumentNullException>("configure", () => outerClass.Interface(null!, null!));
        Assert.Throws<ArgumentNullException>("configure", () => outerStruct.Interface(null!, null!));
        Assert.Throws<ArgumentNullException>("interfaceBuilder", () => outerClass.Add((InterfaceBuilder)null!));
        Assert.Throws<ArgumentNullException>("interfaceBuilder", () => outerStruct.Add((InterfaceBuilder)null!));
        Assert.Throws<ArgumentNullException>("name", () => outerInterface.Interface(null!));
        Assert.Throws<ArgumentNullException>("name", () => outerInterface.Interface(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => outerInterface.Interface("IRepository", null!));
        Assert.Throws<ArgumentNullException>("configure", () => outerInterface.Interface(null!, null!));
        Assert.Throws<ArgumentNullException>("interfaceBuilder", () => outerInterface.Add((InterfaceBuilder)null!));
        Assert.False(configured);
        OutputAssert.Equal("", file.ToString());
        OutputAssert.Equal("namespace Example\n{\n}\n", ns.ToString());
        OutputAssert.Equal("class Outer\n{\n}\n", outerClass.ToString());
        OutputAssert.Equal("struct Outer\n{\n}\n", outerStruct.ToString());
        OutputAssert.Equal("interface Outer\n{\n}\n", outerInterface.ToString());
    }

    [Fact]
    public void FluentMethods_ReturnSameInterfaceBuilder()
    {
        var builder = CSharp.Interface("IRepository");

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
        Assert.Same(builder, builder.Interface("FirstInterface"));
        Assert.Same(builder, builder.Interface("SecondInterface", _ => { }));
        Assert.Same(builder, builder.Add(CSharp.Interface("ThirdInterface")));
        Assert.Same(builder, builder.Raw("// raw"));
    }

    [Fact]
    public void InterfaceEntryPoints_ReturnSameParentBuilder()
    {
        var file = CSharp.File();
        var ns = CSharp.Namespace("Example");
        var fileScoped = CSharp.FileScopedNamespace("Example");
        var outer = CSharp.Class("Outer");
        var outerStruct = CSharp.Struct("Outer");

        Assert.Same(file, file.Interface("First"));
        Assert.Same(file, file.Interface("Second", _ => { }));
        Assert.Same(file, file.Add(CSharp.Interface("Third")));
        Assert.Same(ns, ns.Interface("First"));
        Assert.Same(ns, ns.Interface("Second", _ => { }));
        Assert.Same(ns, ns.Add(CSharp.Interface("Third")));
        Assert.Same(fileScoped, fileScoped.Interface("First"));
        Assert.Same(fileScoped, fileScoped.Interface("Second", _ => { }));
        Assert.Same(fileScoped, fileScoped.Add(CSharp.Interface("Third")));
        Assert.Same(outer, outer.Interface("First"));
        Assert.Same(outer, outer.Interface("Second", _ => { }));
        Assert.Same(outer, outer.Add(CSharp.Interface("Third")));
        Assert.Same(outerStruct, outerStruct.Interface("First"));
        Assert.Same(outerStruct, outerStruct.Interface("Second", _ => { }));
        Assert.Same(outerStruct, outerStruct.Add(CSharp.Interface("Third")));
    }

    [Fact]
    public void CompleteFile_RendersHeadersNamespacesAndInterfacesAndCompiles()
    {
        var file = CSharp.File().AutoGeneratedHeader().NullableEnable().Using("System")
            .Namespace("Example", n => n.Interface("IRepository", i => i.BaseType("System.IDisposable")
                    .TypeParameter("T", t => t.Constraint("class")).Partial().Public())
                .Class("Outer", c => c.Interface("Nested", s => s.Public())))
            .Interface("TopLevel");
        const string expected = "// <auto-generated/>\n#nullable enable\n\nusing System;\n\nnamespace Example\n{\n    public partial interface IRepository<T> : System.IDisposable where T : class\n    {\n    }\n\n    class Outer\n    {\n        public interface Nested\n        {\n        }\n    }\n}\n\ninterface TopLevel\n{\n}\n";

        OutputAssert.Equal(expected, file.ToString());
        OutputAssert.Equal(expected, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }
}
