using System;
using Xunit;

namespace Codrio.Tests;

public class OperatorBuilderTests
{
    [Fact]
    public void EntryPoints_RenderDeclarationHeadsWithoutImplicitModifiers()
    {
        OutputAssert.Equal("int operator +();\n", CSharp.Operator("int", "+").ToString());
        OutputAssert.Equal("implicit operator decimal();\n", CSharp.ImplicitOperator("decimal").ToString());
        OutputAssert.Equal("explicit operator Money();\n", CSharp.ExplicitOperator("Money").ToString());
        OutputAssert.Equal("public static Money operator +(Money left, Money right) => new Money(left.Amount + right.Amount);\n",
            CSharp.Operator("Money", "+", o => o.Public().Static().Parameter("Money", "left").Parameter("Money", "right")
                .Arrow("new Money(left.Amount + right.Amount)")).ToString());
        OutputAssert.Equal("public static implicit operator decimal(Money value) => value.Amount;\n",
            CSharp.ImplicitOperator("decimal", o => o.Public().Static().Parameter("Money", "value").Arrow("value.Amount")).ToString());
        OutputAssert.Equal("public static explicit operator Money(decimal value) => new Money(value);\n",
            CSharp.ExplicitOperator("Money", o => o.Public().Static().Parameter("decimal", "value").Arrow("new Money(value)")).ToString());
        OutputAssert.Equal("static abstract TSelf operator +(TSelf left, TSelf right);\n",
            CSharp.Operator("TSelf", "+", o => o.Static().Abstract().Parameter("TSelf", "left").Parameter("TSelf", "right")).ToString());
    }

    [Theory]
    [InlineData("==")]
    [InlineData("!")]
    [InlineData("true")]
    [InlineData("checked +")]
    [InlineData("unvalidated symbol")]
    [InlineData("")]
    public void Operator_WritesSymbolVerbatim(string symbol)
    {
        OutputAssert.Equal("int operator " + symbol + "();\n", CSharp.Operator("int", symbol).ToString());
    }

    [Theory]
    [InlineData("global::Example.Money")]
    [InlineData("unvalidated type")]
    [InlineData("")]
    public void EntryPoints_WriteTypesVerbatim(string type)
    {
        OutputAssert.Equal(type + " operator +();\n", CSharp.Operator(type, "+").ToString());
        OutputAssert.Equal("implicit operator " + type + "();\n", CSharp.ImplicitOperator(type).ToString());
        OutputAssert.Equal("explicit operator " + type + "();\n", CSharp.ExplicitOperator(type).ToString());
    }

    [Fact]
    public void Parameters_PreserveOrderEscapeKeywordsAndConfigureImmediatelyOnce()
    {
        var builder = CSharp.Operator("Money", "+").Parameter("Money", "class");
        ParameterBuilder? configured = null;
        var calls = 0;
        Assert.Same(builder, builder.Parameter("Money", "right", p =>
        {
            configured = p;
            calls++;
            p.In();
        }));
        Assert.Equal(1, calls);
        Assert.Same(configured, builder.parameters[1]);
        OutputAssert.Equal("Money operator +(Money @class, in Money right);\n", builder.ToString());
        OutputAssert.Equal("Money operator +(Money @class, in Money right);\n", builder.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void BodyAndArrow_LastCallWins()
    {
        var builder = CSharp.Operator("int", "+");
        OutputAssert.Equal("int operator +();\n", builder.ToString());
        Assert.Same(builder, builder.Body());
        OutputAssert.Equal("int operator +()\n{\n}\n", builder.ToString());
        Assert.Same(builder, builder.Body(b => b.Line("return 1;")));
        OutputAssert.Equal("int operator +()\n{\n    return 1;\n}\n", builder.ToString());
        Assert.Same(builder, builder.Arrow("2"));
        OutputAssert.Equal("int operator +() => 2;\n", builder.ToString());
        builder.Arrow("3");
        OutputAssert.Equal("int operator +() => 3;\n", builder.ToString());
        builder.Body(b => b.Line("return 4;"));
        OutputAssert.Equal("int operator +()\n{\n    return 4;\n}\n", builder.ToString());
        builder.Arrow("5").Body();
        OutputAssert.Equal("int operator +()\n{\n}\n", builder.ToString());
        builder.Body(_ => { });
        OutputAssert.Equal("int operator +()\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void Body_ImmediatelyConfiguresOnceAndRetainsFreshBuilder()
    {
        var builder = CSharp.Operator("int", "+");
        BodyBuilder? configured = null;
        var calls = 0;
        builder.Body(b => { configured = b; calls++; b.Line("First();"); });
        Assert.Equal(1, calls);
        Assert.NotNull(configured);
        configured.Line("return 1;");
        OutputAssert.Equal("int operator +()\n{\n    First();\n    return 1;\n}\n", builder.ToString());
        Assert.Equal(1, calls);
        builder.Body(b => Assert.NotSame(configured, b));
        configured.Line("Detached();");
        OutputAssert.Equal("int operator +()\n{\n}\n", builder.ToString());
    }

    [Theory]
    [InlineData("value.Amount")]
    [InlineData("unvalidated expression")]
    [InlineData("")]
    public void Arrow_WritesExpressionVerbatim(string expression)
    {
        OutputAssert.Equal("int operator +() => " + expression + ";\n", CSharp.Operator("int", "+").Arrow(expression).ToString());
    }

    [Fact]
    public void Modifiers_RenderInFixedOrderOnce()
    {
        var builder = CSharp.Operator("int", "+").ReadOnly().Override().Sealed().Abstract().Virtual().New().Static().Public();
        const string expected = "public static new virtual abstract sealed override readonly int operator +();\n";
        OutputAssert.Equal(expected, builder.ToString());
        builder.Static().New().Virtual().Abstract().Sealed().Override().ReadOnly();
        OutputAssert.Equal(expected, builder.ToString());
    }

    [Fact]
    public void Accessibility_LastCallWinsAndFluentMethodsReturnSameBuilder()
    {
        var builder = CSharp.Operator("int", "+");
        Assert.Same(builder, builder.Public());
        Assert.Same(builder, builder.Internal());
        OutputAssert.Equal("internal int operator +();\n", builder.ToString());
        Assert.Same(builder, builder.Protected());
        OutputAssert.Equal("protected int operator +();\n", builder.ToString());
        Assert.Same(builder, builder.Private());
        OutputAssert.Equal("private int operator +();\n", builder.ToString());
        Assert.Same(builder, builder.ProtectedInternal());
        OutputAssert.Equal("protected internal int operator +();\n", builder.ToString());
        Assert.Same(builder, builder.PrivateProtected());
        OutputAssert.Equal("private protected int operator +();\n", builder.ToString());
        Assert.Same(builder, builder.Public());
        OutputAssert.Equal("public int operator +();\n", builder.ToString());
        Assert.Same(builder, builder.Static());
        Assert.Same(builder, builder.New());
        Assert.Same(builder, builder.Virtual());
        Assert.Same(builder, builder.Abstract());
        Assert.Same(builder, builder.Sealed());
        Assert.Same(builder, builder.Override());
        Assert.Same(builder, builder.ReadOnly());
        Assert.Same(builder, builder.Parameter("int", "value"));
    }

    [Theory]
    [InlineData("operator")]
    [InlineData("implicit")]
    [InlineData("explicit")]
    public void EntryPoints_InvokeConfigureImmediatelyOnceOnReturnedBuilder(string kind)
    {
        OperatorBuilder? configured = null;
        var calls = 0;
        Action<OperatorBuilder> configure = o => { configured = o; calls++; o.Public(); };
        var builder = kind == "operator" ? CSharp.Operator("int", "+", configure) :
            kind == "implicit" ? CSharp.ImplicitOperator("int", configure) : CSharp.ExplicitOperator("int", configure);
        var plain = kind == "operator" ? CSharp.Operator("int", "+") :
            kind == "implicit" ? CSharp.ImplicitOperator("int") : CSharp.ExplicitOperator("int");

        Assert.Same(builder, configured);
        Assert.NotSame(builder, plain);
        Assert.Equal(1, calls);
        OutputAssert.Equal("public " + plain.ToString(), builder.ToString());
        OutputAssert.Equal("public " + plain.ToString(), builder.ToString());
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void TypeOperators_AllEntryPointsRetainReferencesAndReturnParent(string keyword)
    {
        switch (keyword)
        {
            case "class": AssertTypeOperators(CSharp.Class("Outer"), keyword); break;
            case "struct": AssertTypeOperators(CSharp.Struct("Outer"), keyword); break;
            case "interface": AssertTypeOperators(CSharp.Interface("Outer"), keyword); break;
            case "record": AssertTypeOperators(CSharp.Record("Outer"), keyword); break;
            case "record struct": AssertTypeOperators(CSharp.RecordStruct("Outer"), keyword); break;
        }
    }

    private static void AssertTypeOperators<TSelf>(TypeDeclarationBuilder<TSelf> builder, string keyword)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        var calls = 0;
        Action<OperatorBuilder> configure = o =>
        {
            Assert.DoesNotContain(builder.items, item => ReferenceEquals(item.Target, o));
            calls++;
            o.Public();
        };
        Assert.Same(builder, builder.Operator("Outer", "+"));
        Assert.Same(builder, builder.Operator("Outer", "-", configure));
        Assert.Same(builder, builder.ImplicitOperator("int"));
        Assert.Same(builder, builder.ImplicitOperator("long", configure));
        Assert.Same(builder, builder.ExplicitOperator("int"));
        Assert.Same(builder, builder.ExplicitOperator("long", configure));
        var added = CSharp.Operator("bool", "!");
        Assert.Same(builder, builder.Add(added));
        Assert.Same(added, builder.items[6].Target);
        Assert.Equal(3, calls);
        var prefix = keyword + " Outer\n{\n    Outer operator +();\n\n    public Outer operator -();\n\n" +
            "    implicit operator int();\n\n    public implicit operator long();\n\n" +
            "    explicit operator int();\n\n    public explicit operator long();\n\n";
        OutputAssert.Equal(prefix + "    bool operator !();\n}\n", builder.ToString());
        added.Static().Arrow("false");
        OutputAssert.Equal(prefix + "    static bool operator !() => false;\n}\n", builder.ToString());
        Assert.Equal(3, calls);
        AssertNullTypeArguments(builder);
    }

    [Fact]
    public void MixedItems_PreserveOrderSpacingAndNestedBodyIndentation()
    {
        var builder = CSharp.Class("Outer").Operator("int", "+")
            .Property("int", "Count").Method("Run", m => m.Body()).Raw("// first\r\n// second")
            .Class("Inner", c => c.Operator("int", "-", o => o.Body(b => b
                .Block("if (ready)", ib => ib.Line("return 1;"))
                .Line("return 0;"))));
        OutputAssert.Equal("class Outer\n{\n    int operator +();\n\n    int Count { get; set; }\n\n" +
            "    void Run()\n    {\n    }\n\n    // first\n    // second\n\n" +
            "    class Inner\n    {\n        int operator -()\n        {\n            if (ready)\n" +
            "            {\n                return 1;\n            }\n            return 0;\n        }\n    }\n}\n", builder.ToString());
    }

    [Fact]
    public void Record_WithOnlyOperatorRendersBlock()
    {
        OutputAssert.Equal("record Money\n{\n    Money operator +();\n}\n", CSharp.Record("Money").Operator("Money", "+").ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("1value")]
    [InlineData("a.b")]
    [InlineData("a b")]
    [InlineData("a-b")]
    [InlineData("@@value")]
    public void InvalidParameterNames_ThrowBeforeConfiguringOrAdding(string name)
    {
        var builder = CSharp.Operator("int", "+");
        var configured = false;
        Assert.Throws<ArgumentException>("name", () => builder.Parameter("int", name));
        Assert.Throws<ArgumentException>("name", () => builder.Parameter("int", name, _ => configured = true));
        Assert.False(configured);
        OutputAssert.Equal("int operator +();\n", builder.ToString());
    }

    [Fact]
    public void NullArguments_ThrowWithParameterNamesBeforeConfiguringOrMutating()
    {
        var configured = false;
        Assert.Throws<ArgumentNullException>("returnType", () => CSharp.Operator((string)null!, "+"));
        Assert.Throws<ArgumentNullException>("returnType", () => CSharp.Operator((string)null!, "+", _ => configured = true));
        Assert.Throws<ArgumentNullException>("symbol", () => CSharp.Operator("int", null!));
        Assert.Throws<ArgumentNullException>("symbol", () => CSharp.Operator("int", null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Operator("int", "+", null!));
        Assert.Throws<ArgumentNullException>("type", () => CSharp.ImplicitOperator((string)null!));
        Assert.Throws<ArgumentNullException>("type", () => CSharp.ImplicitOperator((string)null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.ImplicitOperator("int", null!));
        Assert.Throws<ArgumentNullException>("type", () => CSharp.ExplicitOperator((string)null!));
        Assert.Throws<ArgumentNullException>("type", () => CSharp.ExplicitOperator((string)null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.ExplicitOperator("int", null!));
        var builder = CSharp.Operator("int", "+").Arrow("1");
        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter((string)null!, "value"));
        Assert.Throws<ArgumentNullException>("type", () => builder.Parameter((string)null!, "value", _ => configured = true));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter("int", null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Parameter("int", null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Parameter("int", "value", null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Body(null!));
        Assert.Throws<ArgumentNullException>("expression", () => builder.Arrow(null!));
        Assert.False(configured);
        OutputAssert.Equal("int operator +() => 1;\n", builder.ToString());
    }

    private static void AssertNullTypeArguments<TSelf>(TypeDeclarationBuilder<TSelf> builder)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        var before = builder.ToString();
        var configured = false;
        Assert.Throws<ArgumentNullException>("returnType", () => builder.Operator((string)null!, "+"));
        Assert.Throws<ArgumentNullException>("returnType", () => builder.Operator((string)null!, "+", _ => configured = true));
        Assert.Throws<ArgumentNullException>("symbol", () => builder.Operator("int", null!));
        Assert.Throws<ArgumentNullException>("symbol", () => builder.Operator("int", null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Operator("int", "+", null!));
        Assert.Throws<ArgumentNullException>("type", () => builder.ImplicitOperator((string)null!));
        Assert.Throws<ArgumentNullException>("type", () => builder.ImplicitOperator((string)null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.ImplicitOperator("int", null!));
        Assert.Throws<ArgumentNullException>("type", () => builder.ExplicitOperator((string)null!));
        Assert.Throws<ArgumentNullException>("type", () => builder.ExplicitOperator((string)null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.ExplicitOperator("int", null!));
        Assert.Throws<ArgumentNullException>("operatorBuilder", () => builder.Add((OperatorBuilder)null!));
        Assert.False(configured);
        OutputAssert.Equal(before, builder.ToString());
    }

    [Theory]
    [InlineData(typeof(EnumBuilder))]
    [InlineData(typeof(FileBuilder))]
    [InlineData(typeof(NamespaceBuilder))]
    public void UnsupportedContainers_DoNotExposeOperatorEntryPoints(Type type)
    {
        Assert.DoesNotContain(type.GetMethods(), method => method.Name == "Operator" ||
            method.Name == "ImplicitOperator" || method.Name == "ExplicitOperator");
        Assert.DoesNotContain(type.GetMethods(), method => method.Name == "Add" &&
            Array.Exists(method.GetParameters(), parameter => parameter.ParameterType == typeof(OperatorBuilder)));
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    public void CompleteFile_RendersOperatorsAndConversionsAndCompiles(string keyword)
    {
        var file = CSharp.File();
        if (keyword == "class")
        {
            file.Class("Money", c => ConfigureMoney(c));
        }
        else
        {
            file.Struct("Money", s => ConfigureMoney(s));
        }

        var expected = keyword + " Money\n{\n    public decimal Amount { get; set; }\n\n" +
            "    public static Money operator +(Money left, Money right) => new Money { Amount = left.Amount + right.Amount };\n\n" +
            "    public static Money operator -(Money value) => new Money { Amount = -value.Amount };\n\n" +
            "    public static bool operator ==(Money left, Money right)\n    {\n        return left.Amount == right.Amount;\n    }\n\n" +
            "    public static bool operator !=(Money left, Money right)\n    {\n        return left.Amount != right.Amount;\n    }\n\n" +
            "    public static implicit operator decimal(Money value) => value.Amount;\n\n" +
            "    public static explicit operator Money(decimal value) => new Money { Amount = value };\n}\n";

        OutputAssert.Equal(expected, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }

    private static void ConfigureMoney<TSelf>(TypeDeclarationBuilder<TSelf> builder)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        builder.Property("decimal", "Amount", p => p.Public())
            .Operator("Money", "+", o => o.Public().Static().Parameter("Money", "left").Parameter("Money", "right")
                .Arrow("new Money { Amount = left.Amount + right.Amount }"))
            .Operator("Money", "-", o => o.Public().Static().Parameter("Money", "value")
                .Arrow("new Money { Amount = -value.Amount }"))
            .Operator("bool", "==", o => o.Public().Static().Parameter("Money", "left").Parameter("Money", "right")
                .Body(b => b.Line("return left.Amount == right.Amount;")))
            .Operator("bool", "!=", o => o.Public().Static().Parameter("Money", "left").Parameter("Money", "right")
                .Body(b => b.Line("return left.Amount != right.Amount;")))
            .ImplicitOperator("decimal", o => o.Public().Static().Parameter("Money", "value").Arrow("value.Amount"))
            .ExplicitOperator("Money", o => o.Public().Static().Parameter("decimal", "value").Arrow("new Money { Amount = value }"));
    }

    [Fact]
    public void CompleteFile_StaticAbstractInterfaceOperatorAndImplementationCompile()
    {
        var file = CSharp.File()
            .Interface("IAddable", i => i.TypeParameter("TSelf", t => t.Constraint("IAddable<TSelf>"))
                .Operator("TSelf", "+", o => o.Static().Abstract().Parameter("TSelf", "left").Parameter("TSelf", "right")))
            .Struct("Money", s => s.BaseType("IAddable<Money>")
                .Operator("Money", "+", o => o.Public().Static().Parameter("Money", "left").Parameter("Money", "right").Arrow("left")));

        OutputAssert.Equal("interface IAddable<TSelf> where TSelf : IAddable<TSelf>\n" +
            "{\n    static abstract TSelf operator +(TSelf left, TSelf right);\n}\n\n" +
            "struct Money : IAddable<Money>\n{\n    public static Money operator +(Money left, Money right) => left;\n}\n", file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }
}
