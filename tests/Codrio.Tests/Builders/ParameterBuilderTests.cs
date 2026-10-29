using System;
using Xunit;

namespace Codrio.Tests;

public class ParameterBuilderTests
{
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
