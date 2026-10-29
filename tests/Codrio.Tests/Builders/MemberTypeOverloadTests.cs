using System;
using System.Collections.Generic;
using Codrio.Tests.Fixtures;
using Xunit;

namespace Codrio.Tests;

public class MemberTypeOverloadTests
{
    [Fact]
    public void CSharpFields_AllOverloadsRenderExpectedNames()
    {
        AssertFieldNames<int>("int");
        AssertFieldNames<List<string>>("global::System.Collections.Generic.List<string>");
        AssertFieldNames<Outer<int>.Inner<string>>("global::Codrio.Tests.Fixtures.Outer<int>.Inner<string>");
        AssertFieldNames<int[,][]>("int[,][]");
        AssertFieldNames<int?>("global::System.Nullable<int>");
    }

    private static void AssertFieldNames<T>(string typeName)
    {
        var expected = typeName + " @class;\n";
        OutputAssert.Equal(expected, CSharp.Field(typeName, "class").ToString());
        OutputAssert.Equal(expected, CSharp.Field(typeof(T), "class").ToString());
        OutputAssert.Equal(expected, CSharp.Field<T>("class").ToString());
        OutputAssert.Equal(expected, CSharp.Field(typeName, "class", _ => { }).ToString());
        OutputAssert.Equal(expected, CSharp.Field(typeof(T), "class", _ => { }).ToString());
        OutputAssert.Equal(expected, CSharp.Field<T>("class", _ => { }).ToString());
    }

    [Fact]
    public void CSharpProperties_AllOverloadsRenderExpectedNames()
    {
        AssertPropertyNames<string>("string");
        AssertPropertyNames<List<string>>("global::System.Collections.Generic.List<string>");
        AssertPropertyNames<Outer<int>.Leaf>("global::Codrio.Tests.Fixtures.Outer<int>.Leaf");
        AssertPropertyNames<string[][]>("string[][]");
        AssertPropertyNames<int?>("global::System.Nullable<int>");
    }

    private static void AssertPropertyNames<T>(string typeName)
    {
        var expected = typeName + " @class { get; set; }\n";
        OutputAssert.Equal(expected, CSharp.Property(typeName, "class").ToString());
        OutputAssert.Equal(expected, CSharp.Property(typeof(T), "class").ToString());
        OutputAssert.Equal(expected, CSharp.Property<T>("class").ToString());
        OutputAssert.Equal(expected, CSharp.Property(typeName, "class", _ => { }).ToString());
        OutputAssert.Equal(expected, CSharp.Property(typeof(T), "class", _ => { }).ToString());
        OutputAssert.Equal(expected, CSharp.Property<T>("class", _ => { }).ToString());
    }

    [Fact]
    public void CSharpEvents_AllOverloadsRenderExpectedNames()
    {
        AssertEventNames<EventHandler>("global::System.EventHandler");
        AssertEventNames<Action<Outer<int>.Inner<string>[], int?>>("global::System.Action<global::Codrio.Tests.Fixtures.Outer<int>.Inner<string>[], global::System.Nullable<int>>");
    }

    private static void AssertEventNames<T>(string typeName)
    {
        var expected = "event " + typeName + " @event;\n";
        OutputAssert.Equal(expected, CSharp.Event(typeName, "event").ToString());
        OutputAssert.Equal(expected, CSharp.Event(typeof(T), "event").ToString());
        OutputAssert.Equal(expected, CSharp.Event<T>("event").ToString());
        OutputAssert.Equal(expected, CSharp.Event(typeName, "event", _ => { }).ToString());
        OutputAssert.Equal(expected, CSharp.Event(typeof(T), "event", _ => { }).ToString());
        OutputAssert.Equal(expected, CSharp.Event<T>("event", _ => { }).ToString());
    }

    [Fact]
    public void CSharpConfiguration_RunsImmediatelyOnce()
    {
        var calls = 0;
        var field = CSharp.Field(typeof(int), "count", f => { calls++; f.Private(); });
        Assert.Equal(1, calls);
        var genericField = CSharp.Field<int>("count", f => { calls++; f.Private(); });
        Assert.Equal(2, calls);
        var property = CSharp.Property(typeof(string), "Name", p => { calls++; p.Public(); });
        Assert.Equal(3, calls);
        var genericProperty = CSharp.Property<string>("Name", p => { calls++; p.Public(); });
        Assert.Equal(4, calls);
        var eventBuilder = CSharp.Event(typeof(EventHandler), "Changed", e => { calls++; e.Public(); });
        Assert.Equal(5, calls);
        var genericEvent = CSharp.Event<EventHandler>("Changed", e => { calls++; e.Public(); });
        Assert.Equal(6, calls);

        OutputAssert.Equal("private int count;\n", field.ToString());
        OutputAssert.Equal("private int count;\n", genericField.ToString());
        OutputAssert.Equal("public string Name { get; set; }\n", property.ToString());
        OutputAssert.Equal("public string Name { get; set; }\n", genericProperty.ToString());
        OutputAssert.Equal("public event global::System.EventHandler Changed;\n", eventBuilder.ToString());
        OutputAssert.Equal("public event global::System.EventHandler Changed;\n", genericEvent.ToString());
        Assert.Equal(6, calls);
    }

    [Fact]
    public void TypeFields_AllOverloadsPreserveOrderAndReturnSameBuilder()
    {
        AssertTypeFields(CSharp.Class("Sample"), "class");
        AssertTypeFields(CSharp.Struct("Sample"), "struct");
        AssertTypeFields(CSharp.Record("Sample"), "record");
        AssertTypeFields(CSharp.RecordStruct("Sample"), "record struct");
    }

    private static void AssertTypeFields<TSelf>(TSelf builder, string keyword)
        where TSelf : ClassOrStructBuilder<TSelf>
    {
        var typeCalls = 0;
        var genericCalls = 0;
        Assert.Same(builder, builder.Field(typeof(int), "first"));
        Assert.Same(builder, builder.Field(typeof(string), "second", f =>
        {
            typeCalls++;
            f.Private();
        }));
        Assert.Equal(1, typeCalls);
        Assert.Same(builder, builder.Field<Outer<int>.Inner<string>>("third"));
        Assert.Same(builder, builder.Field<int?>("fourth", f =>
        {
            genericCalls++;
            f.Public();
        }));
        Assert.Equal(1, genericCalls);
        OutputAssert.Equal(keyword + " Sample\n{\n    int first;\n\n    private string second;\n\n    global::Codrio.Tests.Fixtures.Outer<int>.Inner<string> third;\n\n    public global::System.Nullable<int> fourth;\n}\n", builder.ToString());
        Assert.Equal(1, typeCalls);
        Assert.Equal(1, genericCalls);
    }

    [Fact]
    public void TypePropertiesAndEvents_AllOverloadsPreserveOrderAndReturnSameBuilder()
    {
        AssertTypePropertiesAndEvents(CSharp.Class("Sample"), "class");
        AssertTypePropertiesAndEvents(CSharp.Struct("Sample"), "struct");
        AssertTypePropertiesAndEvents(CSharp.Interface("Sample"), "interface");
        AssertTypePropertiesAndEvents(CSharp.Record("Sample"), "record");
        AssertTypePropertiesAndEvents(CSharp.RecordStruct("Sample"), "record struct");
    }

    private static void AssertTypePropertiesAndEvents<TSelf>(TSelf builder, string keyword)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        var calls = 0;
        Assert.Same(builder, builder.Property(typeof(int), "First"));
        Assert.Same(builder, builder.Property(typeof(string), "Second", p => { calls++; p.Public(); }));
        Assert.Equal(1, calls);
        Assert.Same(builder, builder.Property<int[]>("Third"));
        Assert.Same(builder, builder.Property<int?>("Fourth", p => { calls++; p.Public(); }));
        Assert.Equal(2, calls);
        Assert.Same(builder, builder.Event(typeof(EventHandler), "FirstChanged"));
        Assert.Same(builder, builder.Event(typeof(EventHandler), "SecondChanged", e => { calls++; e.Public(); }));
        Assert.Equal(3, calls);
        Assert.Same(builder, builder.Event<EventHandler>("ThirdChanged"));
        Assert.Same(builder, builder.Event<EventHandler>("FourthChanged", e => { calls++; e.Public(); }));
        Assert.Equal(4, calls);
        OutputAssert.Equal(keyword + " Sample\n{\n    int First { get; set; }\n\n    public string Second { get; set; }\n\n    int[] Third { get; set; }\n\n    public global::System.Nullable<int> Fourth { get; set; }\n\n    event global::System.EventHandler FirstChanged;\n\n    public event global::System.EventHandler SecondChanged;\n\n    event global::System.EventHandler ThirdChanged;\n\n    public event global::System.EventHandler FourthChanged;\n}\n", builder.ToString());
        Assert.Equal(4, calls);
    }

    [Fact]
    public void TypeFields_StringTypeAndGenericOverloadsRenderSameName()
    {
        AssertTypeFieldNames(() => CSharp.Class("Sample"), "class");
        AssertTypeFieldNames(() => CSharp.Struct("Sample"), "struct");
        AssertTypeFieldNames(() => CSharp.Record("Sample"), "record");
        AssertTypeFieldNames(() => CSharp.RecordStruct("Sample"), "record struct");
    }

    private static void AssertTypeFieldNames<TSelf>(Func<TSelf> create, string keyword)
        where TSelf : ClassOrStructBuilder<TSelf>
    {
        const string typeName = "global::System.Collections.Generic.List<string>";
        var expected = keyword + " Sample\n{\n    global::System.Collections.Generic.List<string> items;\n}\n";
        OutputAssert.Equal(expected, create().Field(typeName, "items").ToString());
        OutputAssert.Equal(expected, create().Field(typeof(List<string>), "items").ToString());
        OutputAssert.Equal(expected, create().Field<List<string>>("items").ToString());
        OutputAssert.Equal(expected, create().Field(typeName, "items", _ => { }).ToString());
        OutputAssert.Equal(expected, create().Field(typeof(List<string>), "items", _ => { }).ToString());
        OutputAssert.Equal(expected, create().Field<List<string>>("items", _ => { }).ToString());
        OutputAssert.Equal(expected, create().Add(CSharp.Field<List<string>>("items")).ToString());
    }

    [Fact]
    public void TypePropertiesAndEvents_StringTypeAndGenericOverloadsRenderSameName()
    {
        AssertTypePropertyAndEventNames(() => CSharp.Class("Sample"), "class");
        AssertTypePropertyAndEventNames(() => CSharp.Struct("Sample"), "struct");
        AssertTypePropertyAndEventNames(() => CSharp.Interface("Sample"), "interface");
        AssertTypePropertyAndEventNames(() => CSharp.Record("Sample"), "record");
        AssertTypePropertyAndEventNames(() => CSharp.RecordStruct("Sample"), "record struct");
    }

    private static void AssertTypePropertyAndEventNames<TSelf>(Func<TSelf> create, string keyword)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        const string propertyType = "global::System.Collections.Generic.List<string>";
        var propertyExpected = keyword + " Sample\n{\n    global::System.Collections.Generic.List<string> Items { get; set; }\n}\n";
        OutputAssert.Equal(propertyExpected, create().Property(propertyType, "Items").ToString());
        OutputAssert.Equal(propertyExpected, create().Property(typeof(List<string>), "Items").ToString());
        OutputAssert.Equal(propertyExpected, create().Property<List<string>>("Items").ToString());
        OutputAssert.Equal(propertyExpected, create().Property(propertyType, "Items", _ => { }).ToString());
        OutputAssert.Equal(propertyExpected, create().Property(typeof(List<string>), "Items", _ => { }).ToString());
        OutputAssert.Equal(propertyExpected, create().Property<List<string>>("Items", _ => { }).ToString());

        const string eventType = "global::System.EventHandler";
        var eventExpected = keyword + " Sample\n{\n    event global::System.EventHandler Changed;\n}\n";
        OutputAssert.Equal(eventExpected, create().Event(eventType, "Changed").ToString());
        OutputAssert.Equal(eventExpected, create().Event(typeof(EventHandler), "Changed").ToString());
        OutputAssert.Equal(eventExpected, create().Event<EventHandler>("Changed").ToString());
        OutputAssert.Equal(eventExpected, create().Event(eventType, "Changed", _ => { }).ToString());
        OutputAssert.Equal(eventExpected, create().Event(typeof(EventHandler), "Changed", _ => { }).ToString());
        OutputAssert.Equal(eventExpected, create().Event<EventHandler>("Changed", _ => { }).ToString());
    }

    [Fact]
    public void CSharpMembers_InvalidArgumentsThrowBeforeConfiguration()
    {
        AssertInvalidArguments<FieldBuilder>(
            (type, name) => CSharp.Field(type, name), (type, name, configure) => CSharp.Field(type, name, configure),
            name => CSharp.Field<int>(name), (name, configure) => CSharp.Field<int>(name, configure));
        AssertInvalidArguments<PropertyBuilder>(
            (type, name) => CSharp.Property(type, name), (type, name, configure) => CSharp.Property(type, name, configure),
            name => CSharp.Property<int>(name), (name, configure) => CSharp.Property<int>(name, configure));
        AssertInvalidArguments<EventBuilder>(
            (type, name) => CSharp.Event(type, name), (type, name, configure) => CSharp.Event(type, name, configure),
            name => CSharp.Event<EventHandler>(name), (name, configure) => CSharp.Event<EventHandler>(name, configure));
    }

    [Fact]
    public void TypeFields_InvalidArgumentsPreserveMembers()
    {
        AssertInvalidTypeFields(CSharp.Class("Sample"));
        AssertInvalidTypeFields(CSharp.Struct("Sample"));
        AssertInvalidTypeFields(CSharp.Record("Sample"));
        AssertInvalidTypeFields(CSharp.RecordStruct("Sample"));
    }

    private static void AssertInvalidTypeFields<TSelf>(TSelf builder)
        where TSelf : ClassOrStructBuilder<TSelf>
    {
        builder.Field("int", "existing");
        var before = builder.ToString();
        AssertInvalidArguments<FieldBuilder>(
            (type, name) => builder.Field(type, name), (type, name, configure) => builder.Field(type, name, configure),
            name => builder.Field<int>(name), (name, configure) => builder.Field<int>(name, configure));
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void TypePropertiesAndEvents_InvalidArgumentsPreserveMembers()
    {
        AssertInvalidTypePropertiesAndEvents(CSharp.Class("Sample"));
        AssertInvalidTypePropertiesAndEvents(CSharp.Struct("Sample"));
        AssertInvalidTypePropertiesAndEvents(CSharp.Interface("Sample"));
        AssertInvalidTypePropertiesAndEvents(CSharp.Record("Sample"));
        AssertInvalidTypePropertiesAndEvents(CSharp.RecordStruct("Sample"));
    }

    private static void AssertInvalidTypePropertiesAndEvents<TSelf>(TSelf builder)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        builder.Property("int", "Existing").Event("System.EventHandler", "ExistingChanged");
        var before = builder.ToString();
        AssertInvalidArguments<PropertyBuilder>(
            (type, name) => builder.Property(type, name), (type, name, configure) => builder.Property(type, name, configure),
            name => builder.Property<int>(name), (name, configure) => builder.Property<int>(name, configure));
        OutputAssert.Equal(before, builder.ToString());
        AssertInvalidArguments<EventBuilder>(
            (type, name) => builder.Event(type, name), (type, name, configure) => builder.Event(type, name, configure),
            name => builder.Event<EventHandler>(name), (name, configure) => builder.Event<EventHandler>(name, configure));
        OutputAssert.Equal(before, builder.ToString());
    }

    private static void AssertInvalidArguments<TBuilder>(
        Action<Type, string> typeOverload, Action<Type, string, Action<TBuilder>> configuredTypeOverload,
        Action<string> genericOverload, Action<string, Action<TBuilder>> configuredGenericOverload)
    {
        var calls = 0;
        Action<TBuilder> configure = _ => calls++;
        Assert.Throws<ArgumentException>("type", () => typeOverload(typeof(List<>), "value"));
        Assert.Throws<ArgumentException>("type", () => configuredTypeOverload(typeof(List<>), "value", configure));
        Assert.Throws<ArgumentException>("type", () => typeOverload(typeof(List<>), "1x"));
        Assert.Throws<ArgumentException>("type", () => configuredTypeOverload(typeof(List<>), null!, null!));
        Assert.Throws<ArgumentException>("name", () => typeOverload(typeof(int), "1x"));
        Assert.Throws<ArgumentException>("name", () => configuredTypeOverload(typeof(int), "1x", configure));
        Assert.Throws<ArgumentException>("name", () => genericOverload("1x"));
        Assert.Throws<ArgumentException>("name", () => configuredGenericOverload("1x", configure));
        Assert.Throws<ArgumentNullException>("type", () => typeOverload((Type)null!, "value"));
        Assert.Throws<ArgumentNullException>("type", () => configuredTypeOverload((Type)null!, "value", configure));
        Assert.Throws<ArgumentNullException>("type", () => configuredTypeOverload((Type)null!, null!, null!));
        Assert.Throws<ArgumentNullException>("name", () => typeOverload(typeof(int), null!));
        Assert.Throws<ArgumentNullException>("name", () => configuredTypeOverload(typeof(int), null!, configure));
        Assert.Throws<ArgumentNullException>("name", () => genericOverload(null!));
        Assert.Throws<ArgumentNullException>("name", () => configuredGenericOverload(null!, configure));
        Assert.Throws<ArgumentNullException>("configure", () => configuredTypeOverload(typeof(int), "value", null!));
        Assert.Throws<ArgumentNullException>("configure", () => configuredGenericOverload("value", null!));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Members_RenderExamples()
    {
        OutputAssert.Equal("int count;\n", CSharp.Field<int>("count").ToString());
        OutputAssert.Equal("class Service\n{\n    private global::System.Collections.Generic.List<string> items;\n\n    public string Name { get; set; }\n\n    public event global::System.EventHandler Changed;\n}\n",
            CSharp.Class("Service", c => c
                .Field(typeof(List<string>), "items", f => f.Private())
                .Property<string>("Name", p => p.Public())
                .Event<EventHandler>("Changed", e => e.Public())).ToString());
    }

    [Fact]
    public void CompleteFile_WithMemberTypeOverloads_Compiles()
    {
        var file = CSharp.File().Namespace("App", n => n
            .Class("Service", c => c
                .Field(typeof(int), "Limit", f => f.Public().Const().Initializer("10"))
                .Field<List<string>>("items", f => f.Private().Initializer("new global::System.Collections.Generic.List<string>()"))
                .Field(typeof(Outer<int>.Inner<string>), "nested")
                .Field<int[,][]>("arrays")
                .Add(CSharp.Field<int?>("optional"))
                .Property(typeof(string), "Name", p => p.Public().Initializer("\"\""))
                .Property<Outer<int>.Leaf>("Leaf", p => p.Public())
                .Event(typeof(EventHandler), "Changed", e => e.Public())
                .Event<Action<Outer<int>.Inner<string>[], int?>>("Received", e => e.Public()))
            .Struct("Value", s => s
                .Field<int>("Limit", f => f.Public().Const().Initializer("20"))
                .Field(typeof(List<string>), "items")
                .Field<int?>("optional")
                .Property<int?>("Count", p => p.Public().Initializer("1"))
                .Property(typeof(CodrioGlobalFixture[]), "Items", p => p.Public())
                .Event<EventHandler>("Changed", e => e.Public())
                .Event(typeof(Action<int[]>), "Received", e => e.Public())
                .Constructor(c => c.Public()))
            .Interface("IService", i => i
                .Property(typeof(List<string>), "Items")
                .Property<int?>("Count")
                .Event(typeof(EventHandler), "Changed")
                .Event<Action<Outer<int>.Leaf>>("Received"))
            .Record("Entry", r => r
                .Field(typeof(CodrioGlobalFixture), "item")
                .Field<int?>("optional")));

        CompilationAssert.NoErrors(file.ToString() + "\n" + TypeFixtures.Source);
    }

    [Fact]
    public void CSharpIndexers_AllOverloadsRenderExpectedNames()
    {
        AssertIndexerNames<int>("int");
        AssertIndexerNames<TimeSpan>("global::System.TimeSpan");
        AssertIndexerNames<Outer<int>.Inner<string>>("global::Codrio.Tests.Fixtures.Outer<int>.Inner<string>");
        AssertIndexerNames<int[,][]>("int[,][]");
        AssertIndexerNames<int?>("global::System.Nullable<int>");
    }

    private static void AssertIndexerNames<T>(string typeName)
    {
        var expected = "" + typeName + " this[] { get; set; }\n";
        OutputAssert.Equal(expected, CSharp.Indexer(typeName).ToString());
        OutputAssert.Equal(expected, CSharp.Indexer(typeof(T)).ToString());
        OutputAssert.Equal(expected, CSharp.Indexer<T>().ToString());
        var calls = 0;
        var fromString = CSharp.Indexer(typeName, b => { calls++; b.Public(); });
        Assert.Equal(1, calls);
        var fromType = CSharp.Indexer(typeof(T), b => { calls++; b.Public(); });
        Assert.Equal(2, calls);
        var fromGeneric = CSharp.Indexer<T>(b => { calls++; b.Public(); });
        Assert.Equal(3, calls);
        OutputAssert.Equal("public " + expected, fromString.ToString());
        OutputAssert.Equal("public " + expected, fromType.ToString());
        OutputAssert.Equal("public " + expected, fromGeneric.ToString());
        Assert.Equal(3, calls);
    }

    [Fact]
    public void CSharpOperators_AllOverloadsRenderExpectedNames()
    {
        AssertOperatorNames<int>("int");
        AssertOperatorNames<TimeSpan>("global::System.TimeSpan");
        AssertOperatorNames<Outer<int>.Inner<string>>("global::Codrio.Tests.Fixtures.Outer<int>.Inner<string>");
        AssertOperatorNames<int[,][]>("int[,][]");
        AssertOperatorNames<int?>("global::System.Nullable<int>");
    }

    private static void AssertOperatorNames<T>(string typeName)
    {
        var expected = "" + typeName + " operator +();\n";
        OutputAssert.Equal(expected, CSharp.Operator(typeName, "+").ToString());
        OutputAssert.Equal(expected, CSharp.Operator(typeof(T), "+").ToString());
        OutputAssert.Equal(expected, CSharp.Operator<T>("+").ToString());
        var calls = 0;
        var fromString = CSharp.Operator(typeName, "+", b => { calls++; b.Public(); });
        Assert.Equal(1, calls);
        var fromType = CSharp.Operator(typeof(T), "+", b => { calls++; b.Public(); });
        Assert.Equal(2, calls);
        var fromGeneric = CSharp.Operator<T>("+", b => { calls++; b.Public(); });
        Assert.Equal(3, calls);
        OutputAssert.Equal("public " + expected, fromString.ToString());
        OutputAssert.Equal("public " + expected, fromType.ToString());
        OutputAssert.Equal("public " + expected, fromGeneric.ToString());
        Assert.Equal(3, calls);
    }

    [Fact]
    public void CSharpImplicitOperators_AllOverloadsRenderExpectedNames()
    {
        AssertImplicitOperatorNames<int>("int");
        AssertImplicitOperatorNames<TimeSpan>("global::System.TimeSpan");
        AssertImplicitOperatorNames<Outer<int>.Inner<string>>("global::Codrio.Tests.Fixtures.Outer<int>.Inner<string>");
        AssertImplicitOperatorNames<int[,][]>("int[,][]");
        AssertImplicitOperatorNames<int?>("global::System.Nullable<int>");
    }

    private static void AssertImplicitOperatorNames<T>(string typeName)
    {
        var expected = "implicit operator " + typeName + "();\n";
        OutputAssert.Equal(expected, CSharp.ImplicitOperator(typeName).ToString());
        OutputAssert.Equal(expected, CSharp.ImplicitOperator(typeof(T)).ToString());
        OutputAssert.Equal(expected, CSharp.ImplicitOperator<T>().ToString());
        var calls = 0;
        var fromString = CSharp.ImplicitOperator(typeName, b => { calls++; b.Public(); });
        Assert.Equal(1, calls);
        var fromType = CSharp.ImplicitOperator(typeof(T), b => { calls++; b.Public(); });
        Assert.Equal(2, calls);
        var fromGeneric = CSharp.ImplicitOperator<T>(b => { calls++; b.Public(); });
        Assert.Equal(3, calls);
        OutputAssert.Equal("public " + expected, fromString.ToString());
        OutputAssert.Equal("public " + expected, fromType.ToString());
        OutputAssert.Equal("public " + expected, fromGeneric.ToString());
        Assert.Equal(3, calls);
    }

    [Fact]
    public void CSharpExplicitOperators_AllOverloadsRenderExpectedNames()
    {
        AssertExplicitOperatorNames<int>("int");
        AssertExplicitOperatorNames<TimeSpan>("global::System.TimeSpan");
        AssertExplicitOperatorNames<Outer<int>.Inner<string>>("global::Codrio.Tests.Fixtures.Outer<int>.Inner<string>");
        AssertExplicitOperatorNames<int[,][]>("int[,][]");
        AssertExplicitOperatorNames<int?>("global::System.Nullable<int>");
    }

    private static void AssertExplicitOperatorNames<T>(string typeName)
    {
        var expected = "explicit operator " + typeName + "();\n";
        OutputAssert.Equal(expected, CSharp.ExplicitOperator(typeName).ToString());
        OutputAssert.Equal(expected, CSharp.ExplicitOperator(typeof(T)).ToString());
        OutputAssert.Equal(expected, CSharp.ExplicitOperator<T>().ToString());
        var calls = 0;
        var fromString = CSharp.ExplicitOperator(typeName, b => { calls++; b.Public(); });
        Assert.Equal(1, calls);
        var fromType = CSharp.ExplicitOperator(typeof(T), b => { calls++; b.Public(); });
        Assert.Equal(2, calls);
        var fromGeneric = CSharp.ExplicitOperator<T>(b => { calls++; b.Public(); });
        Assert.Equal(3, calls);
        OutputAssert.Equal("public " + expected, fromString.ToString());
        OutputAssert.Equal("public " + expected, fromType.ToString());
        OutputAssert.Equal("public " + expected, fromGeneric.ToString());
        Assert.Equal(3, calls);
    }

    [Fact]
    public void TypeIndexersAndOperators_AllOverloadsPreserveOrderAndReturnSameBuilder()
    {
        AssertTypeIndexersAndOperators(CSharp.Class("Sample"), "class");
        AssertTypeIndexersAndOperators(CSharp.Struct("Sample"), "struct");
        AssertTypeIndexersAndOperators(CSharp.Interface("Sample"), "interface");
        AssertTypeIndexersAndOperators(CSharp.Record("Sample"), "record");
        AssertTypeIndexersAndOperators(CSharp.RecordStruct("Sample"), "record struct");
    }

    private static void AssertTypeIndexersAndOperators<TSelf>(TSelf builder, string keyword)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        var calls = 0;
        Assert.Same(builder, builder.Indexer("int"));
        Assert.Same(builder, builder.Indexer("int", b => { calls++; b.Public(); }));
        Assert.Equal(1, calls);
        Assert.Same(builder, builder.Indexer(typeof(int)));
        Assert.Same(builder, builder.Indexer(typeof(int), b => { calls++; b.Public(); }));
        Assert.Equal(2, calls);
        Assert.Same(builder, builder.Indexer<int>());
        Assert.Same(builder, builder.Indexer<int>(b => { calls++; b.Public(); }));
        Assert.Equal(3, calls);
        Assert.Same(builder, builder.Add(CSharp.Indexer(typeof(int))));
        Assert.Same(builder, builder.Add(CSharp.Indexer<int>()));
        Assert.Same(builder, builder.Operator("int", "+"));
        Assert.Same(builder, builder.Operator("int", "+", b => { calls++; b.Public(); }));
        Assert.Equal(4, calls);
        Assert.Same(builder, builder.Operator(typeof(int), "+"));
        Assert.Same(builder, builder.Operator(typeof(int), "+", b => { calls++; b.Public(); }));
        Assert.Equal(5, calls);
        Assert.Same(builder, builder.Operator<int>("+"));
        Assert.Same(builder, builder.Operator<int>("+", b => { calls++; b.Public(); }));
        Assert.Equal(6, calls);
        Assert.Same(builder, builder.Add(CSharp.Operator(typeof(int), "+")));
        Assert.Same(builder, builder.Add(CSharp.Operator<int>("+")));
        Assert.Same(builder, builder.ImplicitOperator("int"));
        Assert.Same(builder, builder.ImplicitOperator("int", b => { calls++; b.Public(); }));
        Assert.Equal(7, calls);
        Assert.Same(builder, builder.ImplicitOperator(typeof(int)));
        Assert.Same(builder, builder.ImplicitOperator(typeof(int), b => { calls++; b.Public(); }));
        Assert.Equal(8, calls);
        Assert.Same(builder, builder.ImplicitOperator<int>());
        Assert.Same(builder, builder.ImplicitOperator<int>(b => { calls++; b.Public(); }));
        Assert.Equal(9, calls);
        Assert.Same(builder, builder.Add(CSharp.ImplicitOperator(typeof(int))));
        Assert.Same(builder, builder.Add(CSharp.ImplicitOperator<int>()));
        Assert.Same(builder, builder.ExplicitOperator("int"));
        Assert.Same(builder, builder.ExplicitOperator("int", b => { calls++; b.Public(); }));
        Assert.Equal(10, calls);
        Assert.Same(builder, builder.ExplicitOperator(typeof(int)));
        Assert.Same(builder, builder.ExplicitOperator(typeof(int), b => { calls++; b.Public(); }));
        Assert.Equal(11, calls);
        Assert.Same(builder, builder.ExplicitOperator<int>());
        Assert.Same(builder, builder.ExplicitOperator<int>(b => { calls++; b.Public(); }));
        Assert.Equal(12, calls);
        Assert.Same(builder, builder.Add(CSharp.ExplicitOperator(typeof(int))));
        Assert.Same(builder, builder.Add(CSharp.ExplicitOperator<int>()));
        OutputAssert.Equal(keyword + " Sample\n{\n" +
            "    int this[] { get; set; }\n\n" +
            "    public int this[] { get; set; }\n\n" +
            "    int this[] { get; set; }\n\n" +
            "    public int this[] { get; set; }\n\n" +
            "    int this[] { get; set; }\n\n" +
            "    public int this[] { get; set; }\n\n" +
            "    int this[] { get; set; }\n\n" +
            "    int this[] { get; set; }\n\n" +
            "    int operator +();\n\n" +
            "    public int operator +();\n\n" +
            "    int operator +();\n\n" +
            "    public int operator +();\n\n" +
            "    int operator +();\n\n" +
            "    public int operator +();\n\n" +
            "    int operator +();\n\n" +
            "    int operator +();\n\n" +
            "    implicit operator int();\n\n" +
            "    public implicit operator int();\n\n" +
            "    implicit operator int();\n\n" +
            "    public implicit operator int();\n\n" +
            "    implicit operator int();\n\n" +
            "    public implicit operator int();\n\n" +
            "    implicit operator int();\n\n" +
            "    implicit operator int();\n\n" +
            "    explicit operator int();\n\n" +
            "    public explicit operator int();\n\n" +
            "    explicit operator int();\n\n" +
            "    public explicit operator int();\n\n" +
            "    explicit operator int();\n\n" +
            "    public explicit operator int();\n\n" +
            "    explicit operator int();\n\n" +
            "    explicit operator int();\n}\n", builder.ToString());
        Assert.Equal(12, calls);
    }

    [Fact]
    public void CSharpIndexersAndOperators_InvalidArgumentsDoNotConfigure()
    {
        AssertInvalidIndexerOrOperatorArguments<IndexerBuilder>("type",
            type => CSharp.Indexer(type), (type, configure) => CSharp.Indexer(type, configure),
            configure => CSharp.Indexer<int>(configure));
        AssertInvalidIndexerOrOperatorArguments<OperatorBuilder>("returnType",
            type => CSharp.Operator(type, "+"), (type, configure) => CSharp.Operator(type, "+", configure),
            configure => CSharp.Operator<int>("+", configure));
        AssertInvalidIndexerOrOperatorArguments<OperatorBuilder>("type",
            type => CSharp.ImplicitOperator(type), (type, configure) => CSharp.ImplicitOperator(type, configure),
            configure => CSharp.ImplicitOperator<int>(configure));
        AssertInvalidIndexerOrOperatorArguments<OperatorBuilder>("type",
            type => CSharp.ExplicitOperator(type), (type, configure) => CSharp.ExplicitOperator(type, configure),
            configure => CSharp.ExplicitOperator<int>(configure));
        AssertInvalidOperatorSymbols(
            (type, symbol) => CSharp.Operator(type, symbol),
            (type, symbol, configure) => CSharp.Operator(type, symbol, configure),
            symbol => CSharp.Operator<int>(symbol),
            (symbol, configure) => CSharp.Operator<int>(symbol, configure));
    }

    [Fact]
    public void TypeIndexersAndOperators_InvalidArgumentsPreserveMembers()
    {
        AssertInvalidTypeIndexersAndOperators(CSharp.Class("Sample"));
        AssertInvalidTypeIndexersAndOperators(CSharp.Struct("Sample"));
        AssertInvalidTypeIndexersAndOperators(CSharp.Interface("Sample"));
        AssertInvalidTypeIndexersAndOperators(CSharp.Record("Sample"));
        AssertInvalidTypeIndexersAndOperators(CSharp.RecordStruct("Sample"));
    }

    private static void AssertInvalidTypeIndexersAndOperators<TSelf>(TSelf builder)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        builder.Property("int", "Existing");
        var before = builder.ToString();
        AssertInvalidIndexerOrOperatorArguments<IndexerBuilder>("type",
            type => builder.Indexer(type), (type, configure) => builder.Indexer(type, configure),
            configure => builder.Indexer<int>(configure));
        OutputAssert.Equal(before, builder.ToString());
        AssertInvalidIndexerOrOperatorArguments<OperatorBuilder>("returnType",
            type => builder.Operator(type, "+"), (type, configure) => builder.Operator(type, "+", configure),
            configure => builder.Operator<int>("+", configure));
        OutputAssert.Equal(before, builder.ToString());
        AssertInvalidIndexerOrOperatorArguments<OperatorBuilder>("type",
            type => builder.ImplicitOperator(type), (type, configure) => builder.ImplicitOperator(type, configure),
            configure => builder.ImplicitOperator<int>(configure));
        OutputAssert.Equal(before, builder.ToString());
        AssertInvalidIndexerOrOperatorArguments<OperatorBuilder>("type",
            type => builder.ExplicitOperator(type), (type, configure) => builder.ExplicitOperator(type, configure),
            configure => builder.ExplicitOperator<int>(configure));
        OutputAssert.Equal(before, builder.ToString());
        AssertInvalidOperatorSymbols(
            (type, symbol) => builder.Operator(type, symbol),
            (type, symbol, configure) => builder.Operator(type, symbol, configure),
            symbol => builder.Operator<int>(symbol),
            (symbol, configure) => builder.Operator<int>(symbol, configure));
        OutputAssert.Equal(before, builder.ToString());
    }

    private static void AssertInvalidIndexerOrOperatorArguments<TBuilder>(string paramName,
        Action<Type> typeOverload, Action<Type, Action<TBuilder>> configuredTypeOverload,
        Action<Action<TBuilder>> configuredGenericOverload)
    {
        var calls = 0;
        Action<TBuilder> configure = _ => calls++;
        var invalidTypes = new[] { typeof(List<>), typeof(int).MakeByRefType(), new { Value = 1 }.GetType() };
        foreach (var type in invalidTypes)
        {
            Assert.Throws<ArgumentException>(paramName, () => typeOverload(type));
            Assert.Throws<ArgumentException>(paramName, () => configuredTypeOverload(type, configure));
            Assert.Throws<ArgumentException>(paramName, () => configuredTypeOverload(type, null!));
        }

        Assert.Throws<ArgumentNullException>(paramName, () => typeOverload(null!));
        Assert.Throws<ArgumentNullException>(paramName, () => configuredTypeOverload(null!, configure));
        Assert.Throws<ArgumentNullException>(paramName, () => configuredTypeOverload(null!, null!));
        Assert.Throws<ArgumentNullException>("configure", () => configuredTypeOverload(typeof(int), null!));
        Assert.Throws<ArgumentNullException>("configure", () => configuredGenericOverload(null!));
        Assert.Equal(0, calls);
    }

    private static void AssertInvalidOperatorSymbols(
        Action<Type, string> typeOverload, Action<Type, string, Action<OperatorBuilder>> configuredTypeOverload,
        Action<string> genericOverload, Action<string, Action<OperatorBuilder>> configuredGenericOverload)
    {
        var calls = 0;
        Action<OperatorBuilder> configure = _ => calls++;
        Assert.Throws<ArgumentNullException>("symbol", () => typeOverload(typeof(int), null!));
        Assert.Throws<ArgumentNullException>("symbol", () => configuredTypeOverload(typeof(int), null!, configure));
        Assert.Throws<ArgumentNullException>("symbol", () => genericOverload(null!));
        Assert.Throws<ArgumentNullException>("symbol", () => configuredGenericOverload(null!, configure));
        Assert.Throws<ArgumentException>("returnType", () => typeOverload(typeof(List<>), null!));
        Assert.Throws<ArgumentException>("returnType", () => configuredTypeOverload(typeof(List<>), null!, null!));
        Assert.Throws<ArgumentNullException>("returnType", () => configuredTypeOverload(null!, null!, null!));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void IndexersAndOperators_RenderExamples()
    {
        OutputAssert.Equal("int this[int index] { get; set; }\n",
            CSharp.Indexer<int>(i => i.Parameter("int", "index")).ToString());
        OutputAssert.Equal("public static bool operator ==(Money left, Money right) => left.Amount == right.Amount;\n",
            CSharp.Operator<bool>("==", o => o.Public().Static()
                .Parameter("Money", "left").Parameter("Money", "right")
                .Arrow("left.Amount == right.Amount")).ToString());
        OutputAssert.Equal("implicit operator decimal();\n", CSharp.ImplicitOperator<decimal>().ToString());
        OutputAssert.Equal("explicit operator global::System.TimeSpan();\n", CSharp.ExplicitOperator(typeof(TimeSpan)).ToString());
    }

    [Fact]
    public void CompleteFile_WithIndexerAndOperatorTypeOverloads_Compiles()
    {
        var file = CSharp.File().Namespace("App", n => n
            .Class("Catalog", c => c
                .Indexer(typeof(Outer<int>.Inner<string>), i => i.Public().Parameter<int>("index").Arrow("null"))
                .Indexer<int?>(i => i.Public().Parameter<string>("key").Arrow("null"))
                .Add(CSharp.Indexer<List<string>>(i => i.Public().Parameter<long>("index").Arrow("null"))))
            .Interface("ICatalog", i => i
                .Indexer(typeof(Outer<int>.Inner<string>), indexer => indexer.Parameter<int>("index").Get())
                .Indexer<int?>(indexer => indexer.Parameter<string>("key").Get()))
            .Struct("Money", s => s
                .Property<decimal>("Amount", p => p.Public())
                .Operator(typeof(bool), "==", o => o.Public().Static()
                    .Parameter("Money", "left").Parameter("Money", "right").Arrow("left.Amount == right.Amount"))
                .Operator<bool>("!=", o => o.Public().Static()
                    .Parameter("Money", "left").Parameter("Money", "right").Arrow("left.Amount != right.Amount"))
                .ImplicitOperator(typeof(decimal), o => o.Public().Static().Parameter("Money", "value").Arrow("value.Amount"))
                .ImplicitOperator<double>(o => o.Public().Static().Parameter("Money", "value").Arrow("(double)value.Amount"))
                .ExplicitOperator(typeof(TimeSpan), o => o.Public().Static().Parameter("Money", "value").Arrow("default"))
                .ExplicitOperator<int>(o => o.Public().Static().Parameter("Money", "value").Arrow("(int)value.Amount"))));

        CompilationAssert.NoErrors(file.ToString() + "\n" + TypeFixtures.Source);
    }
}
