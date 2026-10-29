using System;
using Xunit;

namespace Codrio.Tests;

public class EventBuilderTests
{
    [Theory]
    [InlineData("Done", "event System.Action Done;\n")]
    [InlineData("event", "event System.Action @event;\n")]
    [InlineData("@event", "event System.Action @event;\n")]
    public void ToString_RendersEventAndEscapesNames(string name, string expected)
    {
        var builder = CSharp.Event("System.Action", name);

        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, CSharp.Event("System.Action", name, _ => { }).ToString());
    }

    [Theory]
    [InlineData("System.EventHandler?")]
    [InlineData("global::System.Action<string>")]
    [InlineData("unvalidated type")]
    [InlineData("")]
    public void Event_WritesTypeVerbatim(string type)
    {
        OutputAssert.Equal("event " + type + " Done;\n", CSharp.Event(type, "Done").ToString());
    }

    [Fact]
    public void Accessibility_LastCallWins()
    {
        var builder = CSharp.Event("System.Action", "Done").Public().Internal();
        OutputAssert.Equal("internal event System.Action Done;\n", builder.ToString());
        builder.Protected();
        OutputAssert.Equal("protected event System.Action Done;\n", builder.ToString());
        builder.Private();
        OutputAssert.Equal("private event System.Action Done;\n", builder.ToString());
        builder.ProtectedInternal();
        OutputAssert.Equal("protected internal event System.Action Done;\n", builder.ToString());
        builder.PrivateProtected();
        OutputAssert.Equal("private protected event System.Action Done;\n", builder.ToString());
        builder.Public();
        OutputAssert.Equal("public event System.Action Done;\n", builder.ToString());
    }

    [Fact]
    public void Modifiers_RenderInFixedOrderOnceWithoutValidatingCombinations()
    {
        var builder = CSharp.Event("System.Action", "Done").ReadOnly().Override().Sealed()
            .Abstract().Virtual().New().Static().Public();
        const string expected = "public static new virtual abstract sealed override readonly event System.Action Done;\n";

        OutputAssert.Equal(expected, builder.ToString());
        builder.Static().New().Virtual().Abstract().Sealed().Override().ReadOnly();
        OutputAssert.Equal(expected, builder.ToString());
    }

    [Fact]
    public void Event_InvokesConfigureImmediatelyOnceOnReturnedBuilder()
    {
        EventBuilder? configured = null;
        var calls = 0;
        var builder = CSharp.Event("System.EventHandler?", "Changed", e =>
        {
            configured = e;
            calls++;
            e.Public();
        });

        Assert.Same(builder, configured);
        Assert.Equal(1, calls);
        OutputAssert.Equal("public event System.EventHandler? Changed;\n", builder.ToString());
        OutputAssert.Equal("public event System.EventHandler? Changed;\n", builder.ToString());
        Assert.Equal(1, calls);
        Assert.NotSame(builder, CSharp.Event("System.EventHandler?", "Changed"));
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("interface")]
    [InlineData("record")]
    [InlineData("record struct")]
    public void TypeEvents_ConfigureImmediatelyKeepReferencesAndReturnParent(string keyword)
    {
        switch (keyword)
        {
            case "class": AssertTypeEvents(CSharp.Class("Outer"), keyword); break;
            case "struct": AssertTypeEvents(CSharp.Struct("Outer"), keyword); break;
            case "interface": AssertTypeEvents(CSharp.Interface("Outer"), keyword); break;
            case "record": AssertTypeEvents(CSharp.Record("Outer"), keyword); break;
            case "record struct": AssertTypeEvents(CSharp.RecordStruct("Outer"), keyword); break;
        }
    }

    private static void AssertTypeEvents<TSelf>(TypeDeclarationBuilder<TSelf> builder, string keyword)
        where TSelf : TypeDeclarationBuilder<TSelf>
    {
        EventBuilder? configured = null;
        var calls = 0;
        var added = CSharp.Event("System.Action", "Added");

        Assert.Same(builder, builder.Event("System.Action", "First"));
        Assert.Same(builder, builder.Event("System.Action", "Done", e =>
        {
            configured = e;
            calls++;
            e.Public();
        }));
        Assert.Same(builder, builder.Add(added));
        Assert.NotNull(configured);
        Assert.Equal(1, calls);
        Assert.Same(configured, builder.items[1].Target);
        Assert.Same(added, builder.items[2].Target);
        OutputAssert.Equal(keyword + " Outer\n{\n    event System.Action First;\n\n    public event System.Action Done;\n\n    event System.Action Added;\n}\n",
            builder.ToString());

        configured.Private();
        added.Static();
        var expected = keyword + " Outer\n{\n    event System.Action First;\n\n    private event System.Action Done;\n\n    static event System.Action Added;\n}\n";
        OutputAssert.Equal(expected, builder.ToString());
        OutputAssert.Equal(expected, builder.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void MixedBodyItems_PreserveOrderSpacingAndNestedEventIndentation()
    {
        var builder = CSharp.Class("Outer").Event("System.Action", "First")
            .Property("int", "Count")
            .Method("Run", m => m.Body())
            .Class("Nested", c => c.Event("System.Action", "Done"))
            .Raw("// first\r\n// second")
            .Add(CSharp.Event("System.Action", "Last"));

        OutputAssert.Equal(
            "class Outer\n{\n    event System.Action First;\n\n    int Count { get; set; }\n\n" +
            "    void Run()\n    {\n    }\n\n    class Nested\n    {\n        event System.Action Done;\n    }\n\n" +
            "    // first\n    // second\n\n    event System.Action Last;\n}\n", builder.ToString());
    }

    [Fact]
    public void InterfaceEvent_RendersWithoutAccessibility()
    {
        OutputAssert.Equal("interface INotifier\n{\n    event System.Action Done;\n}\n",
            CSharp.Interface("INotifier", i => i.Event("System.Action", "Done")).ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("1Done")]
    [InlineData("Done.Value")]
    [InlineData("Done Value")]
    [InlineData("Done-Value")]
    [InlineData("@@Done")]
    public void InvalidNames_ThrowAtCallTimeBeforeConfiguringOrAdding(string name)
    {
        var configured = false;
        Assert.Throws<ArgumentException>("name", () => CSharp.Event("System.Action", name));
        Assert.Throws<ArgumentException>("name", () => CSharp.Event("System.Action", name, _ => configured = true));
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
        Assert.Throws<ArgumentException>("name", () => builder.Event("System.Action", name));
        Assert.Throws<ArgumentException>("name", () => builder.Event("System.Action", name, _ => configured = true));
        Assert.False(configured);
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void NullArguments_ThrowWithParameterNameBeforeConfiguringOrAdding()
    {
        var configured = false;
        Assert.Throws<ArgumentNullException>("type", () => CSharp.Event((string)null!, "Done"));
        Assert.Throws<ArgumentNullException>("type", () => CSharp.Event((string)null!, "Done", _ => configured = true));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Event("System.Action", null!));
        Assert.Throws<ArgumentNullException>("name", () => CSharp.Event("System.Action", null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Event("System.Action", "Done", null!));
        Assert.Throws<ArgumentNullException>("configure", () => CSharp.Event((string)null!, null!, null!));
        Assert.False(configured);

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
        Assert.Throws<ArgumentNullException>("type", () => builder.Event((string)null!, "Done"));
        Assert.Throws<ArgumentNullException>("type", () => builder.Event((string)null!, "Done", _ => configured = true));
        Assert.Throws<ArgumentNullException>("name", () => builder.Event("System.Action", null!));
        Assert.Throws<ArgumentNullException>("name", () => builder.Event("System.Action", null!, _ => configured = true));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Event("System.Action", "Done", null!));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Event((string)null!, null!, null!));
        Assert.Throws<ArgumentNullException>("eventBuilder", () => builder.Add((EventBuilder)null!));
        Assert.False(configured);
        OutputAssert.Equal(before, builder.ToString());
    }

    [Fact]
    public void FluentMethods_ReturnSameEventBuilder()
    {
        var builder = CSharp.Event("System.Action", "Done");

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
    }

    [Theory]
    [InlineData(typeof(EnumBuilder))]
    [InlineData(typeof(FileBuilder))]
    [InlineData(typeof(NamespaceBuilder))]
    public void UnsupportedContainers_DoNotExposeEventEntryPoints(Type type)
    {
        Assert.DoesNotContain(type.GetMethods(), method => method.Name == "Event");
        Assert.DoesNotContain(type.GetMethods(), method => method.Name == "Add" &&
            Array.Exists(method.GetParameters(), parameter => parameter.ParameterType == typeof(EventBuilder)));
    }

    [Fact]
    public void CompleteFile_RendersEventsAndCompiles()
    {
        var file = CSharp.File().NullableEnable()
            .Namespace("Example", n => n
                .Class("Notifier", c => c
                    .Event("System.EventHandler?", "Changed", e => e.Public())
                    .Method("Notify", m => m.Public().Body(b => b.Line("Changed?.Invoke(this, System.EventArgs.Empty);"))))
                .Class("Signals", c => c.Static()
                    .Event("System.Action?", "Done", e => e.Public().Static()))
                .Interface("INotifier", i => i.Event("System.Action?", "Done"))
                .Class("Implementation", c => c.BaseType("INotifier")
                    .Event("System.Action?", "Done", e => e.Public()))
                .Class("Base", c => c.Abstract()
                    .Event("System.Action?", "Done", e => e.Public().Abstract())
                    .Event("System.Action?", "Changed", e => e.Public().Virtual()))
                .Class("Derived", c => c.BaseType("Base")
                    .Event("System.Action?", "Done", e => e.Public().Override())
                    .Event("System.Action?", "Changed", e => e.Public().Override()))
                .Struct("ValueNotifier", s => s.Event("System.Action?", "Done", e => e.Public()))
                .Record("RecordNotifier", r => r.Event("System.Action?", "Done", e => e.Public()))
                .RecordStruct("ValueRecordNotifier", r => r.Add(CSharp.Event("System.Action?", "Done").Public())));
        const string expected =
            "#nullable enable\n\nnamespace Example\n{\n" +
            "    class Notifier\n    {\n        public event System.EventHandler? Changed;\n\n" +
            "        public void Notify()\n        {\n            Changed?.Invoke(this, System.EventArgs.Empty);\n        }\n    }\n\n" +
            "    static class Signals\n    {\n        public static event System.Action? Done;\n    }\n\n" +
            "    interface INotifier\n    {\n        event System.Action? Done;\n    }\n\n" +
            "    class Implementation : INotifier\n    {\n        public event System.Action? Done;\n    }\n\n" +
            "    abstract class Base\n    {\n        public abstract event System.Action? Done;\n\n" +
            "        public virtual event System.Action? Changed;\n    }\n\n" +
            "    class Derived : Base\n    {\n        public override event System.Action? Done;\n\n" +
            "        public override event System.Action? Changed;\n    }\n\n" +
            "    struct ValueNotifier\n    {\n        public event System.Action? Done;\n    }\n\n" +
            "    record RecordNotifier\n    {\n        public event System.Action? Done;\n    }\n\n" +
            "    record struct ValueRecordNotifier\n    {\n        public event System.Action? Done;\n    }\n}\n";

        OutputAssert.Equal(expected, file.ToString());
        CompilationAssert.NoErrors(file.ToString());
    }
}
