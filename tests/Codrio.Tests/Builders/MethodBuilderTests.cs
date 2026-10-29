using System;
using Xunit;

namespace Codrio.Tests;

public class MethodBuilderTests
{
    [Fact]
    public void Body_RendersEmptyBlock()
    {
        OutputAssert.Equal("void Run()\n{\n}\n", CSharp.Method("Run").Body().ToString());
        OutputAssert.Equal("void Run()\n{\n}\n", CSharp.Method("Run").Body(_ => { }).ToString());
    }

    [Fact]
    public void Body_RendersConfiguredLinesAndBlocks()
    {
        var method = CSharp.Method("Clamp", m => m.Public().Returns("int").Parameter("int", "x")
            .Body(b => b.Line("var y = x * 2;")
                .Block("if (y > 10)", ib => ib.Line("return 10;"))
                .Line("return y;")));

        OutputAssert.Equal(
            "public int Clamp(int x)\n{\n    var y = x * 2;\n    if (y > 10)\n    {\n        return 10;\n    }\n    return y;\n}\n",
            method.ToString());
    }

    [Theory]
    [InlineData("items.Count")]
    [InlineData("unvalidated expression")]
    [InlineData("")]
    public void Arrow_RendersExpressionVerbatim(string expression)
    {
        var method = CSharp.Method("Count", m => m.Returns("int").Arrow(expression));

        OutputAssert.Equal("int Count() => " + expression + ";\n", method.ToString());
    }

    [Fact]
    public void BodyAndArrow_LastCallWins()
    {
        var method = CSharp.Method("Count").Returns("int");
        OutputAssert.Equal("int Count();\n", method.ToString());
        method.Body(b => b.Line("return 1;")).Arrow("2");
        OutputAssert.Equal("int Count() => 2;\n", method.ToString());
        method.Arrow("3");
        OutputAssert.Equal("int Count() => 3;\n", method.ToString());
        method.Body(b => b.Line("return 4;"));
        OutputAssert.Equal("int Count()\n{\n    return 4;\n}\n", method.ToString());
        method.Body(b => b.Line("return 5;"));
        OutputAssert.Equal("int Count()\n{\n    return 5;\n}\n", method.ToString());
        method.Body();
        OutputAssert.Equal("int Count()\n{\n}\n", method.ToString());
        method.Arrow("6").Body();
        OutputAssert.Equal("int Count()\n{\n}\n", method.ToString());
    }

    [Fact]
    public void Async_RendersOnceAfterCommonModifiersAndBeforePartial()
    {
        OutputAssert.Equal("async void Run();\n", CSharp.Method("Run").Async().Async().ToString());
        var method = CSharp.Method("Run").Partial().Async().ReadOnly().Override().Sealed()
            .Abstract().Virtual().New().Static().Public();
        const string expected = "public static new virtual abstract sealed override readonly async partial void Run();\n";

        OutputAssert.Equal(expected, method.ToString());
        method.Async().Partial();
        OutputAssert.Equal(expected, method.ToString());
    }

    [Fact]
    public void NestedClass_IndentsMethodBodyAndBlocks()
    {
        var builder = CSharp.Class("Outer").Class("Inner", c => c.Method("Run", m => m
            .Body(b => b.Block("if (ready)", ib => ib.Line("Start();")))));

        OutputAssert.Equal(
            "class Outer\n{\n    class Inner\n    {\n        void Run()\n        {\n            if (ready)\n            {\n                Start();\n            }\n        }\n    }\n}\n",
            builder.ToString());
    }

    [Fact]
    public void MultilineArrow_IndentsEachLineAtMethodIndentation()
    {
        var builder = CSharp.Class("Counter").Method("Count", m => m.Returns("int").Arrow("first\r\n+ second\n+ third"));

        OutputAssert.Equal("class Counter\n{\n    int Count() => first\n    + second\n    + third;\n}\n", builder.ToString());
    }

    [Fact]
    public void GenericMethod_RendersBodyAfterWhereClauses()
    {
        var method = CSharp.Method("Create").Returns("T").TypeParameter("T", t => t.Constraint("new()"))
            .Body(b => b.Line("return new T();"));

        OutputAssert.Equal("T Create<T>() where T : new()\n{\n    return new T();\n}\n", method.ToString());
        method.Arrow("new T()");
        OutputAssert.Equal("T Create<T>() where T : new() => new T();\n", method.ToString());
    }

    [Fact]
    public void Body_InvokesConfigureImmediatelyOnceAndRetainsFreshBuilder()
    {
        BodyBuilder? body = null;
        var calls = 0;
        var method = CSharp.Method("Run");
        method.Body(b =>
        {
            body = b;
            calls++;
            OutputAssert.Equal("void Run();\n", method.ToString());
            b.Line("First();");
        });

        Assert.Equal(1, calls);
        Assert.NotNull(body);
        body.Line("Second();");
        const string expected = "void Run()\n{\n    First();\n    Second();\n}\n";
        OutputAssert.Equal(expected, method.ToString());
        OutputAssert.Equal(expected, method.ToString());
        Assert.Equal(1, calls);
        method.Body(b =>
        {
            Assert.NotSame(body, b);
            OutputAssert.Equal("", b.ToString());
        });
        body.Line("Detached();");
        OutputAssert.Equal("void Run()\n{\n}\n", method.ToString());
    }

    [Fact]
    public void NullBodyArguments_ThrowWithoutReplacingBody()
    {
        var method = CSharp.Method("Count").Returns("int").Arrow("1");

        Assert.Throws<ArgumentNullException>("configure", () => method.Body(null!));
        Assert.Throws<ArgumentNullException>("expression", () => method.Arrow(null!));
        OutputAssert.Equal("int Count() => 1;\n", method.ToString());
    }

    [Fact]
    public void BodyMethods_ReturnSameMethodBuilder()
    {
        var method = CSharp.Method("Run");

        Assert.Same(method, method.Body());
        Assert.Same(method, method.Body(b => b.Line("Work();")));
        Assert.Same(method, method.Arrow("Work()"));
        Assert.Same(method, method.Async());
    }

    [Fact]
    public void CompleteFile_RendersControlFlowBlocksAndCompiles()
    {
        var file = CSharp.File().Class("Calculator", c => c.Method("Sum", m => m.Public().Returns("int")
            .Parameter("int", "count").Body(b => b
                .Block("if (count < 0)", ib => ib.Line("return 0;"))
                .Block("else", eb => eb.Line("count += 1;"))
                .Line("var sum = 0;")
                .Block("try", tb => tb.Block("for (var i = 0; i < count; i++)", fb => fb.Line("sum += i;")))
                .Block("catch (System.Exception)", cb => cb.Line("return -1;"))
                .Line("return sum;"))));
        const string expected = "class Calculator\n{\n    public int Sum(int count)\n    {\n" +
            "        if (count < 0)\n        {\n            return 0;\n        }\n" +
            "        else\n        {\n            count += 1;\n        }\n        var sum = 0;\n" +
            "        try\n        {\n            for (var i = 0; i < count; i++)\n            {\n                sum += i;\n            }\n        }\n" +
            "        catch (System.Exception)\n        {\n            return -1;\n        }\n        return sum;\n    }\n}\n";

        OutputAssert.Equal(expected, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }

    [Fact]
    public void CompleteFile_RendersAsyncGenericAndOverrideMethodsAndCompiles()
    {
        var file = CSharp.File().Class("Runner", c => c
            .Method("Count", m => m.Public().Returns("int").Arrow("42"))
            .Method("RunAsync", m => m.Public().Async().Returns("System.Threading.Tasks.Task")
                .Body(b => b.Line("await System.Threading.Tasks.Task.Yield();")))
            .Method("Create", m => m.Public().Returns("T").TypeParameter("T", t => t.Constraint("new()"))
                .Body(b => b.Line("return new T();")))
            .Method("ToString", m => m.Public().Override().Returns("string").Arrow("\"Runner\"")));
        const string expected = "class Runner\n{\n    public int Count() => 42;\n\n" +
            "    public async System.Threading.Tasks.Task RunAsync()\n    {\n        await System.Threading.Tasks.Task.Yield();\n    }\n\n" +
            "    public T Create<T>() where T : new()\n    {\n        return new T();\n    }\n\n" +
            "    public override string ToString() => \"Runner\";\n}\n";

        OutputAssert.Equal(expected, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }

    [Fact]
    public void CompleteFile_RendersNamespaceLevelExtensionMethodAndCompiles()
    {
        var file = CSharp.File().Namespace("Example", n => n.Class("Extensions", c => c.Public().Static()
            .Add(CSharp.Method("Twice").Public().Static().Returns("int")
                .Parameter("int", "value", p => p.This()).Arrow("value * 2"))));

        OutputAssert.Equal(
            "namespace Example\n{\n    public static class Extensions\n    {\n        public static int Twice(this int value) => value * 2;\n    }\n}\n",
            file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }

    [Fact]
    public void CompleteFile_RendersStructInterfaceAndPartialImplementationsAndCompiles()
    {
        var file = CSharp.File()
            .Struct("Value", s => s.Method("Zero", m => m.Public().ReadOnly().Returns("int").Arrow("0")))
            .Interface("IRunner", i => i.Method("Run", m => m.Body()))
            .Class("Observer", c => c.Partial()
                .Method("OnChanged", m => m.Partial())
                .Method("OnChanged", m => m.Partial().Body()));
        const string expected = "struct Value\n{\n    public readonly int Zero() => 0;\n}\n\n" +
            "interface IRunner\n{\n    void Run()\n    {\n    }\n}\n\n" +
            "partial class Observer\n{\n    partial void OnChanged();\n\n    partial void OnChanged()\n    {\n    }\n}\n";

        OutputAssert.Equal(expected, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }

    [Fact]
    public void Parameters_RenderInAddedOrder()
    {
        var builder = CSharp.Method("Save").Parameter("string", "path");

        OutputAssert.Equal("void Save(string path);\n", builder.ToString());
        builder.Parameter("bool", "overwrite", p => p.Default("false")).Parameter("int", "retries");
        OutputAssert.Equal("void Save(string path, bool overwrite = false, int retries);\n", builder.ToString());
    }

    [Fact]
    public void ParameterModifiers_RenderConfiguredFragments()
    {
        var builder = CSharp.Method("Update")
            .Parameter("int", "count", p => p.Ref())
            .Parameter("int", "value", p => p.Out())
            .Parameter("int", "delta", p => p.In())
            .Parameter("object[]", "args", p => p.Params());

        OutputAssert.Equal("void Update(ref int count, out int value, in int delta, params object[] args);\n", builder.ToString());
        OutputAssert.Equal("public static int Twice(this int value);\n",
            CSharp.Method("Twice").Public().Static().Returns("int").Parameter("int", "value", p => p.This()).ToString());
    }

    [Theory]
    [InlineData("class", "@class")]
    [InlineData("@class", "@class")]
    public void ParameterNames_EscapeKeywords(string name, string expected)
    {
        OutputAssert.Equal("void Run(int " + expected + ");\n", CSharp.Method("Run").Parameter("int", name).ToString());
        OutputAssert.Equal("void Run(int " + expected + " = 0);\n",
            CSharp.Method("Run").Parameter("int", name, p => p.Default("0")).ToString());
    }

    [Fact]
    public void TypeParameters_RenderInAddedOrderWithOnlyNonemptyConstraints()
    {
        var builder = CSharp.Method("Run").TypeParameter("T");

        OutputAssert.Equal("void Run<T>();\n", builder.ToString());
        builder.TypeParameter("TValue", t => t.Constraint("class").Constraint("new()"))
            .TypeParameter("TUnused", _ => { })
            .TypeParameter("TKey", t => t.Constraint("struct"));
        OutputAssert.Equal("void Run<T, TValue, TUnused, TKey>() where TValue : class, new() where TKey : struct;\n", builder.ToString());
    }

    [Theory]
    [InlineData("class", "@class")]
    [InlineData("@class", "@class")]
    public void TypeParameterNames_EscapeKeywordsInDeclarationAndConstraints(string name, string expected)
    {
        OutputAssert.Equal("void Run<" + expected + ">();\n", CSharp.Method("Run").TypeParameter(name).ToString());
        OutputAssert.Equal("void Run<" + expected + ">() where " + expected + " : class;\n",
            CSharp.Method("Run").TypeParameter(name, t => t.Constraint("class")).ToString());
    }

    [Fact]
    public void ParametersAndTypeParameters_RenderInSyntaxOrderRegardlessOfCallOrder()
    {
        var first = CSharp.Method("Get", m => m.Public().Returns("T")
            .TypeParameter("T", t => t.Constraint("class"))
            .Parameter("string", "key").Parameter("bool", "required", p => p.Default("false")));
        var second = CSharp.Method("Get").Parameter("string", "key").Returns("T")
            .Parameter("bool", "required", p => p.Default("false"))
            .TypeParameter("T", t => t.Constraint("class")).Public();
        const string expected = "public T Get<T>(string key, bool required = false) where T : class;\n";

        OutputAssert.Equal(expected, first.ToString());
        OutputAssert.Equal(expected, second.ToString());
        second.Partial().ReadOnly().Override().Sealed().Abstract().Virtual().New().Static();
        OutputAssert.Equal("public static new virtual abstract sealed override readonly partial T Get<T>(string key, bool required = false) where T : class;\n", second.ToString());
    }

    [Fact]
    public void ParameterConfiguration_RunsImmediatelyOnceAndRetainsBuilders()
    {
        var builder = CSharp.Method("Run");
        ParameterBuilder? parameter = null;
        TypeParameterBuilder? typeParameter = null;
        var parameterCalls = 0;
        var typeParameterCalls = 0;
        builder.Parameter("int", "value", p =>
        {
            parameter = p;
            parameterCalls++;
            OutputAssert.Equal("void Run();\n", builder.ToString());
        });
        builder.TypeParameter("T", t =>
        {
            typeParameter = t;
            typeParameterCalls++;
            OutputAssert.Equal("void Run(int value);\n", builder.ToString());
        });

        Assert.NotNull(parameter);
        Assert.NotNull(typeParameter);
        Assert.Equal(1, parameterCalls);
        Assert.Equal(1, typeParameterCalls);
        OutputAssert.Equal("void Run<T>(int value);\n", builder.ToString());
        parameter.Default("7");
        typeParameter.Constraint("class");
        OutputAssert.Equal("void Run<T>(int value = 7) where T : class;\n", builder.ToString());
        OutputAssert.Equal("void Run<T>(int value = 7) where T : class;\n", builder.ToString());
        Assert.Equal(1, parameterCalls);
        Assert.Equal(1, typeParameterCalls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1value")]
    [InlineData("a.b")]
    [InlineData("a b")]
    [InlineData("a-b")]
    [InlineData("@@value")]
    public void InvalidParameterNames_ThrowAtCallTimeBeforeConfiguringOrAdding(string name)
    {
        var builder = CSharp.Method("Run");
        var configured = false;

        Assert.Throws<ArgumentException>("name", () => builder.Parameter("int", name));
        Assert.Throws<ArgumentException>("name", () => builder.Parameter("int", name, _ => configured = true));
        Assert.Throws<ArgumentException>("name", () => builder.TypeParameter(name));
        Assert.Throws<ArgumentException>("name", () => builder.TypeParameter(name, _ => configured = true));
        Assert.False(configured);
        OutputAssert.Equal("void Run();\n", builder.ToString());
    }

    [Fact]
    public void NullParameterArguments_ThrowBeforeConfiguringOrAdding()
    {
        var builder = CSharp.Method("Run");
        var configured = false;

        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter((string)null!, "value"));
        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter((string)null!, "value", _ => configured = true));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter("int", null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter("int", null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Parameter("int", "value", null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Parameter((string)null!, null!, null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.TypeParameter(null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.TypeParameter(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.TypeParameter("T", null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.TypeParameter(null!, null!));
        Assert.False(configured);
        OutputAssert.Equal("void Run();\n", builder.ToString());
    }

    [Fact]
    public void ParameterConfigurationFailure_DoesNotAddParameter()
    {
        var builder = CSharp.Method("Run");

        Assert.Throws<InvalidOperationException>(() => builder.Parameter("int", "value", _ => throw new InvalidOperationException()));
        Assert.Throws<InvalidOperationException>(() => builder.TypeParameter("T", _ => throw new InvalidOperationException()));
        OutputAssert.Equal("void Run();\n", builder.ToString());
    }

    [Fact]
    public void ParameterMethods_ReturnSameMethodBuilder()
    {
        var builder = CSharp.Method("Run");

        Assert.Same(builder, builder.Parameter("int", "value"));
        Assert.Same(builder, builder.Parameter("bool", "enabled", p => p.Default("true")));
        Assert.Same(builder, builder.TypeParameter("T"));
        Assert.Same(builder, builder.TypeParameter("TValue", t => t.Constraint("class")));
    }

    [Fact]
    public void CompleteFile_RendersParametersAndGenericConstraintsAndCompiles()
    {
        var file = CSharp.File().Using("System")
            .Interface("IStore", i => i
                .Method("Get", m => m.Returns("T").TypeParameter("T", t => t.Constraint("class")).Parameter("string", "key"))
                .Method("TryGet", m => m.Returns("bool").Parameter("string", "key").Parameter("int", "value", p => p.Out()))
                .Method("Log", m => m.Parameter("string", "format").Parameter("object[]", "args", p => p.Params()))
                .Method("Update", m => m.Parameter("int", "count", p => p.Ref()).Parameter("int", "delta", p => p.In()))
                .Add(CSharp.Method("Create").Returns("TValue")
                    .TypeParameter("TValue", t => t.Constraint("class").Constraint("IDisposable").Constraint("new()"))
                    .TypeParameter("TKey", t => t.Constraint("struct").Constraint("IComparable<TKey>"))
                    .Parameter("TKey", "key")))
            .Class("Store", c => c.Abstract().Method("Save", m => m.Public().Abstract()
                .Parameter("string", "path").Parameter("bool", "overwrite", p => p.Default("false"))));
        const string expected =
            "using System;\n\ninterface IStore\n{\n" +
            "    T Get<T>(string key) where T : class;\n\n" +
            "    bool TryGet(string key, out int value);\n\n" +
            "    void Log(string format, params object[] args);\n\n" +
            "    void Update(ref int count, in int delta);\n\n" +
            "    TValue Create<TValue, TKey>(TKey key) where TValue : class, IDisposable, new() where TKey : struct, IComparable<TKey>;\n" +
            "}\n\nabstract class Store\n{\n    public abstract void Save(string path, bool overwrite = false);\n}\n";

        OutputAssert.Equal(expected, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }

    [Theory]
    [InlineData("Run", "void Run();\n")]
    [InlineData("class", "void @class();\n")]
    [InlineData("@class", "void @class();\n")]
    public void ToString_RendersBodilessMethod(string name, string expected)
    {
        var builder = CSharp.Method(name);

        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
    }

    [Theory]
    [InlineData("int")]
    [InlineData("global::System.Collections.Generic.List<string>")]
    [InlineData("unvalidated type")]
    [InlineData("")]
    public void Returns_WritesTypeVerbatimAndLastCallWins(string type)
    {
        var builder = CSharp.Method("Run").Returns("bool").Returns(type);

        OutputAssert.Equal(type + " Run();\n", builder.ToString());
    }

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected")]
    [InlineData("private")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public void Accessibility_RendersSelectedValueAndLastCallWins(string accessibility)
    {
        var builder = CSharp.Method("Run");
        OutputAssert.Equal("void Run();\n", builder.ToString());
        builder.Public().Internal();

        switch (accessibility)
        {
            case "public": builder.Public(); break;
            case "internal": builder.Internal(); break;
            case "protected": builder.Protected(); break;
            case "private": builder.Private(); break;
            case "protected internal": builder.ProtectedInternal(); break;
            case "private protected": builder.PrivateProtected(); break;
        }

        OutputAssert.Equal(accessibility + " void Run();\n", builder.ToString());
    }

    [Theory]
    [InlineData("static")]
    [InlineData("new")]
    [InlineData("virtual")]
    [InlineData("abstract")]
    [InlineData("sealed")]
    [InlineData("override")]
    [InlineData("readonly")]
    [InlineData("partial")]
    public void Modifiers_RenderIndividually(string modifier)
    {
        var builder = CSharp.Method("Run");
        switch (modifier)
        {
            case "static": builder.Static(); break;
            case "new": builder.New(); break;
            case "virtual": builder.Virtual(); break;
            case "abstract": builder.Abstract(); break;
            case "sealed": builder.Sealed(); break;
            case "override": builder.Override(); break;
            case "readonly": builder.ReadOnly(); break;
            case "partial": builder.Partial(); break;
        }

        OutputAssert.Equal(modifier + " void Run();\n", builder.ToString());
    }

    [Fact]
    public void Modifiers_RenderInFixedOrderOnceWithoutValidatingCombinations()
    {
        var builder = CSharp.Method("Run").Partial().ReadOnly().Override().Sealed()
            .Abstract().Virtual().New().Static().Public();
        const string expected = "public static new virtual abstract sealed override readonly partial void Run();\n";

        OutputAssert.Equal(expected, builder.ToString());
        builder.Static().New().Virtual().Abstract().Sealed().Override().ReadOnly().Partial();
        OutputAssert.Equal(expected, builder.ToString());
    }

    [Fact]
    public void Method_InvokesConfigureImmediatelyOnceOnReturnedBuilder()
    {
        MethodBuilder? configured = null;
        var calls = 0;
        var builder = CSharp.Method("OnChanged", m =>
        {
            configured = m;
            calls++;
            m.Partial().Private();
        });

        Assert.Same(builder, configured);
        Assert.Equal(1, calls);
        OutputAssert.Equal("private partial void OnChanged();\n", builder.ToString());
        OutputAssert.Equal("private partial void OnChanged();\n", builder.ToString());
        Assert.Equal(1, calls);
        Assert.NotSame(builder, CSharp.Method("OnChanged"));
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void TypeMethods_ConfigureImmediatelyKeepReferencesAndReturnParent(string keyword)
    {
        switch (keyword)
        {
            case "class": AssertTypeMethods(CSharp.Class("Outer"), keyword); break;
            case "struct": AssertTypeMethods(CSharp.Struct("Outer"), keyword); break;
            case "interface": AssertTypeMethods(CSharp.Interface("Outer"), keyword); break;
            case "record": AssertTypeMethods(CSharp.Record("Outer"), keyword); break;
            case "record struct": AssertTypeMethods(CSharp.RecordStruct("Outer"), keyword); break;
        }
    }

    private static void AssertTypeMethods<TSelf>(TypeDeclarationBuilder<TSelf> builder, string keyword)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        MethodBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Method("Added");

        Assert.Same(builder, builder.Method("Run"));
        Assert.Same(builder, builder.Method("Count", m =>
        {
            configured = m;
            calls++;
            m.Returns("int");
        }));
        Assert.Same(builder, builder.Add(added));
        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, builder.items[1].Target);
        Assert.Same(added, builder.items[2].Target);
        OutputAssert.Equal(keyword + " Outer\n{\n    void Run();\n\n    int Count();\n\n    void Added();\n}\n", builder.ToString());

        configured.Returns("long");
        added.Public().Returns("string");
        var expected = keyword + " Outer\n{\n    void Run();\n\n    long Count();\n\n    public string Added();\n}\n";
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void MixedBodyItems_PreserveOrderSpacingAndNestedMethodIndentation()
    {
        var builder = CSharp.Class("Outer").Method("First")
            .Class("Nested", c => c.Method("Inside"))
            .Raw("// first\r\n// second")
            .Add(CSharp.Method("Last").Returns("int"));

        OutputAssert.Equal(
            "class Outer\n{\n    void First();\n\n    class Nested\n    {\n        void Inside();\n    }\n\n    // first\n    // second\n\n    int Last();\n}\n",
            builder.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("1Run")]
    [InlineData("Run.Value")]
    [InlineData("Run Value")]
    [InlineData("Run-Value")]
    [InlineData("@@Run")]
    public void InvalidNames_ThrowAtCallTimeBeforeConfiguringOrAdding(string name)
    {
        var configured = false;
        Assert.Throws<ArgumentException>("name", () => CSharp.Method(name));
        Assert.Throws<ArgumentException>("name", () => CSharp.Method(name, _ => configured = true));
        Assert.False(configured);

        AssertInvalidName(CSharp.Class("Outer"), name);
        AssertInvalidName(CSharp.Struct("Outer"), name);
        AssertInvalidName(CSharp.Interface("Outer"), name);
        AssertInvalidName(CSharp.Record("Outer"), name);
        AssertInvalidName(CSharp.RecordStruct("Outer"), name);
    }

    private static void AssertInvalidName<TSelf>(TypeDeclarationBuilder<TSelf> builder, string name)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        var configured = false;
        var before = builder.ToString();
        Assert.Throws<ArgumentException>("name", () => builder.Method(name));
        Assert.Throws<ArgumentException>("name", () => builder.Method(name, _ => configured = true));
        Assert.False(configured);
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void NullArguments_ThrowWithParameterNameBeforeConfiguringOrAdding()
    {
        var configured = false;
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Method(null!));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Method(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Method("Run", null!));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Method(null!, null!));
        Assert.False(configured);
        var method = CSharp.Method("Run").Returns("int");
        Assert.Throws<ArgumentNullException>("type", () => method.Returns((string)null!));
        OutputAssert.Equal("int Run();\n", method.ToString());

        AssertNullArguments(CSharp.Class("Outer"));
        AssertNullArguments(CSharp.Struct("Outer"));
        AssertNullArguments(CSharp.Interface("Outer"));
        AssertNullArguments(CSharp.Record("Outer"));
        AssertNullArguments(CSharp.RecordStruct("Outer"));
    }

    private static void AssertNullArguments<TSelf>(TypeDeclarationBuilder<TSelf> builder)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        var configured = false;
        var before = builder.ToString();
        Assert.Throws<ArgumentNullException>("name", () => builder.Method(null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Method(null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Method("Run", null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Method(null!, null!));
        Assert.Throws<ArgumentNullException>("methodBuilder", () => builder.Add((MethodBuilder)null!));
        Assert.False(configured);
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void FluentMethods_ReturnSameMethodBuilder()
    {
        var builder = CSharp.Method("Run");

        Assert.Same(builder, builder.Public());
        Assert.Same(builder, builder.Internal());
        Assert.Same(builder, builder.Protected());
        Assert.Same(builder, builder.Private());
        Assert.Same(builder, builder.ProtectedInternal());
        Assert.Same(builder, builder.PrivateProtected());
        Assert.Same(builder, builder.Static());
        Assert.Same(builder, builder.New());
        Assert.Same(builder, builder.Virtual());
        Assert.Same(builder, builder.Abstract());
        Assert.Same(builder, builder.Sealed());
        Assert.Same(builder, builder.Override());
        Assert.Same(builder, builder.ReadOnly());
        Assert.Same(builder, builder.Partial());
        Assert.Same(builder, builder.Returns("int"));
    }

    [Fact]
    public void UnsupportedContainers_DoNotExposeMethodEntryPoints()
    {
        Assert.DoesNotContain(typeof(EnumBuilder).GetMethods(), method => method.Name == "Method");
        Assert.DoesNotContain(typeof(FileBuilder).GetMethods(), method => method.Name == "Method");
        Assert.DoesNotContain(typeof(NamespaceBuilder).GetMethods(), method => method.Name == "Method");
    }

    [Fact]
    public void CompleteFile_RendersMethodDeclarationsAndCompiles()
    {
        var file = CSharp.File().AutoGeneratedHeader().NullableEnable().Using("System")
            .Namespace("Example", n => n
                .Interface("IRunner", i => i.Public().Method("Run")
                    .Method("Count", m => m.Returns("int")).Method("class", m => m.Returns("IDisposable")))
                .Class("Runner", c => c.Abstract()
                    .Method("Run", m => m.Public().Abstract())
                    .Method("Count", m => m.Protected().Abstract().Returns("int")))
                .Class("Observer", c => c.Partial().Method("OnChanged", m => m.Partial()))
                .Struct("ValueObserver", s => s.Partial().Method("OnChanged", m => m.Partial()))
                .Record("Shape", r => r.Abstract().Method("Area", m => m.Public().Abstract().Returns("double"))));
        const string expected =
            "// <auto-generated/>\n#nullable enable\n\nusing System;\n\nnamespace Example\n{\n" +
            "    public interface IRunner\n    {\n        void Run();\n\n        int Count();\n\n        IDisposable @class();\n    }\n\n" +
            "    abstract class Runner\n    {\n        public abstract void Run();\n\n        protected abstract int Count();\n    }\n\n" +
            "    partial class Observer\n    {\n        partial void OnChanged();\n    }\n\n" +
            "    partial struct ValueObserver\n    {\n        partial void OnChanged();\n    }\n\n" +
            "    abstract record Shape\n    {\n        public abstract double Area();\n    }\n}\n";

        OutputAssert.Equal(expected, file.ToString());
        OutputAssert.Equal(expected, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }
}
