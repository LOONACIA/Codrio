using System;
using Xunit;

namespace Codrio.Tests;

public class RecordBuilderTests
{
    [Theory]
    [InlineData(false, "Empty", "record Empty;\n")]
    [InlineData(true, "Empty", "record struct Empty;\n")]
    [InlineData(false, "class", "record @class;\n")]
    [InlineData(true, "class", "record struct @class;\n")]
    [InlineData(false, "@class", "record @class;\n")]
    [InlineData(true, "@class", "record struct @class;\n")]
    public void ToString_RendersEmptyRecord(bool isStruct, string name, string expected)
    {
        var builder = isStruct ? CSharp.RecordStruct(name) : CSharp.Record(name);

        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Theory]
    [InlineData(false, "public sealed partial record Value;\n")]
    [InlineData(true, "public readonly partial record struct Value;\n")]
    public void Factory_InvokesConfigureImmediatelyOnceOnReturnedBuilder(bool isStruct, string expected)
    {
        RecordBuilder? configured = null;
        var calls = 0;
        Action<RecordBuilder> configure = r =>
        {
            configured = r;
            calls++;
            r.Partial().Public();
            if (isStruct)
            {
                r.ReadOnly();
            }
            else
            {
                r.Sealed();
            }
        };
        var builder = isStruct ? CSharp.RecordStruct("Value", configure) : CSharp.Record("Value", configure);

        Assert.Same(builder, configured);
        Assert.Equal(1, calls);
        OutputAssert.Equal(expected, builder.ToString());
        builder.Partial();
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
        CompilationAssert.NoErrors(builder.ToString());
    }

    [Fact]
    public void PrimaryParameters_RenderBeforeBaseTypesAndConstraints()
    {
        var person = CSharp.Record("Person").Sealed().Public().PrimaryParameter("string", "Name");
        var point = CSharp.RecordStruct("Point").Partial().ReadOnly().Public()
            .PrimaryParameter("int", "X").PrimaryParameter("int", "Y", p => p.Default("0"));
        var box = CSharp.Record("Box").TypeParameter("T", t => t.Constraint("class"))
            .PrimaryParameter("T", "Value");
        var circle = CSharp.Record("Circle").Sealed().Public().BaseType("Shape")
            .PrimaryParameter("double", "Radius");

        OutputAssert.Equal("public sealed record Person(string Name);\n", person.ToString());
        OutputAssert.Equal("public readonly partial record struct Point(int X, int Y = 0);\n", point.ToString());
        OutputAssert.Equal("record Box<T>(T Value) where T : class;\n", box.ToString());
        OutputAssert.Equal("public sealed record Circle(double Radius) : Shape;\n", circle.ToString());
        CompilationAssert.NoErrors(CSharp.File().Record("Shape", r => r.Public().Abstract())
            .Add(person).Add(point).Add(box).Add(circle).ToString());
    }

    [Theory]
    [InlineData(false, "record Container\n{\n    class Nested\n    {\n    }\n}\n", "record Container\n{\n    // first\n    // second\n}\n")]
    [InlineData(true, "record struct Container\n{\n    class Nested\n    {\n    }\n}\n", "record struct Container\n{\n    // first\n    // second\n}\n")]
    public void BodyItems_RenderBlocks(bool isStruct, string nestedExpected, string rawExpected)
    {
        var nested = isStruct ? CSharp.RecordStruct("Container") : CSharp.Record("Container");
        var raw = isStruct ? CSharp.RecordStruct("Container") : CSharp.Record("Container");

        nested.Class("Nested");
        raw.Raw("// first\r\n// second");

        OutputAssert.Equal(nestedExpected, nested.ToString());
        OutputAssert.Equal(rawExpected, raw.ToString());
        CompilationAssert.NoErrors(nested.ToString());
        CompilationAssert.NoErrors(raw.ToString());
    }

    [Theory]
    [InlineData(false, "record Value;\n", "record Value\n{\n    class Nested\n    {\n    }\n\n    // changed\n}\n")]
    [InlineData(true, "record struct Value;\n", "record struct Value\n{\n    class Nested\n    {\n    }\n\n    // changed\n}\n")]
    public void Add_KeepsReferenceAndSwitchesToBlockAfterBodyItemsAreAdded(bool isStruct, string empty, string populated)
    {
        var added = isStruct ? CSharp.RecordStruct("Value") : CSharp.Record("Value");
        var file = CSharp.File().Add(added);

        Assert.Same(added, file.items[0].Target);
        OutputAssert.Equal(empty, file.ToString());
        added.Class("Nested").Raw("// changed");
        OutputAssert.Equal(populated, file.ToString());
        OutputAssert.Equal(populated, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }

    [Fact]
    public void FileEntryPoints_ConfigureImmediatelyAndPreserveOrderAndReferences()
    {
        RecordBuilder? configured = null;
        RecordBuilder? configuredStruct = null;
        var calls = 0;
        var added = CSharp.Record("Added");
        var addedStruct = CSharp.RecordStruct("AddedStruct");
        var file = CSharp.File().Class("Before").Record("Empty").RecordStruct("EmptyStruct")
            .Record("Configured", r => { configured = r; calls++; })
            .RecordStruct("ConfiguredStruct", r => { configuredStruct = r; calls++; })
            .Add(added).Add(addedStruct);

        Assert.NotNull(configured);
        Assert.NotNull(configuredStruct);
        Assert.Equal(2, calls);
        Assert.Same(configured, file.items[3].Target);
        Assert.Same(configuredStruct, file.items[4].Target);
        Assert.Same(added, file.items[5].Target);
        Assert.Same(addedStruct, file.items[6].Target);
        configured.Public();
        configuredStruct.ReadOnly();
        added.Partial();
        addedStruct.Partial();
        const string expected = "class Before\n{\n}\n\nrecord Empty;\n\nrecord struct EmptyStruct;\n\npublic record Configured;\n\nreadonly record struct ConfiguredStruct;\n\npartial record Added;\n\npartial record struct AddedStruct;\n";
        OutputAssert.Equal(expected, file.ToString());
        OutputAssert.Equal(expected, file.ToString());
        Assert.Equal(2, calls);
        CompilationAssert.NoErrors(file.ToString());
    }

    [Theory]
    [InlineData(false, "namespace Example\n{\n    record Empty;\n\n    record struct EmptyStruct;\n\n    public record Configured;\n\n    readonly record struct ConfiguredStruct;\n\n    partial record Added;\n\n    partial record struct AddedStruct;\n}\n")]
    [InlineData(true, "namespace Example;\n\nrecord Empty;\n\nrecord struct EmptyStruct;\n\npublic record Configured;\n\nreadonly record struct ConfiguredStruct;\n\npartial record Added;\n\npartial record struct AddedStruct;\n")]
    public void NamespaceEntryPoints_ConfigureImmediatelyAndKeepReferences(bool fileScoped, string expected)
    {
        RecordBuilder? configured = null;
        RecordBuilder? configuredStruct = null;
        var calls = 0;
        var added = CSharp.Record("Added");
        var addedStruct = CSharp.RecordStruct("AddedStruct");
        var ns = fileScoped ? CSharp.FileScopedNamespace("Example") : CSharp.Namespace("Example");
        ns.Record("Empty").RecordStruct("EmptyStruct")
            .Record("Configured", r => { configured = r; calls++; })
            .RecordStruct("ConfiguredStruct", r => { configuredStruct = r; calls++; })
            .Add(added).Add(addedStruct);

        Assert.NotNull(configured);
        Assert.NotNull(configuredStruct);
        Assert.Equal(2, calls);
        Assert.Same(configured, ns.items[2].Target);
        Assert.Same(configuredStruct, ns.items[3].Target);
        Assert.Same(added, ns.items[4].Target);
        Assert.Same(addedStruct, ns.items[5].Target);
        configured.Public();
        configuredStruct.ReadOnly();
        added.Partial();
        addedStruct.Partial();
        OutputAssert.Equal(expected, ns.ToString());
        OutputAssert.Equal(expected, ns.ToString());
        Assert.Equal(2, calls);
        CompilationAssert.NoErrors(CSharp.File().Add(ns).ToString());
    }

    [Fact]
    public void NestedEntryPoints_ConfigureImmediatelyAndKeepReferences()
    {
        RecordBuilder? configured = null;
        RecordBuilder? configuredStruct = null;
        var calls = 0;
        var added = CSharp.Record("Added");
        var addedStruct = CSharp.RecordStruct("AddedStruct");
        var outer = CSharp.Class("Outer").Record("Empty").RecordStruct("EmptyStruct")
            .Record("Configured", r => { configured = r; calls++; })
            .RecordStruct("ConfiguredStruct", r => { configuredStruct = r; calls++; })
            .Add(added).Add(addedStruct);

        Assert.NotNull(configured);
        Assert.NotNull(configuredStruct);
        Assert.Equal(2, calls);
        Assert.Same(configured, outer.items[2].Target);
        Assert.Same(configuredStruct, outer.items[3].Target);
        Assert.Same(added, outer.items[4].Target);
        Assert.Same(addedStruct, outer.items[5].Target);
        configured.Private();
        configuredStruct.ReadOnly();
        added.Class("Nested");
        addedStruct.Raw("// changed");
        const string expected = "class Outer\n{\n    record Empty;\n\n    record struct EmptyStruct;\n\n    private record Configured;\n\n    readonly record struct ConfiguredStruct;\n\n    record Added\n    {\n        class Nested\n        {\n        }\n    }\n\n    record struct AddedStruct\n    {\n        // changed\n    }\n}\n";
        OutputAssert.Equal(expected, outer.ToString());
        OutputAssert.Equal(expected, outer.ToString());
        Assert.Equal(2, calls);
        CompilationAssert.NoErrors(CSharp.File().Add(outer).ToString());
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
        var configured = false;

        Assert.Throws<ArgumentException>("name", () => CSharp.Record(name));
        Assert.Throws<ArgumentException>("name", () => CSharp.Record(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => CSharp.RecordStruct(name));
        Assert.Throws<ArgumentException>("name", () => CSharp.RecordStruct(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => file.Record(name));
        Assert.Throws<ArgumentException>("name", () => file.Record(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => file.RecordStruct(name));
        Assert.Throws<ArgumentException>("name", () => file.RecordStruct(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => ns.Record(name));
        Assert.Throws<ArgumentException>("name", () => ns.Record(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => ns.RecordStruct(name));
        Assert.Throws<ArgumentException>("name", () => ns.RecordStruct(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => outer.Record(name));
        Assert.Throws<ArgumentException>("name", () => outer.Record(name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => outer.RecordStruct(name));
        Assert.Throws<ArgumentException>("name", () => outer.RecordStruct(name, _ => configured = true));
        Assert.False(configured);
        OutputAssert.Equal("", file.ToString());
        OutputAssert.Equal("namespace Example\n{\n}\n", ns.ToString());
        OutputAssert.Equal("class Outer\n{\n}\n", outer.ToString());
    }

    [Fact]
    public void NullArguments_ThrowWithParameterNameBeforeConfiguringOrAdding()
    {
        var file = CSharp.File();
        var ns = CSharp.Namespace("Example");
        var outer = CSharp.Class("Outer");
        var configured = false;

        Assert.Throws<ArgumentNullException>("name", () => CSharp.Record(null!));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Record(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Record("Value", null!));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Record(null!, null!));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.RecordStruct(null!));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.RecordStruct(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.RecordStruct("Value", null!));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.RecordStruct(null!, null!));
        Assert.Throws<ArgumentNullException>("name", () => file.Record(null!));
        Assert.Throws<ArgumentNullException>("name", () => file.Record(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => file.Record("Value", null!));
        Assert.Throws<ArgumentNullException>("name", () => file.RecordStruct(null!));
        Assert.Throws<ArgumentNullException>("name", () => file.RecordStruct(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => file.RecordStruct("Value", null!));
        Assert.Throws<ArgumentNullException>("recordBuilder", () => file.Add((RecordBuilder)null!));
        Assert.Throws<ArgumentNullException>("name", () => ns.Record(null!));
        Assert.Throws<ArgumentNullException>("name", () => ns.Record(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => ns.Record("Value", null!));
        Assert.Throws<ArgumentNullException>("name", () => ns.RecordStruct(null!));
        Assert.Throws<ArgumentNullException>("name", () => ns.RecordStruct(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => ns.RecordStruct("Value", null!));
        Assert.Throws<ArgumentNullException>("recordBuilder", () => ns.Add((RecordBuilder)null!));
        Assert.Throws<ArgumentNullException>("name", () => outer.Record(null!));
        Assert.Throws<ArgumentNullException>("name", () => outer.Record(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => outer.Record("Value", null!));
        Assert.Throws<ArgumentNullException>("name", () => outer.RecordStruct(null!));
        Assert.Throws<ArgumentNullException>("name", () => outer.RecordStruct(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => outer.RecordStruct("Value", null!));
        Assert.Throws<ArgumentNullException>("recordBuilder", () => outer.Add((RecordBuilder)null!));
        Assert.False(configured);
        OutputAssert.Equal("", file.ToString());
        OutputAssert.Equal("namespace Example\n{\n}\n", ns.ToString());
        OutputAssert.Equal("class Outer\n{\n}\n", outer.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FluentMethods_ReturnSameRecordBuilder(bool isStruct)
    {
        var builder = isStruct ? CSharp.RecordStruct("Value") : CSharp.Record("Value");

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
        Assert.Same(builder, builder.BaseType("Base"));
        Assert.Same(builder, builder.TypeParameter("T"));
        Assert.Same(builder, builder.TypeParameter("U", t => t.Constraint("class")));
        Assert.Same(builder, builder.PrimaryParameter("int", "X"));
        Assert.Same(builder, builder.PrimaryParameter("int", "Y", p => p.Default("0")));
        Assert.Same(builder, builder.Class("NestedClass"));
        Assert.Same(builder, builder.Struct("NestedStruct"));
        Assert.Same(builder, builder.Interface("NestedInterface"));
        Assert.Same(builder, builder.Record("NestedRecord"));
        Assert.Same(builder, builder.Record("ConfiguredRecord", _ => { }));
        Assert.Same(builder, builder.RecordStruct("NestedRecordStruct"));
        Assert.Same(builder, builder.RecordStruct("ConfiguredRecordStruct", _ => { }));
        Assert.Same(builder, builder.Add(CSharp.Record("Added")));
        Assert.Same(builder, builder.Add(CSharp.RecordStruct("AddedStruct")));
        Assert.Same(builder, builder.Raw("// raw"));
    }

    [Fact]
    public void EntryPoints_ReturnSameParentBuilder()
    {
        var file = CSharp.File();
        var ns = CSharp.Namespace("Example");
        var outer = CSharp.Class("Outer");

        Assert.Same(file, file.Record("First"));
        Assert.Same(file, file.Record("Second", _ => { }));
        Assert.Same(file, file.RecordStruct("Third"));
        Assert.Same(file, file.RecordStruct("Fourth", _ => { }));
        Assert.Same(file, file.Add(CSharp.Record("Fifth")));
        Assert.Same(file, file.Add(CSharp.RecordStruct("Sixth")));
        Assert.Same(ns, ns.Record("First"));
        Assert.Same(ns, ns.Record("Second", _ => { }));
        Assert.Same(ns, ns.RecordStruct("Third"));
        Assert.Same(ns, ns.RecordStruct("Fourth", _ => { }));
        Assert.Same(ns, ns.Add(CSharp.Record("Fifth")));
        Assert.Same(ns, ns.Add(CSharp.RecordStruct("Sixth")));
        Assert.Same(outer, outer.Record("First"));
        Assert.Same(outer, outer.Record("Second", _ => { }));
        Assert.Same(outer, outer.RecordStruct("Third"));
        Assert.Same(outer, outer.RecordStruct("Fourth", _ => { }));
        Assert.Same(outer, outer.Add(CSharp.Record("Fifth")));
        Assert.Same(outer, outer.Add(CSharp.RecordStruct("Sixth")));
    }

    [Fact]
    public void CompleteFile_RendersRecordsAndCompiles()
    {
        var file = CSharp.File().Namespace("Example", n => n
            .Record("Shape", r => r.Partial().Abstract().Public())
            .Record("Circle", r => r.Public().Sealed().PrimaryParameter("double", "Radius").BaseType("Shape"))
            .RecordStruct("Point", r => r.Public().ReadOnly().PrimaryParameter("int", "X").PrimaryParameter("int", "Y"))
            .Record("Box", r => r.Public().TypeParameter("T", t => t.Constraint("class")).PrimaryParameter("T", "Value"))
            .Record("Container", r => r.Class("Nested"))
            .Class("Outer", c => c.Record("Nested").RecordStruct("NestedStruct")));
        const string expected = "namespace Example\n{\n    public abstract partial record Shape;\n\n    public sealed record Circle(double Radius) : Shape;\n\n    public readonly record struct Point(int X, int Y);\n\n    public record Box<T>(T Value) where T : class;\n\n    record Container\n    {\n        class Nested\n        {\n        }\n    }\n\n    class Outer\n    {\n        record Nested;\n\n        record struct NestedStruct;\n    }\n}\n";

        OutputAssert.Equal(expected, file.ToString());
        OutputAssert.Equal(expected, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }
}
