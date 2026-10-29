using System;
using System.Collections.Generic;
using System.Text;
using Codrio.Rendering;
using Codrio.Tests.Fixtures;
using Xunit;

namespace Codrio.Tests;

public class TypeNameTests
{
    [Theory]
    [InlineData(typeof(bool), "bool")]
    [InlineData(typeof(byte), "byte")]
    [InlineData(typeof(sbyte), "sbyte")]
    [InlineData(typeof(char), "char")]
    [InlineData(typeof(decimal), "decimal")]
    [InlineData(typeof(double), "double")]
    [InlineData(typeof(float), "float")]
    [InlineData(typeof(int), "int")]
    [InlineData(typeof(uint), "uint")]
    [InlineData(typeof(long), "long")]
    [InlineData(typeof(ulong), "ulong")]
    [InlineData(typeof(short), "short")]
    [InlineData(typeof(ushort), "ushort")]
    [InlineData(typeof(object), "object")]
    [InlineData(typeof(string), "string")]
    [InlineData(typeof(void), "void")]
    [InlineData(typeof(IntPtr), "global::System.IntPtr")]
    [InlineData(typeof(UIntPtr), "global::System.UIntPtr")]
    [InlineData(typeof(DateTime), "global::System.DateTime")]
    [InlineData(typeof(StringBuilder), "global::System.Text.StringBuilder")]
    [InlineData(typeof(Dictionary<string, int>), "global::System.Collections.Generic.Dictionary<string, int>")]
    [InlineData(typeof(List<List<int>>), "global::System.Collections.Generic.List<global::System.Collections.Generic.List<int>>")]
    [InlineData(typeof(Dictionary<string, int[]>), "global::System.Collections.Generic.Dictionary<string, int[]>")]
    [InlineData(typeof(int?), "global::System.Nullable<int>")]
    [InlineData(typeof(ValueTuple<int, string>), "global::System.ValueTuple<int, string>")]
    [InlineData(typeof(Environment.SpecialFolder), "global::System.Environment.SpecialFolder")]
    [InlineData(typeof(Dictionary<string, int>.KeyCollection), "global::System.Collections.Generic.Dictionary<string, int>.KeyCollection")]
    [InlineData(typeof(Outer<int>.Inner<string>), "global::Codrio.Tests.Fixtures.Outer<int>.Inner<string>")]
    [InlineData(typeof(Outer<int>.Leaf), "global::Codrio.Tests.Fixtures.Outer<int>.Leaf")]
    [InlineData(typeof(int[]), "int[]")]
    [InlineData(typeof(string[,,]), "string[,,]")]
    [InlineData(typeof(int[][,]), "int[][,]")]
    [InlineData(typeof(int[,][]), "int[,][]")]
    [InlineData(typeof(List<int>[]), "global::System.Collections.Generic.List<int>[]")]
    [InlineData(typeof(Outer<int>.Inner<string>[]), "global::Codrio.Tests.Fixtures.Outer<int>.Inner<string>[]")]
    [InlineData(typeof(@class), "global::Codrio.Tests.Fixtures.@class")]
    [InlineData(typeof(Fixtures.@event.Handler), "global::Codrio.Tests.Fixtures.@event.Handler")]
    [InlineData(typeof(CodrioGlobalFixture), "global::CodrioGlobalFixture")]
    public void Render_WritesCSharpTypeName(Type type, string expected)
    {
        OutputAssert.Equal(expected, TypeName.Render(type, "type"));
    }

    [Fact]
    public void Render_WritesPointersAndPointerArrays()
    {
        var pointer = typeof(int).MakePointerType();

        OutputAssert.Equal("int*", TypeName.Render(pointer, "type"));
        OutputAssert.Equal("int*[]", TypeName.Render(pointer.MakeArrayType(), "type"));
    }

    [Theory]
    [InlineData("type")]
    [InlineData("returnType")]
    public void Render_RejectsNullWithParameterName(string paramName)
    {
        Assert.Throws<ArgumentNullException>(paramName, () => TypeName.Render(null!, paramName));
    }

    [Theory]
    [InlineData("type")]
    [InlineData("returnType")]
    public void Render_RejectsTypesWithoutCSharpNames(string paramName)
    {
        var invalidTypes = new[]
        {
            typeof(List<>),
            typeof(List<>).GetGenericArguments()[0],
            typeof(Dictionary<,>).MakeGenericType(typeof(string), typeof(Dictionary<,>).GetGenericArguments()[1]),
            typeof(List<>).MakeArrayType(),
            typeof(int).MakeByRefType(),
            typeof(int).MakeArrayType(1),
            new { A = 1 }.GetType()
        };

        foreach (var type in invalidTypes)
        {
            Assert.Throws<ArgumentException>(paramName, () => TypeName.Render(type, paramName));
        }
    }
}
