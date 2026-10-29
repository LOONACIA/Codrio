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
    public void IndexerParameters_AllOverloadsPreserveOrderAndReturnSameBuilder()
    {
        var builder = CSharp.Indexer("string").Parameter("bool", "first");
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

        OutputAssert.Equal("string this[bool first, int @class, string text = \"\", global::Codrio.Tests.Fixtures.Outer<int>.Inner<string> nested, params int[] values] { get; set; }\n", builder.ToString());
        Assert.Equal(1, typeCalls);
        Assert.Equal(1, genericCalls);
    }

    [Fact]
    public void IndexerParameters_StringTypeAndGenericOverloadsRenderSameName()
    {
        const string typeName = "global::System.Collections.Generic.Dictionary<string, int>";
        const string expected = "string this[global::System.Collections.Generic.Dictionary<string, int> map] { get; set; }\n";

        OutputAssert.Equal(expected, CSharp.Indexer("string").Parameter(typeName, "map").ToString());
        OutputAssert.Equal(expected, CSharp.Indexer("string").Parameter(typeof(Dictionary<string, int>), "map").ToString());
        OutputAssert.Equal(expected, CSharp.Indexer("string").Parameter<Dictionary<string, int>>("map").ToString());
        OutputAssert.Equal(expected, CSharp.Indexer("string").Parameter(typeName, "map", _ => { }).ToString());
        OutputAssert.Equal(expected, CSharp.Indexer("string").Parameter(typeof(Dictionary<string, int>), "map", _ => { }).ToString());
        OutputAssert.Equal(expected, CSharp.Indexer("string").Parameter<Dictionary<string, int>>("map", _ => { }).ToString());
        OutputAssert.Equal("string this[global::Codrio.Tests.Fixtures.Outer<int>.Inner<string> @class] { get; set; }\n",
            CSharp.Indexer("string").Parameter(typeof(Outer<int>.Inner<string>), "class").ToString());
    }

    [Fact]
    public void IndexerParameters_TypeOverloadsCombineWithParameterConfiguration()
    {
        var builder = CSharp.Indexer("string")
            .Parameter(typeof(string), "receiver", p => p.This())
            .Parameter(typeof(int), "changed", p => p.Ref())
            .Parameter(typeof(int), "result", p => p.Out())
            .Parameter(typeof(int), "input", p => p.In())
            .Parameter(typeof(int), "count", p => p.Attribute("Count").Default("1"))
            .Parameter(typeof(int[]), "values", p => p.Params());

        OutputAssert.Equal("string this[this string receiver, ref int changed, out int result, in int input, [Count] int count = 1, params int[] values] { get; set; }\n", builder.ToString());
    }

    [Fact]
    public void IndexerParameters_InvalidTypesThrowBeforeNameValidationConfigurationOrAdding()
    {
        var builder = CSharp.Indexer("string").Parameter("int", "existing");
        var configured = false;

        foreach (var type in new[] { typeof(List<>), typeof(int).MakeByRefType() })
        {
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "value"));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "value", _ => configured = true));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "1x"));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "1x", _ => configured = true));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, null!, null!));
            Assert.False(configured);
            OutputAssert.Equal("string this[int existing] { get; set; }\n", builder.ToString());
        }
    }

    [Fact]
    public void IndexerParameters_InvalidNamesAndNullArgumentsPreserveParameters()
    {
        var builder = CSharp.Indexer("string").Parameter("int", "existing");
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
        OutputAssert.Equal("string this[int existing] { get; set; }\n", builder.ToString());
    }

    [Fact]
    public void OperatorParameters_AllOverloadsPreserveOrderAndReturnSameBuilder()
    {
        var builder = CSharp.Operator("Money", "+").Public().Static().Arrow("left").Parameter("bool", "first");
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

        OutputAssert.Equal("public static Money operator +(bool first, int @class, string text = \"\", global::Codrio.Tests.Fixtures.Outer<int>.Inner<string> nested, params int[] values) => left;\n", builder.ToString());
        Assert.Equal(1, typeCalls);
        Assert.Equal(1, genericCalls);
    }

    [Fact]
    public void OperatorParameters_StringTypeAndGenericOverloadsRenderSameName()
    {
        const string typeName = "global::System.Collections.Generic.Dictionary<string, int>";
        const string expected = "public static Money operator +(global::System.Collections.Generic.Dictionary<string, int> map) => left;\n";

        OutputAssert.Equal(expected, CSharp.Operator("Money", "+").Public().Static().Arrow("left").Parameter(typeName, "map").ToString());
        OutputAssert.Equal(expected, CSharp.Operator("Money", "+").Public().Static().Arrow("left").Parameter(typeof(Dictionary<string, int>), "map").ToString());
        OutputAssert.Equal(expected, CSharp.Operator("Money", "+").Public().Static().Arrow("left").Parameter<Dictionary<string, int>>("map").ToString());
        OutputAssert.Equal(expected, CSharp.Operator("Money", "+").Public().Static().Arrow("left").Parameter(typeName, "map", _ => { }).ToString());
        OutputAssert.Equal(expected, CSharp.Operator("Money", "+").Public().Static().Arrow("left").Parameter(typeof(Dictionary<string, int>), "map", _ => { }).ToString());
        OutputAssert.Equal(expected, CSharp.Operator("Money", "+").Public().Static().Arrow("left").Parameter<Dictionary<string, int>>("map", _ => { }).ToString());
        OutputAssert.Equal("public static Money operator +(global::Codrio.Tests.Fixtures.Outer<int>.Inner<string> @class) => left;\n",
            CSharp.Operator("Money", "+").Public().Static().Arrow("left").Parameter(typeof(Outer<int>.Inner<string>), "class").ToString());
    }

    [Fact]
    public void OperatorParameters_TypeOverloadsCombineWithParameterConfiguration()
    {
        var builder = CSharp.Operator("Money", "+").Public().Static().Arrow("left")
            .Parameter(typeof(string), "receiver", p => p.This())
            .Parameter(typeof(int), "changed", p => p.Ref())
            .Parameter(typeof(int), "result", p => p.Out())
            .Parameter(typeof(int), "input", p => p.In())
            .Parameter(typeof(int), "count", p => p.Attribute("Count").Default("1"))
            .Parameter(typeof(int[]), "values", p => p.Params());

        OutputAssert.Equal("public static Money operator +(this string receiver, ref int changed, out int result, in int input, [Count] int count = 1, params int[] values) => left;\n", builder.ToString());
    }

    [Fact]
    public void OperatorParameters_InvalidTypesThrowBeforeNameValidationConfigurationOrAdding()
    {
        var builder = CSharp.Operator("Money", "+").Public().Static().Arrow("left").Parameter("int", "existing");
        var configured = false;

        foreach (var type in new[] { typeof(List<>), typeof(int).MakeByRefType() })
        {
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "value"));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "value", _ => configured = true));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "1x"));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, "1x", _ => configured = true));
            Assert.Throws<ArgumentException>("type", () => builder.Parameter(type, null!, null!));
            Assert.False(configured);
            OutputAssert.Equal("public static Money operator +(int existing) => left;\n", builder.ToString());
        }
    }

    [Fact]
    public void OperatorParameters_InvalidNamesAndNullArgumentsPreserveParameters()
    {
        var builder = CSharp.Operator("Money", "+").Public().Static().Arrow("left").Parameter("int", "existing");
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
        OutputAssert.Equal("public static Money operator +(int existing) => left;\n", builder.ToString());
    }

    [Fact]
    public void PrimaryParameters_AllOverloadsPreserveOrderAndReturnSameBuilder()
    {
        AssertPrimaryAllOverloadsPreserveOrderAndReturnSameBuilder(() => CSharp.Class("Sample"), "class", "\n{\n}\n");
        AssertPrimaryAllOverloadsPreserveOrderAndReturnSameBuilder(() => CSharp.Struct("Sample"), "struct", "\n{\n}\n");
        AssertPrimaryAllOverloadsPreserveOrderAndReturnSameBuilder(() => CSharp.Record("Sample"), "record", ";\n");
        AssertPrimaryAllOverloadsPreserveOrderAndReturnSameBuilder(() => CSharp.RecordStruct("Sample"), "record struct", ";\n");
    }

    private static void AssertPrimaryAllOverloadsPreserveOrderAndReturnSameBuilder<TSelf>(Func<TSelf> create, string keyword, string ending)
        where TSelf : ClassOrStructBuilder<TSelf>
    {
        var builder = create().PrimaryParameter("bool", "first");
        var typeCalls = 0;
        var genericCalls = 0;

        Assert.Same(builder, builder.PrimaryParameter(typeof(int), "class"));
        Assert.Same(builder, builder.PrimaryParameter(typeof(string), "text", p =>
        {
            typeCalls++;
            p.Default("\"\"");
        }));
        Assert.Equal(1, typeCalls);
        Assert.Same(builder, builder.PrimaryParameter<Outer<int>.Inner<string>>("nested"));
        Assert.Same(builder, builder.PrimaryParameter<int[]>("values", p =>
        {
            genericCalls++;
            p.Params();
        }));
        Assert.Equal(1, genericCalls);

        OutputAssert.Equal(keyword + " Sample(bool first, int @class, string text = \"\", global::Codrio.Tests.Fixtures.Outer<int>.Inner<string> nested, params int[] values)" + ending, builder.ToString());
        Assert.Equal(1, typeCalls);
        Assert.Equal(1, genericCalls);
    }

    [Fact]
    public void PrimaryParameters_StringTypeAndGenericOverloadsRenderSameName()
    {
        AssertPrimaryStringTypeAndGenericOverloadsRenderSameName(() => CSharp.Class("Sample"), "class", "\n{\n}\n");
        AssertPrimaryStringTypeAndGenericOverloadsRenderSameName(() => CSharp.Struct("Sample"), "struct", "\n{\n}\n");
        AssertPrimaryStringTypeAndGenericOverloadsRenderSameName(() => CSharp.Record("Sample"), "record", ";\n");
        AssertPrimaryStringTypeAndGenericOverloadsRenderSameName(() => CSharp.RecordStruct("Sample"), "record struct", ";\n");
    }

    private static void AssertPrimaryStringTypeAndGenericOverloadsRenderSameName<TSelf>(Func<TSelf> create, string keyword, string ending)
        where TSelf : ClassOrStructBuilder<TSelf>
    {
        const string typeName = "global::System.Collections.Generic.Dictionary<string, int>";
        var expected = keyword + " Sample(global::System.Collections.Generic.Dictionary<string, int> map)" + ending;

        OutputAssert.Equal(expected, create().PrimaryParameter(typeName, "map").ToString());
        OutputAssert.Equal(expected, create().PrimaryParameter(typeof(Dictionary<string, int>), "map").ToString());
        OutputAssert.Equal(expected, create().PrimaryParameter<Dictionary<string, int>>("map").ToString());
        OutputAssert.Equal(expected, create().PrimaryParameter(typeName, "map", _ => { }).ToString());
        OutputAssert.Equal(expected, create().PrimaryParameter(typeof(Dictionary<string, int>), "map", _ => { }).ToString());
        OutputAssert.Equal(expected, create().PrimaryParameter<Dictionary<string, int>>("map", _ => { }).ToString());
        OutputAssert.Equal(keyword + " Sample(global::Codrio.Tests.Fixtures.Outer<int>.Inner<string> @class)" + ending,
            create().PrimaryParameter(typeof(Outer<int>.Inner<string>), "class").ToString());
    }

    [Fact]
    public void PrimaryParameters_TypeOverloadsCombineWithParameterConfiguration()
    {
        AssertPrimaryTypeOverloadsCombineWithParameterConfiguration(() => CSharp.Class("Sample"), "class", "\n{\n}\n");
        AssertPrimaryTypeOverloadsCombineWithParameterConfiguration(() => CSharp.Struct("Sample"), "struct", "\n{\n}\n");
        AssertPrimaryTypeOverloadsCombineWithParameterConfiguration(() => CSharp.Record("Sample"), "record", ";\n");
        AssertPrimaryTypeOverloadsCombineWithParameterConfiguration(() => CSharp.RecordStruct("Sample"), "record struct", ";\n");
    }

    private static void AssertPrimaryTypeOverloadsCombineWithParameterConfiguration<TSelf>(Func<TSelf> create, string keyword, string ending)
        where TSelf : ClassOrStructBuilder<TSelf>
    {
        var builder = create()
            .PrimaryParameter(typeof(string), "receiver", p => p.This())
            .PrimaryParameter(typeof(int), "changed", p => p.Ref())
            .PrimaryParameter(typeof(int), "result", p => p.Out())
            .PrimaryParameter(typeof(int), "input", p => p.In())
            .PrimaryParameter(typeof(int), "count", p => p.Attribute("Count").Default("1"))
            .PrimaryParameter(typeof(int[]), "values", p => p.Params());

        OutputAssert.Equal(keyword + " Sample(this string receiver, ref int changed, out int result, in int input, [Count] int count = 1, params int[] values)" + ending, builder.ToString());
    }

    [Fact]
    public void PrimaryParameters_InvalidTypesThrowBeforeNameValidationConfigurationOrAdding()
    {
        AssertPrimaryInvalidTypesThrowBeforeNameValidationConfigurationOrAdding(() => CSharp.Class("Sample"), "class", "\n{\n}\n");
        AssertPrimaryInvalidTypesThrowBeforeNameValidationConfigurationOrAdding(() => CSharp.Struct("Sample"), "struct", "\n{\n}\n");
        AssertPrimaryInvalidTypesThrowBeforeNameValidationConfigurationOrAdding(() => CSharp.Record("Sample"), "record", ";\n");
        AssertPrimaryInvalidTypesThrowBeforeNameValidationConfigurationOrAdding(() => CSharp.RecordStruct("Sample"), "record struct", ";\n");
    }

    private static void AssertPrimaryInvalidTypesThrowBeforeNameValidationConfigurationOrAdding<TSelf>(Func<TSelf> create, string keyword, string ending)
        where TSelf : ClassOrStructBuilder<TSelf>
    {
        var builder = create().PrimaryParameter("int", "existing");
        var configured = false;

        foreach (var type in new[] { typeof(List<>), typeof(int).MakeByRefType() })
        {
            Assert.Throws<ArgumentException>("type", () => builder.PrimaryParameter(type, "value"));
            Assert.Throws<ArgumentException>("type", () => builder.PrimaryParameter(type, "value", _ => configured = true));
            Assert.Throws<ArgumentException>("type", () => builder.PrimaryParameter(type, "1x"));
            Assert.Throws<ArgumentException>("type", () => builder.PrimaryParameter(type, "1x", _ => configured = true));
            Assert.Throws<ArgumentException>("type", () => builder.PrimaryParameter(type, null!, null!));
            Assert.False(configured);
            OutputAssert.Equal(keyword + " Sample(int existing)" + ending, builder.ToString());
        }
    }

    [Fact]
    public void PrimaryParameters_InvalidNamesAndNullArgumentsPreserveParameters()
    {
        AssertPrimaryInvalidNamesAndNullArgumentsPreserveParameters(() => CSharp.Class("Sample"), "class", "\n{\n}\n");
        AssertPrimaryInvalidNamesAndNullArgumentsPreserveParameters(() => CSharp.Struct("Sample"), "struct", "\n{\n}\n");
        AssertPrimaryInvalidNamesAndNullArgumentsPreserveParameters(() => CSharp.Record("Sample"), "record", ";\n");
        AssertPrimaryInvalidNamesAndNullArgumentsPreserveParameters(() => CSharp.RecordStruct("Sample"), "record struct", ";\n");
    }

    private static void AssertPrimaryInvalidNamesAndNullArgumentsPreserveParameters<TSelf>(Func<TSelf> create, string keyword, string ending)
        where TSelf : ClassOrStructBuilder<TSelf>
    {
        var builder = create().PrimaryParameter("int", "existing");
        var configured = false;

        Assert.Throws<ArgumentException>("name", () => builder.PrimaryParameter(typeof(int), "1x"));
        Assert.Throws<ArgumentException>("name", () => builder.PrimaryParameter(typeof(int), "1x", _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => builder.PrimaryParameter<int>("1x"));
        Assert.Throws<ArgumentException>("name", () => builder.PrimaryParameter<int>("1x", _ => configured = true));
        Assert.Throws<ArgumentNullException>("type", () => builder.PrimaryParameter((Type)null!, "value"));
        Assert.Throws<ArgumentNullException>("type", () => builder.PrimaryParameter((Type)null!, "value", _ => configured = true));
        Assert.Throws<ArgumentNullException>("type", () => builder.PrimaryParameter((Type)null!, null!, null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.PrimaryParameter(typeof(int), null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.PrimaryParameter(typeof(int), null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.PrimaryParameter(typeof(int), "value", null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.PrimaryParameter<int>(null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.PrimaryParameter<int>(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.PrimaryParameter<int>("value", null!));
        Assert.False(configured);
        OutputAssert.Equal(keyword + " Sample(int existing)" + ending, builder.ToString());
    }

    [Fact]
    public void IndexerOperatorAndPrimaryParameters_RenderExamples()
    {
        OutputAssert.Equal("string this[int index, string key] { get; set; }\n",
            CSharp.Indexer("string", i => i.Parameter<int>("index").Parameter(typeof(string), "key")).ToString());
        OutputAssert.Equal("public static Money operator +(Money left, decimal right) => left;\n",
            CSharp.Operator("Money", "+", o => o.Public().Static()
                .Parameter("Money", "left").Parameter<decimal>("right").Arrow("left")).ToString());
        OutputAssert.Equal("record Point(int X, int Y);\n",
            CSharp.Record("Point", r => r.PrimaryParameter<int>("X").PrimaryParameter(typeof(int), "Y")).ToString());
    }

    [Fact]
    public void CompleteFile_WithIndexerOperatorAndPrimaryParameters_Compiles()
    {
        var file = CSharp.File().Namespace("App", n => n
            .Class("Lookup", c => c
                .Indexer("string", i => i.Public()
                    .Parameter(typeof(int), "index")
                    .Parameter<string>("key")
                    .Parameter(typeof(Outer<int>.Inner<string>), "nested", p => p.Attribute("System.ComponentModel.Description", "\"Nested\""))
                    .Parameter<int[]>("values", p => p.Params())
                    .Arrow("key")))
            .Struct("Money", s => s
                .Operator("bool", "==", o => o.Public().Static()
                    .Parameter("Money", "left").Parameter(typeof(int), "right").Arrow("true"))
                .Operator("bool", "!=", o => o.Public().Static()
                    .Parameter("Money", "left").Parameter<int>("right").Arrow("false"))
                .Operator("Money", "+", o => o.Public().Static()
                    .Parameter("Money", "left").Parameter(typeof(decimal), "right", p => p.In()).Arrow("left"))
                .Operator("Money", "+", o => o.Public().Static()
                    .Parameter("Money", "left").Parameter<int>("right", p => p.In()).Arrow("left")))
            .Class("Service", c => c
                .PrimaryParameter(typeof(int), "id")
                .PrimaryParameter<Outer<int>.Inner<string>>("nested")
                .PrimaryParameter(typeof(string), "name", p => p.Attribute("System.ComponentModel.Description", "\"Name\""))
                .PrimaryParameter<int>("count", p => p.Default("1")))
            .Struct("Value", s => s
                .PrimaryParameter(typeof(int), "id")
                .PrimaryParameter<CodrioGlobalFixture>("item")
                .PrimaryParameter(typeof(string), "name", p => p.Attribute("System.ComponentModel.Description", "\"Name\""))
                .PrimaryParameter<int>("count", p => p.Default("1")))
            .Record("Entry", r => r
                .PrimaryParameter(typeof(int), "Id")
                .PrimaryParameter<Outer<int>.Leaf>("Item")
                .PrimaryParameter(typeof(string), "Name", p => p.Attribute("System.ComponentModel.Description", "\"Name\""))
                .PrimaryParameter<int>("Count", p => p.Default("1")))
            .RecordStruct("Point", r => r
                .PrimaryParameter(typeof(int), "X")
                .PrimaryParameter<Fixtures.@class>("Item")
                .PrimaryParameter(typeof(string), "Name", p => p.Attribute("System.ComponentModel.Description", "\"Name\""))
                .PrimaryParameter<int>("Y", p => p.Default("1"))));

        const string expected = """
            namespace App
            {
                class Lookup
                {
                    public string this[int index, string key, [System.ComponentModel.Description("Nested")] global::Codrio.Tests.Fixtures.Outer<int>.Inner<string> nested, params int[] values] => key;
                }

                struct Money
                {
                    public static bool operator ==(Money left, int right) => true;

                    public static bool operator !=(Money left, int right) => false;

                    public static Money operator +(Money left, in decimal right) => left;

                    public static Money operator +(Money left, in int right) => left;
                }

                class Service(int id, global::Codrio.Tests.Fixtures.Outer<int>.Inner<string> nested, [System.ComponentModel.Description("Name")] string name, int count = 1)
                {
                }

                struct Value(int id, global::CodrioGlobalFixture item, [System.ComponentModel.Description("Name")] string name, int count = 1)
                {
                }

                record Entry(int Id, global::Codrio.Tests.Fixtures.Outer<int>.Leaf Item, [System.ComponentModel.Description("Name")] string Name, int Count = 1);

                record struct Point(int X, global::Codrio.Tests.Fixtures.@class Item, [System.ComponentModel.Description("Name")] string Name, int Y = 1);
            }
            """;

        OutputAssert.Equal(expected + "\n", file.ToString());
        CompilationAssert.NoErrors(file.ToString() + "\n" + TypeFixtures.Source);
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
