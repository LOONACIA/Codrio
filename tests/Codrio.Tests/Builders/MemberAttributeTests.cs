using System;
using Xunit;

namespace Codrio.Tests;

public class MemberAttributeTests
{
    [Fact]
    public void MethodAttributes_PrecedeBodylessBlockAndExpressionDeclarations()
    {
        OutputAssert.Equal("[Obsolete]\npublic void Run();\n",
            CSharp.Method("Run", m => m.Public().Attribute("Obsolete")).ToString());
        OutputAssert.Equal("[Obsolete]\npublic void Run()\n{\n}\n",
            CSharp.Method("Run", m => m.Public().Attribute("Obsolete").Body()).ToString());
        OutputAssert.Equal("[return: NotNull]\nstring Name() => \"\";\n",
            CSharp.Method("Name", m => m.Returns("string").Attribute("return: NotNull").Arrow("\"\"")).ToString());
    }

    [Fact]
    public void FieldAttribute_PrecedesDeclaration()
    {
        OutputAssert.Equal("[NonSerialized]\nprivate int count = 1;\n",
            CSharp.Field("int", "count", f => f.Private().Initializer("1").Attribute("NonSerialized")).ToString());
    }

    [Fact]
    public void ConstructorAttribute_PrecedesStandaloneAndContainedDeclarationOnce()
    {
        var constructor = CSharp.Constructor(k => k.Attribute("Obsolete"));
        var type = CSharp.Class("Service").Add(constructor);

        OutputAssert.Equal("[Obsolete]\n()\n{\n}\n", constructor.ToString());
        OutputAssert.Equal("class Service\n{\n    [Obsolete]\n    Service()\n    {\n    }\n}\n", type.ToString());
    }

    [Fact]
    public void PropertyAttributes_PrecedeEveryDeclarationFormOnce()
    {
        OutputAssert.Equal("[Browsable(false)]\npublic string Name { get; set; }\n",
            CSharp.Property("string", "Name", p => p.Public().Attribute("Browsable", "false")).ToString());
        OutputAssert.Equal("[Browsable(false)]\npublic int Value\n{\n    get\n    {\n        return value;\n    }\n    set\n    {\n        this.value = value;\n    }\n}\n",
            CSharp.Property("int", "Value", p => p.Public().Attribute("Browsable", "false")
                .Get(a => a.Body(b => b.Line("return value;")))
                .Set(a => a.Body(b => b.Line("this.value = value;")))).ToString());
        OutputAssert.Equal("[Browsable(false)]\npublic int Value => 1;\n",
            CSharp.Property("int", "Value", p => p.Public().Attribute("Browsable", "false").Arrow("1")).ToString());
        OutputAssert.Equal("[Browsable(false)]\npublic int Value { get; set; } = 1;\n",
            CSharp.Property("int", "Value", p => p.Public().Attribute("Browsable", "false").Initializer("1")).ToString());
    }

    [Fact]
    public void IndexerAttribute_PrecedesDeclaration()
    {
        OutputAssert.Equal("[IndexerName(\"Entry\")]\npublic int this[int index] => index;\n",
            CSharp.Indexer("int", x => x.Public().Parameter("int", "index").Arrow("index")
                .Attribute("IndexerName", "\"Entry\"")).ToString());
    }

    [Fact]
    public void EventAndDelegateAttributes_PrecedeDeclarations()
    {
        OutputAssert.Equal("[Obsolete]\npublic event Action Changed;\n",
            CSharp.Event("Action", "Changed", e => e.Public().Attribute("Obsolete")).ToString());
        OutputAssert.Equal("[Obsolete]\npublic delegate T Map<T>(T value) where T : class;\n",
            CSharp.Delegate("Map", d => d.Public().Returns("T").TypeParameter("T", t => t.Constraint("class"))
                .Parameter("T", "value").Attribute("Obsolete")).ToString());
    }

    [Fact]
    public void OperatorAttributes_PrecedeOrdinaryImplicitAndExplicitDeclarations()
    {
        OutputAssert.Equal("[Obsolete]\npublic static Value operator +(Value left, Value right) => left;\n",
            CSharp.Operator("Value", "+", o => o.Public().Static().Parameter("Value", "left").Parameter("Value", "right")
                .Arrow("left").Attribute("Obsolete")).ToString());
        OutputAssert.Equal("[Obsolete]\npublic static implicit operator int(Value value) => 0;\n",
            CSharp.ImplicitOperator("int", o => o.Public().Static().Parameter("Value", "value")
                .Arrow("0").Attribute("Obsolete")).ToString());
        OutputAssert.Equal("[Obsolete]\npublic static explicit operator Value(int value)\n{\n    return new Value();\n}\n",
            CSharp.ExplicitOperator("Value", o => o.Public().Static().Parameter("int", "value")
                .Body(b => b.Line("return new Value();")).Attribute("Obsolete")).ToString());
    }

    [Fact]
    public void Attributes_PreserveOrderDuplicatesAndRawArgumentsRegardlessOfCallOrder()
    {
        var builder = CSharp.Method("Run").Attribute("First").Public().Static()
            .Attribute("Second", "\"message\"", "true").Attribute("First")
            .Attribute("global::Example.Tag", "  1  ", " Name = \"tag\" ").Attribute("Empty", "").Attribute("");

        const string expected = "[First]\n[Second(\"message\", true)]\n[First]\n[global::Example.Tag(  1  ,  Name = \"tag\" )]\n[Empty()]\n[]\npublic static void Run();\n";
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
    }

    [Fact]
    public void ClassMembers_PreserveOrderSpacingAndNestedIndentation()
    {
        var builder = CSharp.Class("Service", c => c
            .Field("int", "count", f => f.Private().Attribute("NonSerialized"))
            .Property("string", "Name", p => p.Public().Attribute("Browsable", "false"))
            .Method("Run", m => m.Body())
            .Raw("// Between members")
            .Class("Nested", n => n.Method("Stop", m => m.Attribute("Obsolete").Body()))
            .Field("int", "last", f => f.Attribute("NonSerialized")));

        const string expected = "class Service\n{\n" +
            "    [NonSerialized]\n    private int count;\n\n" +
            "    [Browsable(false)]\n    public string Name { get; set; }\n\n" +
            "    void Run()\n    {\n    }\n\n" +
            "    // Between members\n\n" +
            "    class Nested\n    {\n        [Obsolete]\n        void Stop()\n        {\n        }\n    }\n\n" +
            "    [NonSerialized]\n    int last;\n}\n";
        OutputAssert.Equal(expected, builder.ToString());
    }

    [Fact]
    public void FileAndBlockNamespace_IndentDelegateAttributes()
    {
        var file = CSharp.File().Delegate("Top", d => d.Attribute("Obsolete"))
            .Namespace("App", n => n.Delegate("Nested", d => d.Attribute("Obsolete")));

        OutputAssert.Equal("[Obsolete]\ndelegate void Top();\n\nnamespace App\n{\n    [Obsolete]\n    delegate void Nested();\n}\n", file.ToString());
    }

    [Theory]
    [InlineData(false, "record")]
    [InlineData(true, "record struct")]
    public void RecordWithAttributedMember_RendersBlock(bool isStruct, string keyword)
    {
        var builder = isStruct ? CSharp.RecordStruct("Entry") : CSharp.Record("Entry");
        builder.Property("int", "Value", p => p.Attribute("Obsolete"));

        OutputAssert.Equal(keyword + " Entry\n{\n    [Obsolete]\n    int Value { get; set; }\n}\n", builder.ToString());
    }

    [Fact]
    public void AddedMembers_IncludeAttributesAddedBeforeAndAfterAdd()
    {
        var method = CSharp.Method("Run", m => m.Attribute("First"));
        var field = CSharp.Field("int", "value", f => f.Attribute("First"));
        var constructor = CSharp.Constructor(k => k.Attribute("First"));
        var property = CSharp.Property("int", "Value", p => p.Attribute("First"));
        var indexer = CSharp.Indexer("int", x => x.Parameter("int", "index").Get().Attribute("First"));
        var eventBuilder = CSharp.Event("Action", "Changed", e => e.Attribute("First"));
        var delegateBuilder = CSharp.Delegate("Handler", d => d.Attribute("First"));
        var operatorBuilder = CSharp.Operator("Service", "+", o => o.Parameter("Service", "value").Attribute("First"));
        var builder = CSharp.Class("Service").Add(method).Add(field).Add(constructor).Add(property)
            .Add(indexer).Add(eventBuilder).Add(delegateBuilder).Add(operatorBuilder);
        var file = CSharp.File().Add(delegateBuilder);
        var ns = CSharp.Namespace("App").Add(delegateBuilder);

        method.Attribute("Second");
        field.Attribute("Second");
        constructor.Attribute("Second");
        property.Attribute("Second");
        indexer.Attribute("Second");
        eventBuilder.Attribute("Second");
        delegateBuilder.Attribute("Second");
        operatorBuilder.Attribute("Second");

        const string expected = "class Service\n{\n" +
            "    [First]\n    [Second]\n    void Run();\n\n" +
            "    [First]\n    [Second]\n    int value;\n\n" +
            "    [First]\n    [Second]\n    Service()\n    {\n    }\n\n" +
            "    [First]\n    [Second]\n    int Value { get; set; }\n\n" +
            "    [First]\n    [Second]\n    int this[int index] { get; }\n\n" +
            "    [First]\n    [Second]\n    event Action Changed;\n\n" +
            "    [First]\n    [Second]\n    delegate void Handler();\n\n" +
            "    [First]\n    [Second]\n    Service operator +(Service value);\n}\n";
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal("[First]\n[Second]\ndelegate void Handler();\n", file.ToString());
        OutputAssert.Equal("namespace App\n{\n    [First]\n    [Second]\n    delegate void Handler();\n}\n", ns.ToString());
    }

    [Fact]
    public void Attribute_CapturesArgumentsAtCallTime()
    {
        var arguments = new[] { "\"original\"", "true" };
        var builder = CSharp.Method("Run").Attribute("Obsolete", arguments);

        arguments[0] = "\"changed\"";
        arguments[1] = null!;

        OutputAssert.Equal("[Obsolete(\"original\", true)]\nvoid Run();\n", builder.ToString());
    }

    [Fact]
    public void NullArguments_ThrowAtCallTimeWithoutAddingAttribute()
    {
        var builder = CSharp.Method("Run").Attribute("Existing");

        Assert.Throws<ArgumentNullException>("name", () => builder.Attribute((string)null!));
        Assert.Throws<ArgumentNullException>("arguments", () => builder.Attribute("X", (string[])null!));
        Assert.Throws<ArgumentNullException>("arguments", () => builder.Attribute("X", "a", null!));
        OutputAssert.Equal("[Existing]\nvoid Run();\n", builder.ToString());
    }

    [Fact]
    public void Attribute_ReturnsSameConcreteBuilder()
    {
        var method = CSharp.Method("Run");
        var field = CSharp.Field("int", "value");
        var constructor = CSharp.Constructor();
        var property = CSharp.Property("int", "Value");
        var indexer = CSharp.Indexer("int");
        var eventBuilder = CSharp.Event("Action", "Changed");
        var delegateBuilder = CSharp.Delegate("Handler");
        var operatorBuilder = CSharp.Operator("Value", "+");

        MethodBuilder returnedMethod = method.Attribute("X");
        FieldBuilder returnedField = field.Attribute("X");
        ConstructorBuilder returnedConstructor = constructor.Attribute("X");
        PropertyBuilder returnedProperty = property.Attribute("X");
        IndexerBuilder returnedIndexer = indexer.Attribute("X");
        EventBuilder returnedEvent = eventBuilder.Attribute("X");
        DelegateBuilder returnedDelegate = delegateBuilder.Attribute("X");
        OperatorBuilder returnedOperator = operatorBuilder.Attribute("X");

        Assert.Same(method, returnedMethod);
        Assert.Same(field, returnedField);
        Assert.Same(constructor, returnedConstructor);
        Assert.Same(property, returnedProperty);
        Assert.Same(indexer, returnedIndexer);
        Assert.Same(eventBuilder, returnedEvent);
        Assert.Same(delegateBuilder, returnedDelegate);
        Assert.Same(operatorBuilder, returnedOperator);
    }

    [Fact]
    public void CompleteFile_WithAttributesOnAllMemberKinds_Compiles()
    {
        var file = CSharp.File().Using("System").Namespace("App", n => n
            .Delegate("Handler", d => d.Public().Attribute("Obsolete"))
            .Interface("IService", i => i.Public().Method("Run", m => m.Attribute("Obsolete")))
            .Class("Service", c => c.Public().BaseType("IService")
                .Field("int", "count", f => f.Private().Attribute("NonSerialized"))
                .Constructor(k => k.Public().Attribute("Obsolete", "\"Use the factory.\"").Body())
                .Method("Run", m => m.Public().Attribute("Obsolete").Body())
                .Method("Trace", m => m.Public().Attribute("System.Diagnostics.Conditional", "\"DEBUG\"").Body())
                .Method("Name", m => m.Public().Returns("string")
                    .Attribute("return: System.Diagnostics.CodeAnalysis.NotNull").Arrow("\"\""))
                .Property("int", "Count", p => p.Public().Attribute("System.ComponentModel.Browsable", "false"))
                .Property("int", "Value", p => p.Public().Attribute("System.ComponentModel.Browsable", "false")
                    .Get(a => a.Body(b => b.Line("return count;")))
                    .Set(a => a.Body(b => b.Line("count = value;"))))
                .Indexer("int", x => x.Public().Parameter("int", "index")
                    .Attribute("System.Runtime.CompilerServices.IndexerName", "\"Entry\"").Arrow("count + index"))
                .Event("Action", "Changed", e => e.Public().Attribute("Obsolete"))
                .Delegate("NestedHandler", d => d.Public().Attribute("Obsolete")))
            .Struct("Value", s => s.Public()
                .Operator("Value", "+", o => o.Public().Static().Parameter("Value", "left").Parameter("Value", "right")
                    .Attribute("Obsolete").Arrow("left"))
                .ImplicitOperator("int", o => o.Public().Static().Parameter("Value", "value")
                    .Attribute("Obsolete").Arrow("0"))));

        CompilationAssert.NoErrors(file.ToString());
    }
}
