using System;
using System.Collections.Generic;
using Codrio.Tests.Fixtures;
using Xunit;

namespace Codrio.Tests;

public class ParameterTypeOverloadTests
{
    [Fact]
    public void MethodParameters_AllOverloadsPreserveOrderAndReturnSameBuilder()
    {
        var builder = CSharp.Method("Run").Parameter("bool", "first");
        var typeCalls = 0;
        var genericCalls = 0;

        Assert.Same(builder, builder.Parameter(typeof(int), "class"));
        Assert.Same(builder, builder.Parameter(typeof(string), "text", p =>
        {
            typeCalls++;
            p.Default("\"\"");
        }));
        Assert.Equal(1, typeCalls);
        Assert.Same(builder, builder.Parameter<Outer<int>.Inner<string>>("nested"));
        Assert.Same(builder, builder.Parameter<int[]>("values", p =>
        {
            genericCalls++;
            p.Params();
        }));
        Assert.Equal(1, genericCalls);

        OutputAssert.Equal("void Run(bool first, int @class, string text = \"\", global::Codrio.Tests.Fixtures.Outer<int>.Inner<string> nested, params int[] values);\n", builder.ToString());
        Assert.Equal(1, typeCalls);
        Assert.Equal(1, genericCalls);
    }

    [Fact]
    public void MethodParameters_StringTypeAndGenericOverloadsRenderSameName()
    {
        const string typeName = "global::System.Collections.Generic.Dictionary<string, int>";
        const string expected = "void Run(global::System.Collections.Generic.Dictionary<string, int> map);\n";

        OutputAssert.Equal(expected, CSharp.Method("Run").Parameter(typeName, "map").ToString());
        OutputAssert.Equal(expected, CSharp.Method("Run").Parameter(typeof(Dictionary<string, int>), "map").ToString());
        OutputAssert.Equal(expected, CSharp.Method("Run").Parameter<Dictionary<string, int>>("map").ToString());
        OutputAssert.Equal(expected, CSharp.Method("Run").Parameter(typeName, "map", _ => { }).ToString());
        OutputAssert.Equal(expected, CSharp.Method("Run").Parameter(typeof(Dictionary<string, int>), "map", _ => { }).ToString());
        OutputAssert.Equal(expected, CSharp.Method("Run").Parameter<Dictionary<string, int>>("map", _ => { }).ToString());
        OutputAssert.Equal("void Run(global::Codrio.Tests.Fixtures.Outer<int>.Inner<string> @class);\n",
            CSharp.Method("Run").Parameter(typeof(Outer<int>.Inner<string>), "class").ToString());
    }

    [Fact]
    public void MethodParameters_TypeOverloadsCombineWithParameterConfiguration()
    {
        var builder = CSharp.Method("Run")
            .Parameter(typeof(string), "receiver", p => p.This())
            .Parameter(typeof(int), "changed", p => p.Ref())
            .Parameter(typeof(int), "result", p => p.Out())
            .Parameter(typeof(int), "input", p => p.In())
            .Parameter(typeof(int), "count", p => p.Attribute("Count").Default("1"))
            .Parameter(typeof(int[]), "values", p => p.Params());

        OutputAssert.Equal("void Run(this string receiver, ref int changed, out int result, in int input, [Count] int count = 1, params int[] values);\n", builder.ToString());
    }

    [Fact]
    public void MethodParameters_InvalidTypesThrowBeforeNameValidationConfigurationOrAdding()
    {
        var builder = CSharp.Method("Run").Parameter("int", "existing");
        var configured = false;

        foreach (var type in new[] { typeof(List<>), typeof(int).MakeByRefType() })
        {
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "value"));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "value", _ => configured = true));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "1x"));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "1x", _ => configured = true));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, null!, null!));
            Assert.False(configured);
            OutputAssert.Equal("void Run(int existing);\n", builder.ToString());
        }
    }

    [Fact]
    public void MethodParameters_InvalidNamesAndNullArgumentsPreserveParameters()
    {
        var builder = CSharp.Method("Run").Parameter("int", "existing");
        var configured = false;

        Assert.Throws<ArgumentException>("name", () => builder.Parameter(typeof(int), "1x"));
        Assert.Throws<ArgumentException>("name", () => builder.Parameter(typeof(int), "1x", _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => builder.Parameter<int>("1x"));
        Assert.Throws<ArgumentException>("name", () => builder.Parameter<int>("1x", _ => configured = true));
        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter((Type)null!, "value"));
        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter((Type)null!, "value", _ => configured = true));
        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter((Type)null!, null!, null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter(typeof(int), null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter(typeof(int), null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Parameter(typeof(int), "value", null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter<int>(null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter<int>(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Parameter<int>("value", null!));
        Assert.False(configured);
        OutputAssert.Equal("void Run(int existing);\n", builder.ToString());
    }

    [Fact]
    public void ConstructorParameters_AllOverloadsPreserveOrderAndReturnSameBuilder()
    {
        var builder = CSharp.Constructor().Parameter("bool", "first");
        var typeCalls = 0;
        var genericCalls = 0;

        Assert.Same(builder, builder.Parameter(typeof(int), "class"));
        Assert.Same(builder, builder.Parameter(typeof(string), "text", p =>
        {
            typeCalls++;
            p.Default("\"\"");
        }));
        Assert.Equal(1, typeCalls);
        Assert.Same(builder, builder.Parameter<Outer<int>.Inner<string>>("nested"));
        Assert.Same(builder, builder.Parameter<int[]>("values", p =>
        {
            genericCalls++;
            p.Params();
        }));
        Assert.Equal(1, genericCalls);

        OutputAssert.Equal("(bool first, int @class, string text = \"\", global::Codrio.Tests.Fixtures.Outer<int>.Inner<string> nested, params int[] values)\n{\n}\n", builder.ToString());
        Assert.Equal(1, typeCalls);
        Assert.Equal(1, genericCalls);
    }

    [Fact]
    public void ConstructorParameters_StringTypeAndGenericOverloadsRenderSameName()
    {
        const string typeName = "global::System.Collections.Generic.Dictionary<string, int>";
        const string expected = "(global::System.Collections.Generic.Dictionary<string, int> map)\n{\n}\n";

        OutputAssert.Equal(expected, CSharp.Constructor().Parameter(typeName, "map").ToString());
        OutputAssert.Equal(expected, CSharp.Constructor().Parameter(typeof(Dictionary<string, int>), "map").ToString());
        OutputAssert.Equal(expected, CSharp.Constructor().Parameter<Dictionary<string, int>>("map").ToString());
        OutputAssert.Equal(expected, CSharp.Constructor().Parameter(typeName, "map", _ => { }).ToString());
        OutputAssert.Equal(expected, CSharp.Constructor().Parameter(typeof(Dictionary<string, int>), "map", _ => { }).ToString());
        OutputAssert.Equal(expected, CSharp.Constructor().Parameter<Dictionary<string, int>>("map", _ => { }).ToString());
        OutputAssert.Equal("(global::Codrio.Tests.Fixtures.Outer<int>.Inner<string> @class)\n{\n}\n",
            CSharp.Constructor().Parameter(typeof(Outer<int>.Inner<string>), "class").ToString());
    }

    [Fact]
    public void ConstructorParameters_TypeOverloadsCombineWithParameterConfiguration()
    {
        var builder = CSharp.Constructor()
            .Parameter(typeof(string), "receiver", p => p.This())
            .Parameter(typeof(int), "changed", p => p.Ref())
            .Parameter(typeof(int), "result", p => p.Out())
            .Parameter(typeof(int), "input", p => p.In())
            .Parameter(typeof(int), "count", p => p.Attribute("Count").Default("1"))
            .Parameter(typeof(int[]), "values", p => p.Params());

        OutputAssert.Equal("(this string receiver, ref int changed, out int result, in int input, [Count] int count = 1, params int[] values)\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void ConstructorParameters_InvalidTypesThrowBeforeNameValidationConfigurationOrAdding()
    {
        var builder = CSharp.Constructor().Parameter("int", "existing");
        var configured = false;

        foreach (var type in new[] { typeof(List<>), typeof(int).MakeByRefType() })
        {
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "value"));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "value", _ => configured = true));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "1x"));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "1x", _ => configured = true));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, null!, null!));
            Assert.False(configured);
            OutputAssert.Equal("(int existing)\n{\n}\n", builder.ToString());
        }
    }

    [Fact]
    public void ConstructorParameters_InvalidNamesAndNullArgumentsPreserveParameters()
    {
        var builder = CSharp.Constructor().Parameter("int", "existing");
        var configured = false;

        Assert.Throws<ArgumentException>("name", () => builder.Parameter(typeof(int), "1x"));
        Assert.Throws<ArgumentException>("name", () => builder.Parameter(typeof(int), "1x", _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => builder.Parameter<int>("1x"));
        Assert.Throws<ArgumentException>("name", () => builder.Parameter<int>("1x", _ => configured = true));
        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter((Type)null!, "value"));
        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter((Type)null!, "value", _ => configured = true));
        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter((Type)null!, null!, null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter(typeof(int), null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter(typeof(int), null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Parameter(typeof(int), "value", null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter<int>(null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter<int>(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Parameter<int>("value", null!));
        Assert.False(configured);
        OutputAssert.Equal("(int existing)\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void DelegateParameters_AllOverloadsPreserveOrderAndReturnSameBuilder()
    {
        var builder = CSharp.Delegate("Handler").Parameter("bool", "first");
        var typeCalls = 0;
        var genericCalls = 0;

        Assert.Same(builder, builder.Parameter(typeof(int), "class"));
        Assert.Same(builder, builder.Parameter(typeof(string), "text", p =>
        {
            typeCalls++;
            p.Default("\"\"");
        }));
        Assert.Equal(1, typeCalls);
        Assert.Same(builder, builder.Parameter<Outer<int>.Inner<string>>("nested"));
        Assert.Same(builder, builder.Parameter<int[]>("values", p =>
        {
            genericCalls++;
            p.Params();
        }));
        Assert.Equal(1, genericCalls);

        OutputAssert.Equal("delegate void Handler(bool first, int @class, string text = \"\", global::Codrio.Tests.Fixtures.Outer<int>.Inner<string> nested, params int[] values);\n", builder.ToString());
        Assert.Equal(1, typeCalls);
        Assert.Equal(1, genericCalls);
    }

    [Fact]
    public void DelegateParameters_StringTypeAndGenericOverloadsRenderSameName()
    {
        const string typeName = "global::System.Collections.Generic.Dictionary<string, int>";
        const string expected = "delegate void Handler(global::System.Collections.Generic.Dictionary<string, int> map);\n";

        OutputAssert.Equal(expected, CSharp.Delegate("Handler").Parameter(typeName, "map").ToString());
        OutputAssert.Equal(expected, CSharp.Delegate("Handler").Parameter(typeof(Dictionary<string, int>), "map").ToString());
        OutputAssert.Equal(expected, CSharp.Delegate("Handler").Parameter<Dictionary<string, int>>("map").ToString());
        OutputAssert.Equal(expected, CSharp.Delegate("Handler").Parameter(typeName, "map", _ => { }).ToString());
        OutputAssert.Equal(expected, CSharp.Delegate("Handler").Parameter(typeof(Dictionary<string, int>), "map", _ => { }).ToString());
        OutputAssert.Equal(expected, CSharp.Delegate("Handler").Parameter<Dictionary<string, int>>("map", _ => { }).ToString());
        OutputAssert.Equal("delegate void Handler(global::Codrio.Tests.Fixtures.Outer<int>.Inner<string> @class);\n",
            CSharp.Delegate("Handler").Parameter(typeof(Outer<int>.Inner<string>), "class").ToString());
    }

    [Fact]
    public void DelegateParameters_TypeOverloadsCombineWithParameterConfiguration()
    {
        var builder = CSharp.Delegate("Handler")
            .Parameter(typeof(string), "receiver", p => p.This())
            .Parameter(typeof(int), "changed", p => p.Ref())
            .Parameter(typeof(int), "result", p => p.Out())
            .Parameter(typeof(int), "input", p => p.In())
            .Parameter(typeof(int), "count", p => p.Attribute("Count").Default("1"))
            .Parameter(typeof(int[]), "values", p => p.Params());

        OutputAssert.Equal("delegate void Handler(this string receiver, ref int changed, out int result, in int input, [Count] int count = 1, params int[] values);\n", builder.ToString());
    }

    [Fact]
    public void DelegateParameters_InvalidTypesThrowBeforeNameValidationConfigurationOrAdding()
    {
        var builder = CSharp.Delegate("Handler").Parameter("int", "existing");
        var configured = false;

        foreach (var type in new[] { typeof(List<>), typeof(int).MakeByRefType() })
        {
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "value"));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "value", _ => configured = true));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "1x"));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "1x", _ => configured = true));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, null!, null!));
            Assert.False(configured);
            OutputAssert.Equal("delegate void Handler(int existing);\n", builder.ToString());
        }
    }

    [Fact]
    public void DelegateParameters_InvalidNamesAndNullArgumentsPreserveParameters()
    {
        var builder = CSharp.Delegate("Handler").Parameter("int", "existing");
        var configured = false;

        Assert.Throws<ArgumentException>("name", () => builder.Parameter(typeof(int), "1x"));
        Assert.Throws<ArgumentException>("name", () => builder.Parameter(typeof(int), "1x", _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => builder.Parameter<int>("1x"));
        Assert.Throws<ArgumentException>("name", () => builder.Parameter<int>("1x", _ => configured = true));
        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter((Type)null!, "value"));
        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter((Type)null!, "value", _ => configured = true));
        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter((Type)null!, null!, null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter(typeof(int), null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter(typeof(int), null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Parameter(typeof(int), "value", null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter<int>(null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter<int>(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Parameter<int>("value", null!));
        Assert.False(configured);
        OutputAssert.Equal("delegate void Handler(int existing);\n", builder.ToString());
    }

    [Fact]
    public void CompleteFile_RendersParametersAndCompiles()
    {
        var file = CSharp.File().Using("System").Namespace("App", n => n
            .Class("Extensions", c => c.Static()
                .Method("Log", m => m.Public().Static()
                    .Parameter(typeof(string), "message", p => p.This())
                    .Parameter<int[]>("codes", p => p.Params())
                    .Body()))
            .Class("Sample", c => c
                .Constructor(k => k.Public()
                    .Parameter(typeof(int), "result", p => p.Out())
                    .Parameter<int>("changed", p => p.Ref())
                    .Parameter(typeof(int), "input", p => p.In())
                    .Parameter<Dictionary<string, int>>("map")
                    .Parameter(typeof(Outer<int>.Inner<string>), "nested")
                    .Parameter<int[]>("values", p => p.Params())
                    .Body(b => b.Line("result = input;")))
                .Method("Run", m => m.Public()
                    .Parameter<int>("result", p => p.Out())
                    .Parameter(typeof(int), "changed", p => p.Ref())
                    .Parameter<int>("input", p => p.In())
                    .Parameter(typeof(Dictionary<string, int>), "map")
                    .Parameter<Outer<int>.Inner<string>>("nested")
                    .Parameter(typeof(int[]), "values", p => p.Params())
                    .Body(b => b.Line("result = input;"))))
            .Delegate("Handler", d => d
                .Parameter<object>("sender")
                .Parameter(typeof(EventArgs), "e")
                .Parameter<int>("count", p => p.In())
                .Parameter(typeof(int[]), "values", p => p.Params())));

        const string expected = """
            using System;

            namespace App
            {
                static class Extensions
                {
                    public static void Log(this string message, params int[] codes)
                    {
                    }
                }

                class Sample
                {
                    public Sample(out int result, ref int changed, in int input, global::System.Collections.Generic.Dictionary<string, int> map, global::Codrio.Tests.Fixtures.Outer<int>.Inner<string> nested, params int[] values)
                    {
                        result = input;
                    }

                    public void Run(out int result, ref int changed, in int input, global::System.Collections.Generic.Dictionary<string, int> map, global::Codrio.Tests.Fixtures.Outer<int>.Inner<string> nested, params int[] values)
                    {
                        result = input;
                    }
                }

                delegate void Handler(object sender, global::System.EventArgs e, in int count, params int[] values);
            }
            """;

        OutputAssert.Equal(expected + "\n", file.ToString());
        CompilationAssert.NoErrors(file.ToString() + "\n" + TypeFixtures.Source);
    }
}
