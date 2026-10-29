using System;
using System.Collections.Generic;
using Codrio.Tests.Fixtures;
using Xunit;

namespace Codrio.Tests;

public class ReturnTypeOverloadTests
{
    [Fact]
    public void MethodReturns_RendersKeywordAndVoid()
    {
        OutputAssert.Equal("int Run();\n", CSharp.Method("Run").Returns<int>().ToString());
        OutputAssert.Equal("void Run();\n", CSharp.Method("Run").Returns(typeof(void)).ToString());
    }

    [Fact]
    public void DelegateReturns_RendersKeywordAndVoid()
    {
        OutputAssert.Equal("delegate int Factory();\n", CSharp.Delegate("Factory").Returns<int>().ToString());
        OutputAssert.Equal("delegate void Factory();\n", CSharp.Delegate("Factory").Returns(typeof(void)).ToString());
    }

    [Fact]
    public void MethodReturns_AllOverloadsRenderSameGenericType()
    {
        const string expected = "global::System.Collections.Generic.Dictionary<string, int> Run();\n";

        OutputAssert.Equal(expected, CSharp.Method("Run").Returns<Dictionary<string, int>>().ToString());
        OutputAssert.Equal(expected, CSharp.Method("Run").Returns(typeof(Dictionary<string, int>)).ToString());
        OutputAssert.Equal(expected, CSharp.Method("Run").Returns("global::System.Collections.Generic.Dictionary<string, int>").ToString());
    }

    [Fact]
    public void DelegateReturns_AllOverloadsRenderSameGenericType()
    {
        const string expected = "delegate global::System.Collections.Generic.Dictionary<string, int> Factory();\n";

        OutputAssert.Equal(expected, CSharp.Delegate("Factory").Returns<Dictionary<string, int>>().ToString());
        OutputAssert.Equal(expected, CSharp.Delegate("Factory").Returns(typeof(Dictionary<string, int>)).ToString());
        OutputAssert.Equal(expected, CSharp.Delegate("Factory").Returns("global::System.Collections.Generic.Dictionary<string, int>").ToString());
    }

    [Fact]
    public void MethodReturns_MixedOverloadsUseLastCallAndReturnSameBuilder()
    {
        var builder = CSharp.Method("Run").Returns("bool");

        Assert.Same(builder, builder.Returns(typeof(string)));
        OutputAssert.Equal("string Run();\n", builder.ToString());
        Assert.Same(builder, builder.Returns<int>());
        OutputAssert.Equal("int Run();\n", builder.ToString());
        Assert.Same(builder, builder.Returns("long"));
        OutputAssert.Equal("long Run();\n", builder.ToString());
    }

    [Fact]
    public void DelegateReturns_MixedOverloadsUseLastCallAndReturnSameBuilder()
    {
        var builder = CSharp.Delegate("Factory").Returns("bool");

        Assert.Same(builder, builder.Returns(typeof(string)));
        OutputAssert.Equal("delegate string Factory();\n", builder.ToString());
        Assert.Same(builder, builder.Returns<int>());
        OutputAssert.Equal("delegate int Factory();\n", builder.ToString());
        Assert.Same(builder, builder.Returns("long"));
        OutputAssert.Equal("delegate long Factory();\n", builder.ToString());
    }

    [Fact]
    public void MethodReturns_InvalidTypeThrowsAtCallTimeAndPreservesReturnType()
    {
        var builder = CSharp.Method("Run").Returns("int");

        Assert.Throws<ArgumentException>("type", () => builder.Returns(typeof(List<>)));
        OutputAssert.Equal("int Run();\n", builder.ToString());
        Assert.Throws<ArgumentNullException>("type", () => builder.Returns((Type)null!));
        OutputAssert.Equal("int Run();\n", builder.ToString());
    }

    [Fact]
    public void DelegateReturns_InvalidTypeThrowsAtCallTimeAndPreservesReturnType()
    {
        var builder = CSharp.Delegate("Factory").Returns("int");

        Assert.Throws<ArgumentException>("type", () => builder.Returns(typeof(List<>)));
        OutputAssert.Equal("delegate int Factory();\n", builder.ToString());
        Assert.Throws<ArgumentNullException>("type", () => builder.Returns((Type)null!));
        OutputAssert.Equal("delegate int Factory();\n", builder.ToString());
    }

    [Fact]
    public void GenericReturns_AnonymousTypeThrowsWithTypeParameterNameAndPreservesReturnType()
    {
        AssertInvalidGenericReturns(new { A = 1 });
    }

    private static void AssertInvalidGenericReturns<T>(T value)
    {
        var method = CSharp.Method("Run").Returns("int");
        var delegateBuilder = CSharp.Delegate("Factory").Returns("int");

        Assert.Throws<ArgumentException>("type", () => method.Returns<T>());
        Assert.Throws<ArgumentException>("type", () => delegateBuilder.Returns<T>());
        OutputAssert.Equal("int Run();\n", method.ToString());
        OutputAssert.Equal("delegate int Factory();\n", delegateBuilder.ToString());
    }

    [Fact]
    public void CompleteFile_RendersReturnTypesAndCompiles()
    {
        var file = CSharp.File().Using("System").Namespace("App", n => n
            .Interface("IReturns", i => i
                .Method("Keyword", m => m.Returns<int>())
                .Method("Nothing", m => m.Returns(typeof(void)))
                .Method("Generic", m => m.Returns<Dictionary<string, int>>())
                .Method("Nullable", m => m.Returns(typeof(int?)))
                .Method("Nested", m => m.Returns<Environment.SpecialFolder>())
                .Method("Collection", m => m.Returns(typeof(Dictionary<string, int>.KeyCollection)))
                .Method("Inner", m => m.Returns<Outer<int>.Inner<string>>())
                .Method("Leaf", m => m.Returns(typeof(Outer<int>.Leaf)))
                .Method("Array", m => m.Returns<int[]>())
                .Method("Multidimensional", m => m.Returns(typeof(string[,,])))
                .Method("Jagged", m => m.Returns<int[][,]>())
                .Method("Mixed", m => m.Returns(typeof(int[,][])))
                .Method("NestedArray", m => m.Returns<Outer<int>.Inner<string>[]>())
                .Method("KeywordName", m => m.Returns(typeof(@class)))
                .Method("KeywordNamespace", m => m.Returns<Fixtures.@event.Handler>())
                .Method("Global", m => m.Returns(typeof(CodrioGlobalFixture))))
            .Delegate("Factory", d => d.Returns<List<int>[]>()));

        const string expected = """
            using System;

            namespace App
            {
                interface IReturns
                {
                    int Keyword();

                    void Nothing();

                    global::System.Collections.Generic.Dictionary<string, int> Generic();

                    global::System.Nullable<int> Nullable();

                    global::System.Environment.SpecialFolder Nested();

                    global::System.Collections.Generic.Dictionary<string, int>.KeyCollection Collection();

                    global::Codrio.Tests.Fixtures.Outer<int>.Inner<string> Inner();

                    global::Codrio.Tests.Fixtures.Outer<int>.Leaf Leaf();

                    int[] Array();

                    string[,,] Multidimensional();

                    int[][,] Jagged();

                    int[,][] Mixed();

                    global::Codrio.Tests.Fixtures.Outer<int>.Inner<string>[] NestedArray();

                    global::Codrio.Tests.Fixtures.@class KeywordName();

                    global::Codrio.Tests.Fixtures.@event.Handler KeywordNamespace();

                    global::CodrioGlobalFixture Global();
                }

                delegate global::System.Collections.Generic.List<int>[] Factory();
            }
            """;

        OutputAssert.Equal(expected + "\n", file.ToString());
        CompilationAssert.NoErrors(file.ToString() + "\n" + TypeFixtures.Source);
    }
}
