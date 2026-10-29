using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Codrio.Tests;

public class AttributeTypeOverloadTests
{
    [Theory]
    [InlineData(false, 0, "[global::System.ObsoleteAttribute]\n")]
    [InlineData(false, 1, "[global::System.ObsoleteAttribute(\"message\")]\n")]
    [InlineData(false, 2, "[global::System.ObsoleteAttribute(\"message\", true)]\n")]
    [InlineData(true, 0, "[global::System.ObsoleteAttribute]\n")]
    [InlineData(true, 1, "[global::System.ObsoleteAttribute(\"message\")]\n")]
    [InlineData(true, 2, "[global::System.ObsoleteAttribute(\"message\", true)]\n")]
    public void Attributes_RenderZeroOneAndMultipleArguments(bool generic, int count, string attribute)
    {
        var arguments = count == 0 ? Array.Empty<string>() : count == 1 ? new[] { "\"message\"" } : new[] { "\"message\"", "true" };
        var type = CSharp.Class("Widget");
        var member = CSharp.Method("Run");
        if (generic)
        {
            type.Attribute<ObsoleteAttribute>(arguments);
            member.Attribute<ObsoleteAttribute>(arguments);
        }
        else
        {
            type.Attribute(typeof(ObsoleteAttribute), arguments);
            member.Attribute(typeof(ObsoleteAttribute), arguments);
        }

        OutputAssert.Equal(attribute + "class Widget\n{\n}\n", type.ToString());
        OutputAssert.Equal(attribute + "void Run();\n", member.ToString());
    }

    [Fact]
    public void Attributes_RenderExamplesWithQualifiedNamesAndSuffixes()
    {
        OutputAssert.Equal("[global::System.SerializableAttribute]\n[global::System.ObsoleteAttribute(\"Use Gadget.\", true)]\npublic class Widget\n{\n}\n",
            CSharp.Class("Widget", c => c.Public().Attribute<SerializableAttribute>()
                .Attribute(typeof(ObsoleteAttribute), "\"Use Gadget.\"", "true")).ToString());
        OutputAssert.Equal("[global::System.Diagnostics.ConditionalAttribute(\"DEBUG\")]\nvoid Trace();\n",
            CSharp.Method("Trace", m => m.Attribute<ConditionalAttribute>("\"DEBUG\"")).ToString());
    }

    [Fact]
    public void TypeAttributes_AllKindsPreserveOrderDuplicatesDocumentationAndChaining()
    {
        AssertTypeAttributes(CSharp.Class("Sample"), "class Sample\n{\n}\n");
        AssertTypeAttributes(CSharp.Struct("Sample"), "struct Sample\n{\n}\n");
        AssertTypeAttributes(CSharp.Interface("Sample"), "interface Sample\n{\n}\n");
        AssertTypeAttributes(CSharp.Record("Sample"), "record Sample;\n");
        AssertTypeAttributes(CSharp.RecordStruct("Sample"), "record struct Sample;\n");
        AssertTypeAttributes(CSharp.Enum("Sample"), "enum Sample\n{\n}\n");
    }

    private static void AssertTypeAttributes<TSelf>(TSelf builder, string declaration)
        where TSelf : TypeBuilder<TSelf>
    {
        builder.Attribute("global::System.ObsoleteAttribute");
        TSelf fromType = builder.Attribute(typeof(SerializableAttribute));
        builder.Doc(d => d.Summary("A sample."));
        TSelf fromGeneric = builder.Attribute<ObsoleteAttribute>();
        builder.Attribute("global::System.SerializableAttribute");

        Assert.Same(builder, fromType);
        Assert.Same(builder, fromGeneric);
        OutputAssert.Equal("/// <summary>A sample.</summary>\n" +
            "[global::System.ObsoleteAttribute]\n[global::System.SerializableAttribute]\n" +
            "[global::System.ObsoleteAttribute]\n[global::System.SerializableAttribute]\n" + declaration,
            builder.ToString());
    }

    [Fact]
    public void MemberAttributes_AllKindsPreserveOrderDuplicatesDocumentationChainingAndIndentation()
    {
        AssertMemberAttributes(CSharp.Method("Run"), "void Run();\n", (c, m) => c.Add(m));
        AssertMemberAttributes(CSharp.Field("int", "value"), "int value;\n", (c, f) => c.Add(f));
        AssertMemberAttributes(CSharp.Constructor(), "()\n{\n}\n", (c, k) => c.Add(k), "Service()\n{\n}\n");
        AssertMemberAttributes(CSharp.Property("int", "Value"), "int Value { get; set; }\n", (c, p) => c.Add(p));
        AssertMemberAttributes(CSharp.Indexer("int", x => x.Parameter("int", "index").Get()),
            "int this[int index] { get; }\n", (c, x) => c.Add(x));
        AssertMemberAttributes(CSharp.Event("Action", "Changed"), "event Action Changed;\n", (c, e) => c.Add(e));
        AssertMemberAttributes(CSharp.Delegate("Handler"), "delegate void Handler();\n", (c, d) => c.Add(d));
        AssertMemberAttributes(CSharp.Operator("Service", "+", o => o.Parameter("Service", "value")),
            "Service operator +(Service value);\n", (c, o) => c.Add(o));
    }

    private static void AssertMemberAttributes<TSelf>(TSelf builder, string declaration,
        Action<ClassBuilder, TSelf> add, string? containedDeclaration = null)
        where TSelf : MemberBuilder<TSelf>
    {
        builder.Attribute("global::System.ObsoleteAttribute");
        TSelf fromType = builder.Attribute(typeof(ObsoleteAttribute), "\"message\"");
        builder.Doc(d => d.Summary("A member."));
        TSelf fromGeneric = builder.Attribute<ObsoleteAttribute>();
        builder.Attribute("global::System.ObsoleteAttribute", "\"message\"");

        Assert.Same(builder, fromType);
        Assert.Same(builder, fromGeneric);
        const string preamble = "/// <summary>A member.</summary>\n" +
            "[global::System.ObsoleteAttribute]\n[global::System.ObsoleteAttribute(\"message\")]\n" +
            "[global::System.ObsoleteAttribute]\n[global::System.ObsoleteAttribute(\"message\")]\n";
        OutputAssert.Equal(preamble + declaration, builder.ToString());

        var owner = CSharp.Class("Service");
        add(owner, builder);
        var expectedMember = preamble + (containedDeclaration ?? declaration);
        OutputAssert.Equal("class Service\n{\n    " + expectedMember.TrimEnd('\n').Replace("\n", "\n    ") + "\n}\n",
            owner.ToString());
    }

    [Fact]
    public void StringTypeAndGenericAttributes_RenderIdentically()
    {
        const string attribute = "[global::System.ObsoleteAttribute(\"message\", true)]\n";
        const string name = "global::System.ObsoleteAttribute";
        OutputAssert.Equal(attribute + "class Widget\n{\n}\n",
            CSharp.Class("Widget").Attribute(name, "\"message\"", "true").ToString());
        OutputAssert.Equal(attribute + "class Widget\n{\n}\n",
            CSharp.Class("Widget").Attribute(typeof(ObsoleteAttribute), "\"message\"", "true").ToString());
        OutputAssert.Equal(attribute + "class Widget\n{\n}\n",
            CSharp.Class("Widget").Attribute<ObsoleteAttribute>("\"message\"", "true").ToString());
        OutputAssert.Equal(attribute + "void Run();\n",
            CSharp.Method("Run").Attribute(name, "\"message\"", "true").ToString());
        OutputAssert.Equal(attribute + "void Run();\n",
            CSharp.Method("Run").Attribute(typeof(ObsoleteAttribute), "\"message\"", "true").ToString());
        OutputAssert.Equal(attribute + "void Run();\n",
            CSharp.Method("Run").Attribute<ObsoleteAttribute>("\"message\"", "true").ToString());
    }

    [Fact]
    public void TypeAttributes_InvalidTypesAndArgumentsThrowInOrderWithoutAddingAttributes()
    {
        var builder = CSharp.Class("Widget").Attribute<SerializableAttribute>();
        var before = builder.ToString();

        Assert.Throws<ArgumentException>("type", () => builder.Attribute(typeof(List<>)));
        OutputAssert.Equal(before, builder.ToString());
        Assert.Throws<ArgumentNullException>("type", () => builder.Attribute((Type)null!));
        OutputAssert.Equal(before, builder.ToString());
        Assert.Throws<ArgumentException>("type", () => builder.Attribute(typeof(List<>), (string[])null!));
        OutputAssert.Equal(before, builder.ToString());
        Assert.Throws<ArgumentNullException>("type", () => builder.Attribute((Type)null!, (string[])null!));
        OutputAssert.Equal(before, builder.ToString());
        Assert.Throws<ArgumentNullException>("arguments", () => builder.Attribute(typeof(ObsoleteAttribute), (string[])null!));
        OutputAssert.Equal(before, builder.ToString());
        Assert.Throws<ArgumentNullException>("arguments", () => builder.Attribute(typeof(ObsoleteAttribute), "\"message\"", null!));
        OutputAssert.Equal(before, builder.ToString());
        Assert.Throws<ArgumentNullException>("arguments", () => builder.Attribute<ObsoleteAttribute>((string[])null!));
        OutputAssert.Equal(before, builder.ToString());
        Assert.Throws<ArgumentNullException>("arguments", () => builder.Attribute<ObsoleteAttribute>("\"message\"", null!));
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void MemberAttributes_InvalidTypesAndArgumentsThrowInOrderWithoutAddingAttributes()
    {
        var builder = CSharp.Method("Run").Attribute<ObsoleteAttribute>();
        var before = builder.ToString();

        Assert.Throws<ArgumentException>("type", () => builder.Attribute(typeof(List<>)));
        OutputAssert.Equal(before, builder.ToString());
        Assert.Throws<ArgumentNullException>("type", () => builder.Attribute((Type)null!));
        OutputAssert.Equal(before, builder.ToString());
        Assert.Throws<ArgumentException>("type", () => builder.Attribute(typeof(List<>), (string[])null!));
        OutputAssert.Equal(before, builder.ToString());
        Assert.Throws<ArgumentNullException>("type", () => builder.Attribute((Type)null!, (string[])null!));
        OutputAssert.Equal(before, builder.ToString());
        Assert.Throws<ArgumentNullException>("arguments", () => builder.Attribute(typeof(ObsoleteAttribute), (string[])null!));
        OutputAssert.Equal(before, builder.ToString());
        Assert.Throws<ArgumentNullException>("arguments", () => builder.Attribute(typeof(ObsoleteAttribute), "\"message\"", null!));
        OutputAssert.Equal(before, builder.ToString());
        Assert.Throws<ArgumentNullException>("arguments", () => builder.Attribute<ObsoleteAttribute>((string[])null!));
        OutputAssert.Equal(before, builder.ToString());
        Assert.Throws<ArgumentNullException>("arguments", () => builder.Attribute<ObsoleteAttribute>("\"message\"", null!));
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void TypeOverloads_DoNotRequireAttributeDerivedTypes()
    {
        OutputAssert.Equal("[int]\nclass Widget\n{\n}\n", CSharp.Class("Widget").Attribute(typeof(int)).ToString());
        OutputAssert.Equal("[string]\nvoid Run();\n", CSharp.Method("Run").Attribute(typeof(string)).ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Attributes_CaptureRawArgumentsAtCallTime(bool generic)
    {
        var arguments = new[] { "  \"message\"  ", " false " };
        var type = CSharp.Class("Widget");
        var member = CSharp.Method("Run");
        if (generic)
        {
            type.Attribute<ObsoleteAttribute>(arguments);
            member.Attribute<ObsoleteAttribute>(arguments);
        }
        else
        {
            type.Attribute(typeof(ObsoleteAttribute), arguments);
            member.Attribute(typeof(ObsoleteAttribute), arguments);
        }

        arguments[0] = "\"changed\"";
        arguments[1] = null!;
        const string attribute = "[global::System.ObsoleteAttribute(  \"message\"  ,  false )]\n";
        OutputAssert.Equal(attribute + "class Widget\n{\n}\n", type.ToString());
        OutputAssert.Equal(attribute + "void Run();\n", member.ToString());
    }

    [Fact]
    public void CompleteFile_WithTypeAndGenericAttributesOnTypesAndMembers_Compiles()
    {
        var file = CSharp.File().Namespace("App", n => n
            .Class("Service", c => c.Public().Attribute<SerializableAttribute>()
                .Field("int", "count", f => f.Private().Attribute(typeof(NonSerializedAttribute)))
                .Constructor(k => k.Public().Attribute<ObsoleteAttribute>().Body())
                .Method("Trace", m => m.Public().Attribute(typeof(ConditionalAttribute), "\"DEBUG\"").Body())
                .Property("int", "Count", p => p.Public().Attribute<BrowsableAttribute>("false"))
                .Indexer("int", x => x.Public().Parameter("int", "index")
                    .Attribute(typeof(IndexerNameAttribute), "\"Entry\"").Arrow("count + index"))
                .Event("global::System.Action", "Changed", e => e.Public().Attribute<ObsoleteAttribute>())
                .Delegate("Handler", d => d.Public().Attribute(typeof(ObsoleteAttribute)))
                .Operator("Service", "+", o => o.Public().Static().Parameter("Service", "left").Parameter("Service", "right")
                    .Attribute<ObsoleteAttribute>().Arrow("left")))
            .Enum("Options", e => e.Attribute(typeof(FlagsAttribute)).Member("First", "1").Member("Second", "2"))
            .Struct("Value", s => s.Attribute<StructLayoutAttribute>("global::System.Runtime.InteropServices.LayoutKind.Sequential"))
            .Interface("IService", i => i.Attribute(typeof(ObsoleteAttribute)))
            .Record("Data", r => r.Attribute<ObsoleteAttribute>())
            .RecordStruct("Point", r => r.Attribute(typeof(ObsoleteAttribute))));

        CompilationAssert.NoErrors(file.ToString());
    }
}
