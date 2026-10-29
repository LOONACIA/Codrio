using System;
using Xunit;

namespace Codrio.Tests;

public class TypeAttributeTests
{
    [Fact]
    public void Attribute_RendersZeroOneAndMultipleArgumentsVerbatim()
    {
        var builder = CSharp.Class("Widget")
            .Attribute("X")
            .Attribute("Obsolete", "\"Use Gadget.\"")
            .Attribute("Obsolete", "\"Use Gadget.\"", "true")
            .Attribute("global::System.ObsoleteAttribute", "  \"message\"  ", " false ")
            .Attribute("X", "")
            .Attribute("return: X", "1")
            .Attribute("");

        const string expected = "[X]\n[Obsolete(\"Use Gadget.\")]\n[Obsolete(\"Use Gadget.\", true)]\n[global::System.ObsoleteAttribute(  \"message\"  ,  false )]\n[X()]\n[return: X(1)]\n[]\nclass Widget\n{\n}\n";
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
    }

    [Fact]
    public void Attributes_PreserveOrderAndDuplicatesBeforeDeclarationRegardlessOfCallOrder()
    {
        var builder = CSharp.Class("Widget").Attribute("First").Public().Sealed()
            .Attribute("Second").Attribute("First");

        OutputAssert.Equal("[First]\n[Second]\n[First]\npublic sealed class Widget\n{\n}\n", builder.ToString());
        OutputAssert.Equal("[Serializable]\n[Obsolete(\"Use Gadget.\", true)]\npublic class Widget\n{\n}\n",
            CSharp.Class("Widget", c => c.Public().Attribute("Serializable").Attribute("Obsolete", "\"Use Gadget.\"", "true")).ToString());
    }

    [Fact]
    public void Attributes_WorkWithAllTypeKindsAndDeclarationParts()
    {
        OutputAssert.Equal("[X]\npublic sealed partial class Widget<T>(T value) : Base, IWidget where T : class, new()\n{\n}\n",
            CSharp.Class("Widget", c => c.Public().Sealed().Partial()
                .TypeParameter("T", t => t.Constraint("class").Constraint("new()"))
                .PrimaryParameter("T", "value").BaseType("Base").BaseType("IWidget").Attribute("X")).ToString());
        OutputAssert.Equal("[X]\npublic readonly partial struct Value<T>(T value) : IValue where T : struct\n{\n}\n",
            CSharp.Struct("Value", s => s.Public().ReadOnly().Partial()
                .TypeParameter("T", t => t.Constraint("struct"))
                .PrimaryParameter("T", "value").BaseType("IValue").Attribute("X")).ToString());
        OutputAssert.Equal("[X]\npublic partial interface IValue<T> : IBase where T : class\n{\n}\n",
            CSharp.Interface("IValue", i => i.Public().Partial()
                .TypeParameter("T", t => t.Constraint("class")).BaseType("IBase").Attribute("X")).ToString());
        OutputAssert.Equal("[X]\npublic sealed partial record Data<T>(T Value) : Base where T : class;\n",
            CSharp.Record("Data", r => r.Public().Sealed().Partial()
                .TypeParameter("T", t => t.Constraint("class"))
                .PrimaryParameter("T", "Value").BaseType("Base").Attribute("X")).ToString());
        OutputAssert.Equal("[X]\npublic readonly partial record struct Data<T>(T Value) : IValue where T : struct;\n",
            CSharp.RecordStruct("Data", r => r.Public().ReadOnly().Partial()
                .TypeParameter("T", t => t.Constraint("struct"))
                .PrimaryParameter("T", "Value").BaseType("IValue").Attribute("X")).ToString());
        OutputAssert.Equal("[Flags]\npublic enum Color : byte\n{\n    Red = 1\n}\n",
            CSharp.Enum("Color", e => e.Public().UnderlyingType("byte").Member("Red", "1").Attribute("Flags")).ToString());
    }

    [Theory]
    [InlineData(false, "record")]
    [InlineData(true, "record struct")]
    public void RecordAttributes_DoNotCountAsBodyItems(bool isStruct, string keyword)
    {
        var builder = isStruct ? CSharp.RecordStruct("Point") : CSharp.Record("Point");
        builder.Attribute("Obsolete");

        OutputAssert.Equal("[Obsolete]\n" + keyword + " Point;\n", builder.ToString());
        builder.PrimaryParameter("int", "X");
        OutputAssert.Equal("[Obsolete]\n" + keyword + " Point(int X);\n", builder.ToString());
        builder.Property("int", "Y");
        OutputAssert.Equal("[Obsolete]\n" + keyword + " Point(int X)\n{\n    int Y { get; set; }\n}\n", builder.ToString());
    }

    [Fact]
    public void NestedAttributes_FollowIndentationAndItemSpacing()
    {
        var file = CSharp.File().Namespace("App", n => n.Class("Outer", c => c
            .Field("int", "value")
            .Class("Inner", i => i.Attribute("Obsolete"))
            .Enum("Color", e => e.Attribute("Flags").Member("Red", "1"))));

        const string expected = "namespace App\n{\n    class Outer\n    {\n        int value;\n\n        [Obsolete]\n        class Inner\n        {\n        }\n\n        [Flags]\n        enum Color\n        {\n            Red = 1\n        }\n    }\n}\n";
        OutputAssert.Equal(expected, file.ToString());
    }

    [Fact]
    public void FileAndNamespaceAttributes_UseCurrentIndentation()
    {
        OutputAssert.Equal("[Obsolete]\nclass Widget\n{\n}\n\n[Flags]\nenum Color\n{\n}\n",
            CSharp.File().Class("Widget", c => c.Attribute("Obsolete"))
                .Enum("Color", e => e.Attribute("Flags")).ToString());
        OutputAssert.Equal("namespace App\n{\n    [Obsolete]\n    class Widget\n    {\n    }\n\n    [Flags]\n    enum Color\n    {\n    }\n}\n",
            CSharp.File().Namespace("App", n => n.Class("Widget", c => c.Attribute("Obsolete"))
                .Enum("Color", e => e.Attribute("Flags"))).ToString());
        OutputAssert.Equal("namespace App;\n\n[Obsolete]\nclass Widget\n{\n}\n\n[Flags]\nenum Color\n{\n}\n",
            CSharp.File().FileScopedNamespace("App", n => n.Class("Widget", c => c.Attribute("Obsolete"))
                .Enum("Color", e => e.Attribute("Flags"))).ToString());
    }

    [Fact]
    public void AddedTypes_IncludeAttributesAddedBeforeAndAfterAdd()
    {
        var classBuilder = CSharp.Class("C").Attribute("First");
        var structBuilder = CSharp.Struct("S").Attribute("First");
        var interfaceBuilder = CSharp.Interface("I").Attribute("First");
        var recordBuilder = CSharp.Record("R").Attribute("First");
        var recordStructBuilder = CSharp.RecordStruct("RS").Attribute("First");
        var enumBuilder = CSharp.Enum("E").Attribute("First");
        var file = CSharp.File().Add(classBuilder).Add(structBuilder).Add(interfaceBuilder)
            .Add(recordBuilder).Add(recordStructBuilder).Add(enumBuilder);
        var ns = CSharp.Namespace("App").Add(classBuilder).Add(enumBuilder);
        var outer = CSharp.Class("Outer").Add(classBuilder).Add(enumBuilder);

        classBuilder.Attribute("Second");
        structBuilder.Attribute("Second");
        interfaceBuilder.Attribute("Second");
        recordBuilder.Attribute("Second");
        recordStructBuilder.Attribute("Second");
        enumBuilder.Attribute("Second");

        OutputAssert.Equal("[First]\n[Second]\nclass C\n{\n}\n\n[First]\n[Second]\nstruct S\n{\n}\n\n[First]\n[Second]\ninterface I\n{\n}\n\n[First]\n[Second]\nrecord R;\n\n[First]\n[Second]\nrecord struct RS;\n\n[First]\n[Second]\nenum E\n{\n}\n", file.ToString());
        OutputAssert.Equal("namespace App\n{\n    [First]\n    [Second]\n    class C\n    {\n    }\n\n    [First]\n    [Second]\n    enum E\n    {\n    }\n}\n", ns.ToString());
        OutputAssert.Equal("class Outer\n{\n    [First]\n    [Second]\n    class C\n    {\n    }\n\n    [First]\n    [Second]\n    enum E\n    {\n    }\n}\n", outer.ToString());
    }

    [Fact]
    public void Attribute_CapturesArgumentsAtCallTime()
    {
        var arguments = new[] { "\"original\"", "true" };
        var builder = CSharp.Class("Widget").Attribute("Obsolete", arguments);

        arguments[0] = "\"changed\"";
        arguments[1] = null!;

        OutputAssert.Equal("[Obsolete(\"original\", true)]\nclass Widget\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void NullArguments_ThrowAtCallTimeWithoutAddingAttribute()
    {
        var builder = CSharp.Class("Widget").Attribute("Existing");

        Assert.Throws<ArgumentNullException>("name", () => builder.Attribute(null!));
        Assert.Throws<ArgumentNullException>("arguments", () => builder.Attribute("X", (string[])null!));
        Assert.Throws<ArgumentNullException>("arguments", () => builder.Attribute("X", "a", null!));
        OutputAssert.Equal("[Existing]\nclass Widget\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void Attribute_ReturnsSameConcreteBuilder()
    {
        var classBuilder = CSharp.Class("C");
        var structBuilder = CSharp.Struct("S");
        var interfaceBuilder = CSharp.Interface("I");
        var recordBuilder = CSharp.Record("R");
        var recordStructBuilder = CSharp.RecordStruct("RS");
        var enumBuilder = CSharp.Enum("E");

        ClassBuilder returnedClass = classBuilder.Attribute("X");
        StructBuilder returnedStruct = structBuilder.Attribute("X");
        InterfaceBuilder returnedInterface = interfaceBuilder.Attribute("X");
        RecordBuilder returnedRecord = recordBuilder.Attribute("X");
        RecordBuilder returnedRecordStruct = recordStructBuilder.Attribute("X");
        EnumBuilder returnedEnum = enumBuilder.Attribute("X");

        Assert.Same(classBuilder, returnedClass);
        Assert.Same(structBuilder, returnedStruct);
        Assert.Same(interfaceBuilder, returnedInterface);
        Assert.Same(recordBuilder, returnedRecord);
        Assert.Same(recordStructBuilder, returnedRecordStruct);
        Assert.Same(enumBuilder, returnedEnum);
    }

    [Fact]
    public void CompleteFile_WithAttributesOnAllTypeKinds_Compiles()
    {
        var file = CSharp.File().Using("System").Namespace("App", n => n
            .Class("Widget", c => c.Public().Attribute("Serializable").Attribute("Obsolete", "\"Use Gadget.\""))
            .Class("TagAttribute", c => c.Public().BaseType("System.Attribute").PrimaryParameter("string", "name")
                .Attribute("System.AttributeUsage", "System.AttributeTargets.Class", "AllowMultiple = true"))
            .Class("Tagged", c => c.Attribute("Tag", "\"a\"").Attribute("Tag", "\"b\""))
            .Enum("Color", e => e.Attribute("Flags").UnderlyingType("byte").Member("Red", "1").Member("Green", "2"))
            .Struct("Value", s => s.Attribute("System.Runtime.InteropServices.StructLayout", "System.Runtime.InteropServices.LayoutKind.Sequential"))
            .Interface("IWidget", i => i.Attribute("Obsolete"))
            .Record("Data", r => r.Attribute("Obsolete").PrimaryParameter("int", "X"))
            .RecordStruct("Point", r => r.Attribute("Obsolete").PrimaryParameter("int", "X"))
            .Class("Outer", c => c.Class("Inner", i => i.Attribute("Obsolete"))));

        CompilationAssert.NoErrors(file.ToString());
    }
}
