using System;
using Xunit;

namespace Codrio.Tests;

public class ParameterBuilderTests
{
    [Fact]
    public void Attribute_RendersInlineWithoutLineEnding()
    {
        CSharp.Method("Run").Parameter("string", "value", p =>
            OutputAssert.Equal("[NotNull] string value", p.Attribute("NotNull").ToString()));
    }

    [Fact]
    public void Attributes_PreserveOrderDuplicatesAndRawArguments()
    {
        CSharp.Method("Run").Parameter("string", "value", p =>
        {
            p.Attribute("NotNull", Array.Empty<string>())
                .Attribute("Example", "\"x\"", "typeof(string)", "Enabled = true")
                .Attribute("NotNull");

            OutputAssert.Equal("[NotNull] [Example(\"x\", typeof(string), Enabled = true)] [NotNull] string value", p.ToString());
        });
    }

    [Theory]
    [InlineData("this")]
    [InlineData("ref")]
    [InlineData("out")]
    [InlineData("in")]
    [InlineData("params")]
    public void Attribute_PrecedesEachModifierAndDefault(string modifier)
    {
        CSharp.Method("Run").Parameter("string", "value", p =>
        {
            switch (modifier)
            {
                case "this": p.This(); break;
                case "ref": p.Ref(); break;
                case "out": p.Out(); break;
                case "in": p.In(); break;
                case "params": p.Params(); break;
            }

            p.Default("\"\"").Attribute("NotNull").Attribute("Description", "\"x\"");

            OutputAssert.Equal("[NotNull] [Description(\"x\")] " + modifier + " string value = \"\"", p.ToString());
        });
    }

    [Fact]
    public void Attribute_PrecedesCombinedModifiersAndDefault()
    {
        CSharp.Method("Run").Parameter("string", "value", p =>
        {
            p.Params().In().Out().Ref().This().Default("\"\"").Attribute("Example");

            OutputAssert.Equal("[Example] this ref out in params string value = \"\"", p.ToString());
        });
    }

    [Theory]
    [InlineData("property: JsonPropertyName")]
    [InlineData("param: Description")]
    public void Attribute_PreservesTargetPrefix(string name)
    {
        CSharp.Record("Person").PrimaryParameter("string", "Name", p =>
            OutputAssert.Equal("[" + name + "(\"name\")] string Name", p.Attribute(name, "\"name\"").ToString()));
    }

    [Fact]
    public void Attribute_CapturesArgumentsAtCallTime()
    {
        CSharp.Method("Run").Parameter("string", "value", p =>
        {
            var arguments = new[] { "\"original\"", "true" };
            p.Attribute("Example", arguments);
            arguments[0] = "\"changed\"";
            arguments[1] = null!;

            OutputAssert.Equal("[Example(\"original\", true)] string value", p.ToString());
        });
    }

    [Fact]
    public void Attribute_RejectsNullAtCallTimeWithoutAddingAttribute()
    {
        CSharp.Method("Run").Parameter("string", "value", p =>
        {
            p.Attribute("Existing");

            Assert.Throws<ArgumentNullException>("name", () => p.Attribute(null!));
            Assert.Throws<ArgumentNullException>("arguments", () => p.Attribute("X", (string[])null!));
            Assert.Throws<ArgumentNullException>("arguments", () => p.Attribute("X", "a", null!));
            OutputAssert.Equal("[Existing] string value", p.ToString());
        });
    }

    [Fact]
    public void Attribute_ReturnsSameBuilder()
    {
        CSharp.Method("Run").Parameter("string", "value", p => Assert.Same(p, p.Attribute("NotNull")));
    }

    [Fact]
    public void Method_RendersAttributeOnSelectedParameter()
    {
        var builder = CSharp.Method("Log", m => m.Parameter("string", "message")
            .Parameter("string", "caller", p => p.Attribute("CallerMemberName").Default("\"\"")));

        OutputAssert.Equal("void Log(string message, [CallerMemberName] string caller = \"\");\n", builder.ToString());
    }

    [Fact]
    public void Constructor_RendersParameterAttributeInline()
    {
        var builder = CSharp.Class("Service", c => c.Constructor(k => k.Public()
            .Parameter("string", "name", p => p.Attribute("NotNull"))));

        OutputAssert.Equal("class Service\n{\n    public Service([NotNull] string name)\n    {\n    }\n}\n", builder.ToString());
    }

    [Fact]
    public void Delegate_RendersParameterAttributeInline()
    {
        var builder = CSharp.Delegate("Handler", d => d.Parameter("string", "value", p => p.Attribute("NotNull")));

        OutputAssert.Equal("delegate void Handler([NotNull] string value);\n", builder.ToString());
    }

    [Fact]
    public void Indexer_RendersParameterAttributeInline()
    {
        var builder = CSharp.Indexer("int", x => x.Parameter("string", "key", p => p.Attribute("NotNull")).Get());

        OutputAssert.Equal("int this[[NotNull] string key] { get; }\n", builder.ToString());
    }

    [Fact]
    public void Operator_RendersParameterAttributeInline()
    {
        var builder = CSharp.Operator("Value", "+", o => o.Public().Static()
            .Parameter("Value", "value", p => p.Attribute("Description", "\"x\"")).Arrow("value"));

        OutputAssert.Equal("public static Value operator +([Description(\"x\")] Value value) => value;\n", builder.ToString());
    }

    [Fact]
    public void ClassAndStruct_RenderPrimaryParameterAttributeInline()
    {
        var classBuilder = CSharp.Class("Service", c => c.PrimaryParameter("string", "name", p => p.Attribute("NotNull")));
        var structBuilder = CSharp.Struct("Value", s => s.PrimaryParameter("int", "id", p => p.Attribute("Description", "\"x\"")));

        OutputAssert.Equal("class Service([NotNull] string name)\n{\n}\n", classBuilder.ToString());
        OutputAssert.Equal("struct Value([Description(\"x\")] int id)\n{\n}\n", structBuilder.ToString());
    }

    [Theory]
    [InlineData(false, "record")]
    [InlineData(true, "record struct")]
    public void Record_RendersPrimaryParameterAttributeAndEndsWithSemicolon(bool isStruct, string keyword)
    {
        var builder = isStruct ? CSharp.RecordStruct("Person") : CSharp.Record("Person");
        builder.PrimaryParameter("string", "Name", p => p.Attribute("property: JsonPropertyName", "\"name\""));

        OutputAssert.Equal(keyword + " Person([property: JsonPropertyName(\"name\")] string Name);\n", builder.ToString());
    }

    [Fact]
    public void CompleteFile_WithAttributesOnAllParameterLocations_Compiles()
    {
        var file = CSharp.File().NullableEnable().Namespace("App", n => n
            .Class("Service", c => c.Public()
                .Constructor(k => k.Public().Parameter("string", "name", p => p.Attribute("System.Diagnostics.CodeAnalysis.NotNull")))
                .Method("TryGet", m => m.Public().Returns("bool")
                    .Parameter("string?", "value", p => p.Out().Attribute("System.Diagnostics.CodeAnalysis.NotNullWhen", "true"))
                    .Body(b => b.Line("value = \"found\";").Line("return true;")))
                .Method("Log", m => m.Public().Parameter("string", "message")
                    .Parameter("string", "caller", p => p.Attribute("System.Runtime.CompilerServices.CallerMemberName").Default("\"\""))
                    .Body())
                .Indexer("int", x => x.Public().Parameter("string", "key", p => p.Attribute("System.Diagnostics.CodeAnalysis.NotNull"))
                    .Arrow("key.Length"))
                .Operator("Service", "+", o => o.Public().Static()
                    .Parameter("Service", "value", p => p.Attribute("System.Diagnostics.CodeAnalysis.NotNull")).Arrow("value")))
            .Class("TextExtensions", c => c.Public().Static()
                .Method("EnsureText", m => m.Public().Static()
                    .Parameter("string?", "text", p => p.Attribute("System.Diagnostics.CodeAnalysis.NotNull").This())
                    .Body(b => b.Line("if (text is null) throw new System.ArgumentNullException(nameof(text));"))))
            .Delegate("Handler", d => d.Public()
                .Parameter("string", "value", p => p.Attribute("System.Diagnostics.CodeAnalysis.NotNull")))
            .Record("Entry", r => r.Public()
                .PrimaryParameter("int", "Id", p => p.Attribute("property: System.ComponentModel.Description", "\"Identifier\"")))
            .Class("Named", c => c.Public()
                .PrimaryParameter("string", "name", p => p.Attribute("param: System.Diagnostics.CodeAnalysis.NotNull")))
            .Struct("Number", s => s.Public()
                .PrimaryParameter("int", "id", p => p.Attribute("System.ComponentModel.Description", "\"Identifier\"")))
            .RecordStruct("Point", r => r.Public()
                .PrimaryParameter("int", "Id", p => p.Attribute("property: System.ComponentModel.Description", "\"Identifier\""))));

        CompilationAssert.NoErrors(file.ToString());
    }

    [Fact]
    public void ToString_RendersParameterFragmentWithoutLineEnding()
    {
        CSharp.Class("Service").PrimaryParameter("string", "name", p =>
            OutputAssert.Equal("string name", p.ToString()));
    }

    [Fact]
    public void Modifiers_RenderIndividually()
    {
        CSharp.Class("Example")
            .PrimaryParameter("int", "value", p => OutputAssert.Equal("ref int value", p.Ref().ToString()))
            .PrimaryParameter("int", "value", p => OutputAssert.Equal("out int value", p.Out().ToString()))
            .PrimaryParameter("int", "value", p => OutputAssert.Equal("in int value", p.In().ToString()))
            .PrimaryParameter("int[]", "values", p => OutputAssert.Equal("params int[] values", p.Params().ToString()))
            .PrimaryParameter("int", "value", p => OutputAssert.Equal("this int value", p.This().ToString()));
    }

    [Fact]
    public void Modifiers_RenderInFixedOrderWithoutDuplicates()
    {
        CSharp.Class("Example").PrimaryParameter("int", "value", p =>
        {
            p.Params().In().Out().Ref().This();
            OutputAssert.Equal("this ref out in params int value", p.ToString());
            p.This().Ref().Out().In().Params();
            OutputAssert.Equal("this ref out in params int value", p.ToString());
        });
    }

    [Fact]
    public void This_CanCombineWithRefOrIn()
    {
        CSharp.Class("Example")
            .PrimaryParameter("int", "value", p => OutputAssert.Equal("this ref int value", p.Ref().This().ToString()))
            .PrimaryParameter("int", "value", p => OutputAssert.Equal("this in int value", p.In().This().ToString()));
    }

    [Fact]
    public void Default_LastCallWins()
    {
        CSharp.Class("Example").PrimaryParameter("int", "count", p =>
        {
            p.Default("0");
            OutputAssert.Equal("int count = 0", p.ToString());
            p.Default("42");
            OutputAssert.Equal("int count = 42", p.ToString());
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("not an expression")]
    [InlineData(" global::Example.Value ")]
    public void Default_PreservesUnvalidatedExpression(string expression)
    {
        CSharp.Class("Example").PrimaryParameter("int", "value", p =>
            OutputAssert.Equal("int value = " + expression, p.Default(expression).ToString()));
    }

    [Theory]
    [InlineData("class", "@class")]
    [InlineData("@class", "@class")]
    public void ToString_EscapesKeywordName(string name, string expected)
    {
        var builder = CSharp.Class("Example").PrimaryParameter("int", name, p =>
            OutputAssert.Equal("int " + expected, p.ToString()));

        OutputAssert.Equal("class Example(int " + expected + ")\n{\n}\n", builder.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a type")]
    [InlineData(" global::System.Collections.Generic.List<int[]> ")]
    public void ToString_PreservesUnvalidatedType(string type)
    {
        CSharp.Class("Example").PrimaryParameter(type, "value", p =>
            OutputAssert.Equal(type + " value", p.ToString()));
    }

    [Fact]
    public void ClassPrimaryParameters_PreserveOrderAndCompile()
    {
        var builder = CSharp.Class("Service").Public()
            .PrimaryParameter("string", "name")
            .PrimaryParameter("int", "retries", p => p.Default("3"));

        OutputAssert.Equal("public class Service(string name, int retries = 3)\n{\n}\n", builder.ToString());
        CompilationAssert.NoErrors(CSharp.File().Add(builder).ToString());
    }

    [Fact]
    public void StructPrimaryParameters_PreserveOrderAndCompile()
    {
        var builder = CSharp.Struct("Point").Public().ReadOnly()
            .PrimaryParameter("int", "x", _ => { })
            .PrimaryParameter("int", "y");

        OutputAssert.Equal("public readonly struct Point(int x, int y)\n{\n}\n", builder.ToString());
        CompilationAssert.NoErrors(CSharp.File().Add(builder).ToString());
    }

    [Fact]
    public void PrimaryParameters_RenderAfterTypeParametersBeforeBaseTypesAndConstraints()
    {
        var file = CSharp.File().Class("Base")
            .Class("Box", c => c.BaseType("Base").PrimaryParameter("T", "value")
                .PrimaryParameter("int", "count", p => p.Default("0"))
                .TypeParameter("T", t => t.Constraint("class")));

        OutputAssert.Equal("class Base\n{\n}\n\nclass Box<T>(T value, int count = 0) : Base where T : class\n{\n}\n", file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }

    [Fact]
    public void GenericPrimaryConstructor_CompilesWithBaseClassInSameFile()
    {
        var file = CSharp.File().Class("Base")
            .Class("Box", c => c.TypeParameter("T", t => t.Constraint("class"))
                .PrimaryParameter("T", "value").BaseType("Base"));

        OutputAssert.Equal("class Base\n{\n}\n\nclass Box<T>(T value) : Base where T : class\n{\n}\n", file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }

    [Fact]
    public void GenericStructPrimaryParameters_RenderBeforeInterfacesAndConstraints()
    {
        var file = CSharp.File().Interface("IMarker")
            .Struct("Box", s => s.BaseType("IMarker").PrimaryParameter("T", "value")
                .TypeParameter("T", t => t.Constraint("class")));

        OutputAssert.Equal("interface IMarker\n{\n}\n\nstruct Box<T>(T value) : IMarker where T : class\n{\n}\n", file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }

    [Fact]
    public void ParamsPrimaryParameters_CompileForClassesAndStructs()
    {
        var file = CSharp.File()
            .Class("Values", c => c.PrimaryParameter("int[]", "values", p => p.Params()))
            .Struct("Numbers", s => s.PrimaryParameter("int[]", "values", p => p.Params()));

        OutputAssert.Equal("class Values(params int[] values)\n{\n}\n\nstruct Numbers(params int[] values)\n{\n}\n", file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }

    [Fact]
    public void NoPrimaryParameters_OmitsParentheses()
    {
        OutputAssert.Equal("class Example\n{\n}\n", CSharp.Class("Example").ToString());
        OutputAssert.Equal("struct Example\n{\n}\n", CSharp.Struct("Example").ToString());
    }

    [Fact]
    public void PrimaryParameter_ConfiguresImmediatelyOnceAndRetainsBuilder()
    {
        ParameterBuilder? parameter = null;
        var calls = 0;
        var builder = CSharp.Class("Example").PrimaryParameter("int", "value", p =>
        {
            parameter = p;
            calls++;
        });

        Assert.NotNull(parameter);
        Assert.Equal(1, calls);
        OutputAssert.Equal("class Example(int value)\n{\n}\n", builder.ToString());
        parameter.Default("7");
        OutputAssert.Equal("class Example(int value = 7)\n{\n}\n", builder.ToString());
        OutputAssert.Equal("class Example(int value = 7)\n{\n}\n", builder.ToString());
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1value")]
    [InlineData("a.b")]
    [InlineData("a b")]
    [InlineData("a-b")]
    [InlineData("@@value")]
    public void InvalidNames_ThrowAtCallTimeBeforeConfiguringOrAdding(string name)
    {
        var classBuilder = CSharp.Class("Example");
        var structBuilder = CSharp.Struct("Example");
        var configured = false;

        Assert.Throws<ArgumentException>("name", () => classBuilder.PrimaryParameter("int", name));
        Assert.Throws<ArgumentException>("name", () => classBuilder.PrimaryParameter("int", name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => structBuilder.PrimaryParameter("int", name));
        Assert.Throws<ArgumentException>("name", () => structBuilder.PrimaryParameter("int", name, _ => configured = true));
        Assert.False(configured);
        OutputAssert.Equal("class Example\n{\n}\n", classBuilder.ToString());
        OutputAssert.Equal("struct Example\n{\n}\n", structBuilder.ToString());
    }

    [Fact]
    public void NullArguments_ThrowBeforeConfiguringOrAdding()
    {
        var classBuilder = CSharp.Class("Example");
        var structBuilder = CSharp.Struct("Example");
        var configured = false;

        Assert.Throws<ArgumentNullException>("type", () => classBuilder.PrimaryParameter(null!, "value"));
        Assert.Throws<ArgumentNullException>("type", () => classBuilder.PrimaryParameter(null!, "value", _ => configured = true));
        Assert.Throws<ArgumentNullException>("name", () => classBuilder.PrimaryParameter("int", null!));
        Assert.Throws<ArgumentNullException>("name", () => classBuilder.PrimaryParameter("int", null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => classBuilder.PrimaryParameter("int", "value", null!));
        Assert.Throws<ArgumentNullException>("type", () => structBuilder.PrimaryParameter(null!, "value"));
        Assert.Throws<ArgumentNullException>("type", () => structBuilder.PrimaryParameter(null!, "value", _ => configured = true));
        Assert.Throws<ArgumentNullException>("name", () => structBuilder.PrimaryParameter("int", null!));
        Assert.Throws<ArgumentNullException>("name", () => structBuilder.PrimaryParameter("int", null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => structBuilder.PrimaryParameter("int", "value", null!));
        Assert.False(configured);
        OutputAssert.Equal("class Example\n{\n}\n", classBuilder.ToString());
        OutputAssert.Equal("struct Example\n{\n}\n", structBuilder.ToString());
    }

    [Fact]
    public void Default_RejectsNullAndPreservesPreviousValue()
    {
        CSharp.Class("Example").PrimaryParameter("int", "value", p =>
        {
            p.Default("3");
            Assert.Throws<ArgumentNullException>("expression", () => p.Default(null!));
            OutputAssert.Equal("int value = 3", p.ToString());
        });
    }

    [Fact]
    public void FluentMethods_ReturnSameBuilder()
    {
        var classBuilder = CSharp.Class("Example");
        var structBuilder = CSharp.Struct("Example");

        Assert.Same(classBuilder, classBuilder.PrimaryParameter("int", "x"));
        Assert.Same(classBuilder, classBuilder.PrimaryParameter("int", "y", _ => { }));
        Assert.Same(structBuilder, structBuilder.PrimaryParameter("int", "x"));
        Assert.Same(structBuilder, structBuilder.PrimaryParameter("int", "y", p =>
        {
            Assert.Same(p, p.Ref());
            Assert.Same(p, p.Out());
            Assert.Same(p, p.In());
            Assert.Same(p, p.Params());
            Assert.Same(p, p.This());
            Assert.Same(p, p.Default("0"));
        }));
    }

    [Fact]
    public void InterfaceBuilder_DoesNotExposePrimaryParameter()
    {
        Assert.DoesNotContain(typeof(InterfaceBuilder).GetMethods(), method => method.Name == "PrimaryParameter");
    }
}
