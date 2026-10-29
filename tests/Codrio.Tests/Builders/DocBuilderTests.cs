using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Codrio.Tests;

public class DocBuilderTests
{
    [Fact]
    public void Elements_RenderInCallOrderIncludingDuplicates()
    {
        var doc = CaptureDoc();
        doc.Returns("The value.").Summary("First.").Param("value", "The input.")
            .Xml("<remarks>See <see cref=\"Gadget\"/> &amp; more.</remarks>")
            .Summary("Second.").Param("value", "Again.").Returns("Again.").Xml("<remarks>Again.</remarks>");

        const string expected = "/// <returns>The value.</returns>\n/// <summary>First.</summary>\n/// <param name=\"value\">The input.</param>\n/// <remarks>See <see cref=\"Gadget\"/> &amp; more.</remarks>\n/// <summary>Second.</summary>\n/// <param name=\"value\">Again.</param>\n/// <returns>Again.</returns>\n/// <remarks>Again.</remarks>\n";
        OutputAssert.Equal(expected, doc.ToString());
        OutputAssert.Equal(expected, doc.ToString());
    }

    [Theory]
    [InlineData("&", "&amp;")]
    [InlineData("<", "&lt;")]
    [InlineData(">", "&gt;")]
    [InlineData("Returns a < b && c > d.", "Returns a &lt; b &amp;&amp; c &gt; d.")]
    [InlineData("\"quoted\" and 'quoted'", "\"quoted\" and 'quoted'")]
    [InlineData("&amp;", "&amp;amp;")]
    [InlineData("", "")]
    public void TextElements_EscapeXmlButPreserveQuotes(string text, string escaped)
    {
        var doc = CaptureDoc().Summary(text).Param("value", text).Returns(text);

        OutputAssert.Equal("/// <summary>" + escaped + "</summary>\n/// <param name=\"value\">" + escaped
            + "</param>\n/// <returns>" + escaped + "</returns>\n", doc.ToString());
    }

    [Theory]
    [InlineData("First line.\n\nThird line.")]
    [InlineData("First line.\r\n\nThird line.")]
    public void TextElements_NormalizeLineEndingsAndPrefixEmptyLines(string text)
    {
        var doc = CaptureDoc().Summary(text).Param("value", text).Returns(text);

        OutputAssert.Equal("/// <summary>First line.\n///\n/// Third line.</summary>\n"
            + "/// <param name=\"value\">First line.\n///\n/// Third line.</param>\n"
            + "/// <returns>First line.\n///\n/// Third line.</returns>\n", doc.ToString());
    }

    [Fact]
    public void Xml_PreservesRawMultilineContentAndNormalizesCrLf()
    {
        var doc = CaptureDoc().Xml("<remarks>\r\n<see cref=\"Gadget\"/> &amp; more.\n\n</remarks>\r\n").Xml("");

        OutputAssert.Equal("/// <remarks>\n/// <see cref=\"Gadget\"/> &amp; more.\n///\n/// </remarks>\n///\n///\n", doc.ToString());
    }

    [Fact]
    public void Xml_DoesNotValidateOrEscapeInput()
    {
        var doc = CaptureDoc().Xml("a < b && c > d.");

        OutputAssert.Equal("/// a < b && c > d.\n", doc.ToString());
    }

    [Theory]
    [InlineData("class", "@class")]
    [InlineData("@class", "@class")]
    [InlineData("@value", "@value")]
    public void Param_EscapesKeywordNamesAndPreservesEscapedNames(string name, string escaped)
    {
        var doc = CaptureDoc().Param(name, "The class.").Returns("The value.");

        OutputAssert.Equal("/// <param name=\"" + escaped + "\">The class.</param>\n/// <returns>The value.</returns>\n", doc.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("1value")]
    [InlineData("a-b")]
    [InlineData("a<b")]
    [InlineData("a&b")]
    [InlineData("a\"b")]
    [InlineData("a'b")]
    [InlineData("@")]
    [InlineData("@@class")]
    public void Param_InvalidNameThrowsAtCallTimeWithoutAddingElement(string name)
    {
        var doc = CaptureDoc();

        Assert.Throws<ArgumentException>("name", () => doc.Param(name, "Text."));
        OutputAssert.Equal("", doc.ToString());
    }

    [Fact]
    public void NullArguments_ThrowAtCallTimeWithoutAddingElements()
    {
        var doc = CaptureDoc();

        Assert.Throws<ArgumentNullException>("text", () => doc.Summary(null!));
        Assert.Throws<ArgumentNullException>("name", () => doc.Param(null!, "Text."));
        Assert.Throws<ArgumentNullException>("text", () => doc.Param("value", null!));
        Assert.Throws<ArgumentNullException>("text", () => doc.Returns(null!));
        Assert.Throws<ArgumentNullException>("xml", () => doc.Xml(null!));
        OutputAssert.Equal("", doc.ToString());

        var builder = CSharp.Class("C").Doc(d => d.Summary("Existing."));
        Assert.Throws<ArgumentNullException>("configure", () => builder.Doc(null!));
        OutputAssert.Equal("/// <summary>Existing.</summary>\nclass C\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void EmptyDocumentation_ProducesNoOutput()
    {
        OutputAssert.Equal("", CaptureDoc().ToString());
        OutputAssert.Equal("class C\n{\n}\n", CSharp.Class("C", c => c.Doc(_ => { })).ToString());
    }

    [Fact]
    public void Doc_InvokesConfigureImmediatelyAndOnlyOnce()
    {
        var calls = 0;
        DocBuilder? captured = null;
        var builder = CSharp.Class("C", c => c.Doc(d =>
        {
            calls++;
            captured = d;
            d.Summary("Text.");
        }));

        Assert.Equal(1, calls);
        Assert.NotNull(captured);
        OutputAssert.Equal("/// <summary>Text.</summary>\n", captured.ToString());
        OutputAssert.Equal("/// <summary>Text.</summary>\nclass C\n{\n}\n", builder.ToString());
        OutputAssert.Equal("/// <summary>Text.</summary>\nclass C\n{\n}\n", builder.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Doc_ReplacesPreviousDocumentationWithANewBuilder()
    {
        DocBuilder? first = null;
        DocBuilder? second = null;
        var builder = CSharp.Class("C").Doc(d => first = d.Summary("First."))
            .Doc(d => second = d.Summary("Last."));

        Assert.NotSame(first, second);
        OutputAssert.Equal("/// <summary>Last.</summary>\nclass C\n{\n}\n", builder.ToString());
        builder.Doc(_ => { });
        OutputAssert.Equal("class C\n{\n}\n", builder.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Documentation_PrecedesAttributesRegardlessOfCallOrder(bool attributeFirst)
    {
        var builder = CSharp.Class("Widget", c => c.Public());
        if (attributeFirst)
        {
            builder.Attribute("Serializable");
        }

        builder.Doc(d => d.Summary("Represents a widget.").Xml("<remarks>See <see cref=\"Gadget\"/>.</remarks>"));
        if (!attributeFirst)
        {
            builder.Attribute("Serializable");
        }

        OutputAssert.Equal("/// <summary>Represents a widget.</summary>\n/// <remarks>See <see cref=\"Gadget\"/>.</remarks>\n[Serializable]\npublic class Widget\n{\n}\n", builder.ToString());
    }

    [Fact]
    public void Doc_WorksWithEveryTypeKind()
    {
        OutputAssert.Equal("/// <summary>Class.</summary>\nclass C\n{\n}\n",
            CSharp.Class("C", c => c.Doc(d => d.Summary("Class."))).ToString());
        OutputAssert.Equal("/// <summary>Struct.</summary>\nstruct S\n{\n}\n",
            CSharp.Struct("S", s => s.Doc(d => d.Summary("Struct."))).ToString());
        OutputAssert.Equal("/// <summary>Interface.</summary>\ninterface I\n{\n}\n",
            CSharp.Interface("I", i => i.Doc(d => d.Summary("Interface."))).ToString());
        OutputAssert.Equal("/// <summary>Enum.</summary>\nenum E\n{\n    Value\n}\n",
            CSharp.Enum("E", e => e.Doc(d => d.Summary("Enum.")).Member("Value")).ToString());
    }

    [Theory]
    [InlineData(false, "record")]
    [InlineData(true, "record struct")]
    public void RecordDocumentation_DoesNotCountAsBodyItems(bool isStruct, string keyword)
    {
        var builder = isStruct ? CSharp.RecordStruct("Point") : CSharp.Record("Point");
        builder.Doc(d => d.Summary("Point."));

        OutputAssert.Equal("/// <summary>Point.</summary>\n" + keyword + " Point;\n", builder.ToString());
        builder.PrimaryParameter("int", "X");
        OutputAssert.Equal("/// <summary>Point.</summary>\n" + keyword + " Point(int X);\n", builder.ToString());
        builder.Property("int", "Y");
        OutputAssert.Equal("/// <summary>Point.</summary>\n" + keyword + " Point(int X)\n{\n    int Y { get; set; }\n}\n", builder.ToString());
    }

    [Fact]
    public void NestedDocumentation_UsesCurrentIndentationAndFollowsItemSpacing()
    {
        var file = CSharp.File().Namespace("App", n => n.Class("Outer", c => c.Doc(d => d.Summary("Outer."))
            .Field("int", "value")
            .Class("Inner", i => i.Attribute("Obsolete").Doc(d => d.Summary("First.\r\n\nThird.")))
            .Enum("Color", e => e.Doc(d => d.Summary("Colors.")).Member("Red"))));

        const string expected = "namespace App\n{\n    /// <summary>Outer.</summary>\n    class Outer\n    {\n        int value;\n\n        /// <summary>First.\n        ///\n        /// Third.</summary>\n        [Obsolete]\n        class Inner\n        {\n        }\n\n        /// <summary>Colors.</summary>\n        enum Color\n        {\n            Red\n        }\n    }\n}\n";
        OutputAssert.Equal(expected, file.ToString());
    }

    [Fact]
    public void AddedTypes_ReflectDocumentationChangesAfterAdd()
    {
        var classBuilder = CSharp.Class("C").Doc(d => d.Summary("Before."));
        var enumBuilder = CSharp.Enum("E");
        var file = CSharp.File().Add(classBuilder).Add(enumBuilder);
        var ns = CSharp.Namespace("App").Add(classBuilder).Add(enumBuilder);
        var outer = CSharp.Class("Outer").Add(classBuilder).Add(enumBuilder);

        OutputAssert.Equal("/// <summary>Before.</summary>\nclass C\n{\n}\n\nenum E\n{\n}\n", file.ToString());
        classBuilder.Doc(d => d.Summary("After."));
        enumBuilder.Doc(d => d.Summary("Added."));

        OutputAssert.Equal("/// <summary>After.</summary>\nclass C\n{\n}\n\n/// <summary>Added.</summary>\nenum E\n{\n}\n", file.ToString());
        OutputAssert.Equal("namespace App\n{\n    /// <summary>After.</summary>\n    class C\n    {\n    }\n\n    /// <summary>Added.</summary>\n    enum E\n    {\n    }\n}\n", ns.ToString());
        OutputAssert.Equal("class Outer\n{\n    /// <summary>After.</summary>\n    class C\n    {\n    }\n\n    /// <summary>Added.</summary>\n    enum E\n    {\n    }\n}\n", outer.ToString());
    }

    [Fact]
    public void FluentMethods_ReturnSameBuilder()
    {
        var doc = CaptureDoc();
        Assert.Same(doc, doc.Summary("Text."));
        Assert.Same(doc, doc.Param("value", "Text."));
        Assert.Same(doc, doc.Returns("Text."));
        Assert.Same(doc, doc.Xml("<remarks/>"));

        var classBuilder = CSharp.Class("C");
        var structBuilder = CSharp.Struct("S");
        var interfaceBuilder = CSharp.Interface("I");
        var recordBuilder = CSharp.Record("R");
        var recordStructBuilder = CSharp.RecordStruct("RS");
        var enumBuilder = CSharp.Enum("E");
        ClassBuilder returnedClass = classBuilder.Doc(d => d.Summary("Text."));
        StructBuilder returnedStruct = structBuilder.Doc(d => d.Summary("Text."));
        InterfaceBuilder returnedInterface = interfaceBuilder.Doc(d => d.Summary("Text."));
        RecordBuilder returnedRecord = recordBuilder.Doc(d => d.Summary("Text."));
        RecordBuilder returnedRecordStruct = recordStructBuilder.Doc(d => d.Summary("Text."));
        EnumBuilder returnedEnum = enumBuilder.Doc(d => d.Summary("Text."));
        Assert.Same(classBuilder, returnedClass);
        Assert.Same(structBuilder, returnedStruct);
        Assert.Same(interfaceBuilder, returnedInterface);
        Assert.Same(recordBuilder, returnedRecord);
        Assert.Same(recordStructBuilder, returnedRecordStruct);
        Assert.Same(enumBuilder, returnedEnum);
    }

    [Fact]
    public void DocumentationDiagnostics_DetectMalformedRawXml()
    {
        var source = CSharp.File().Raw("/// <summary>a < b</summary>").Class("C").ToString();

        Assert.Contains(DocumentationDiagnostics(source), diagnostic => diagnostic.Id == "CS1570");
    }

    [Fact]
    public void CompleteFile_WithDocumentationAndAttributes_CompilesAndHasWellFormedXml()
    {
        const string text = "a < b && c > d.";
        var file = CSharp.File().Using("System").Namespace("App", n => n
            .Class("Widget", c => c.Public().Attribute("Serializable").Doc(d => d.Summary(text)
                .Returns(text).Xml("<remarks>\r\nSee <see cref=\"System.String\"/>.\n\n</remarks>")))
            .Class("Container", c => c.TypeParameter("T").Doc(d => d.Summary(text)
                .Xml("<typeparam name=\"T\">The item type.</typeparam>")))
            .Record("Data", r => r.PrimaryParameter("int", "Value").Doc(d => d.Summary(text).Param("Value", text)))
            .RecordStruct("Point", r => r.PrimaryParameter("int", "X").Doc(d => d.Summary(text).Param("X", text)))
            .Enum("Color", e => e.Attribute("Flags").Doc(d => d.Summary(text)).Member("Red", "1"))
            .Interface("IWidget", i => i.Doc(d => d.Summary(text)))
            .Struct("Value", s => s.Doc(d => d.Summary(text)))
            .Class("Outer", c => c.Doc(d => d.Summary(text))
                .Class("Inner", i => i.Doc(d => d.Summary(text)))));

        var source = file.ToString();
        Assert.DoesNotContain(DocumentationDiagnostics(source), diagnostic => diagnostic.Id == "CS1570");
        CompilationAssert.NoErrors(source);
    }

    [Fact]
    public void MemberDocumentation_PrecedesAttributesAndStandaloneDeclarations()
    {
        OutputAssert.Equal("/// <summary>Runs the job.</summary>\n/// <param name=\"count\">The number of runs.</param>\n/// <returns>The result.</returns>\n[Obsolete]\npublic int Run(int count) => count;\n",
            CSharp.Method("Run", m => m.Public().Returns("int").Parameter("int", "count").Attribute("Obsolete")
                .Doc(d => d.Summary("Runs the job.").Param("count", "The number of runs.").Returns("The result.")).Arrow("count")).ToString());
        OutputAssert.Equal("/// <summary>Field.</summary>\n[NonSerialized]\nprivate int count;\n",
            CSharp.Field("int", "count", f => f.Private().Attribute("NonSerialized").Doc(d => d.Summary("Field."))).ToString());
        OutputAssert.Equal("/// <summary>Constructor.</summary>\n[Obsolete]\npublic ()\n{\n    count = 1;\n}\n",
            CSharp.Constructor(k => k.Public().Attribute("Obsolete").Doc(d => d.Summary("Constructor."))
                .Body(b => b.Line("count = 1;"))).ToString());
        OutputAssert.Equal("/// <summary>Property.</summary>\n[Obsolete]\npublic int Count { get; set; }\n",
            CSharp.Property("int", "Count", p => p.Public().Attribute("Obsolete").Doc(d => d.Summary("Property."))).ToString());
        OutputAssert.Equal("/// <summary>Indexer.</summary>\n[Obsolete]\npublic int this[int index] => index;\n",
            CSharp.Indexer("int", x => x.Public().Parameter("int", "index").Attribute("Obsolete")
                .Doc(d => d.Summary("Indexer.")).Arrow("index")).ToString());
        OutputAssert.Equal("/// <summary>Event.</summary>\n[Obsolete]\npublic event Action Changed;\n",
            CSharp.Event("Action", "Changed", e => e.Public().Attribute("Obsolete").Doc(d => d.Summary("Event."))).ToString());
        OutputAssert.Equal("/// <summary>Delegate.</summary>\n[Obsolete]\npublic delegate void Handler();\n",
            CSharp.Delegate("Handler", d => d.Public().Attribute("Obsolete").Doc(doc => doc.Summary("Delegate."))).ToString());
        OutputAssert.Equal("/// <summary>Operator.</summary>\n[Obsolete]\npublic static Value operator +(Value value) => value;\n",
            CSharp.Operator("Value", "+", o => o.Public().Static().Parameter("Value", "value").Attribute("Obsolete")
                .Doc(d => d.Summary("Operator.")).Arrow("value")).ToString());
    }

    [Fact]
    public void MemberDocumentation_PrecedesBlockBodies()
    {
        OutputAssert.Equal("/// <summary>Method.</summary>\n[Obsolete]\nvoid Run()\n{\n    RunAgain();\n}\n",
            CSharp.Method("Run", m => m.Doc(d => d.Summary("Method.")).Attribute("Obsolete")
                .Body(b => b.Line("RunAgain();"))).ToString());
        OutputAssert.Equal("/// <summary>Property.</summary>\n[Obsolete]\nint Count\n{\n    get\n    {\n        return count;\n    }\n    set\n    {\n        count = value;\n    }\n}\n",
            CSharp.Property("int", "Count", p => p.Doc(d => d.Summary("Property.")).Attribute("Obsolete")
                .Get(a => a.Body(b => b.Line("return count;")))
                .Set(a => a.Body(b => b.Line("count = value;")))).ToString());
    }

    [Fact]
    public void MemberDocumentation_PreservesSpacingAndNestedIndentation()
    {
        var builder = CSharp.Class("Outer", c => c
            .Field("int", "count", f => f.Doc(d => d.Summary("Count.")))
            .Method("Run", m => m.Doc(d => d.Summary("Run.")).Body())
            .Class("Inner", i => i.Property("int", "Value", p => p.Attribute("Obsolete")
                .Doc(d => d.Summary("First.\r\n\nThird.")))));

        OutputAssert.Equal("class Outer\n{\n    /// <summary>Count.</summary>\n    int count;\n\n"
            + "    /// <summary>Run.</summary>\n    void Run()\n    {\n    }\n\n"
            + "    class Inner\n    {\n        /// <summary>First.\n        ///\n        /// Third.</summary>\n"
            + "        [Obsolete]\n        int Value { get; set; }\n    }\n}\n", builder.ToString());
    }

    [Fact]
    public void FileAndBlockNamespace_IndentDelegateDocumentation()
    {
        var file = CSharp.File().Delegate("Top", d => d.Attribute("Obsolete").Doc(doc => doc.Summary("Top.")))
            .Namespace("App", n => n.Delegate("Nested", d => d.Doc(doc => doc.Summary("Nested.")).Attribute("Obsolete")));

        OutputAssert.Equal("/// <summary>Top.</summary>\n[Obsolete]\ndelegate void Top();\n\nnamespace App\n{\n"
            + "    /// <summary>Nested.</summary>\n    [Obsolete]\n    delegate void Nested();\n}\n", file.ToString());
    }

    [Fact]
    public void AddedMembers_ReflectDocumentationChangesAfterAdd()
    {
        var method = CSharp.Method("Run").Doc(d => d.Summary("Before."));
        var field = CSharp.Field("int", "value").Doc(d => d.Summary("Before."));
        var constructor = CSharp.Constructor().Doc(d => d.Summary("Before."));
        var property = CSharp.Property("int", "Value").Doc(d => d.Summary("Before."));
        var indexer = CSharp.Indexer("int").Parameter("int", "index").Get().Doc(d => d.Summary("Before."));
        var eventBuilder = CSharp.Event("Action", "Changed").Doc(d => d.Summary("Before."));
        var delegateBuilder = CSharp.Delegate("Handler").Doc(d => d.Summary("Before."));
        var operatorBuilder = CSharp.Operator("Service", "+").Parameter("Service", "value").Doc(d => d.Summary("Before."));
        var builder = CSharp.Class("Service").Add(method).Add(field).Add(constructor).Add(property)
            .Add(indexer).Add(eventBuilder).Add(delegateBuilder).Add(operatorBuilder);
        var file = CSharp.File().Add(delegateBuilder);
        var ns = CSharp.Namespace("App").Add(delegateBuilder);

        const string expected = "class Service\n{\n"
            + "    /// <summary>Before.</summary>\n    void Run();\n\n"
            + "    /// <summary>Before.</summary>\n    int value;\n\n"
            + "    /// <summary>Before.</summary>\n    Service()\n    {\n    }\n\n"
            + "    /// <summary>Before.</summary>\n    int Value { get; set; }\n\n"
            + "    /// <summary>Before.</summary>\n    int this[int index] { get; }\n\n"
            + "    /// <summary>Before.</summary>\n    event Action Changed;\n\n"
            + "    /// <summary>Before.</summary>\n    delegate void Handler();\n\n"
            + "    /// <summary>Before.</summary>\n    Service operator +(Service value);\n}\n";
        OutputAssert.Equal(expected, builder.ToString());

        method.Doc(d => d.Summary("After."));
        field.Doc(d => d.Summary("After."));
        constructor.Doc(d => d.Summary("After."));
        property.Doc(d => d.Summary("After."));
        indexer.Doc(d => d.Summary("After."));
        eventBuilder.Doc(d => d.Summary("After."));
        delegateBuilder.Doc(d => d.Summary("After."));
        operatorBuilder.Doc(d => d.Summary("After."));

        OutputAssert.Equal(expected.Replace("Before.", "After."), builder.ToString());
        OutputAssert.Equal("/// <summary>After.</summary>\ndelegate void Handler();\n", file.ToString());
        OutputAssert.Equal("namespace App\n{\n    /// <summary>After.</summary>\n    delegate void Handler();\n}\n", ns.ToString());
    }

    [Fact]
    public void MemberDoc_InvokesConfigureImmediatelyAndOnlyOnce()
    {
        var calls = 0;
        DocBuilder? captured = null;
        var builder = CSharp.Method("Run").Doc(d =>
        {
            calls++;
            captured = d.Summary("Run.");
        });

        Assert.Equal(1, calls);
        Assert.NotNull(captured);
        OutputAssert.Equal("/// <summary>Run.</summary>\n", captured.ToString());
        OutputAssert.Equal("/// <summary>Run.</summary>\nvoid Run();\n", builder.ToString());
        OutputAssert.Equal("/// <summary>Run.</summary>\nvoid Run();\n", builder.ToString());
        Assert.Equal(1, calls);
    }

    [Fact]
    public void MemberDoc_ReplacesDocumentationWithANewBuilderAndCanClearIt()
    {
        DocBuilder? first = null;
        DocBuilder? second = null;
        var builder = CSharp.Method("Run").Attribute("Obsolete")
            .Doc(d => first = d.Summary("First.")).Doc(d => second = d.Summary("Last."));

        Assert.NotSame(first, second);
        OutputAssert.Equal("/// <summary>Last.</summary>\n[Obsolete]\nvoid Run();\n", builder.ToString());
        builder.Doc(_ => { });
        OutputAssert.Equal("[Obsolete]\nvoid Run();\n", builder.ToString());
        OutputAssert.Equal("void Run();\n", CSharp.Method("Run").Doc(_ => { }).ToString());
    }

    [Fact]
    public void MemberDoc_NullConfigureThrowsWithoutReplacingDocumentation()
    {
        var builder = CSharp.Method("Run").Doc(d => d.Summary("Existing."));

        Assert.Throws<ArgumentNullException>("configure", () => builder.Doc(null!));
        OutputAssert.Equal("/// <summary>Existing.</summary>\nvoid Run();\n", builder.ToString());
    }

    [Fact]
    public void MemberDoc_ReturnsSameConcreteBuilderForEveryMemberKind()
    {
        var method = CSharp.Method("Run");
        var field = CSharp.Field("int", "value");
        var constructor = CSharp.Constructor();
        var property = CSharp.Property("int", "Value");
        var indexer = CSharp.Indexer("int");
        var eventBuilder = CSharp.Event("Action", "Changed");
        var delegateBuilder = CSharp.Delegate("Handler");
        var operatorBuilder = CSharp.Operator("Value", "+");

        MethodBuilder returnedMethod = method.Doc(d => d.Summary("Method."));
        FieldBuilder returnedField = field.Doc(d => d.Summary("Field."));
        ConstructorBuilder returnedConstructor = constructor.Doc(d => d.Summary("Constructor."));
        PropertyBuilder returnedProperty = property.Doc(d => d.Summary("Property."));
        IndexerBuilder returnedIndexer = indexer.Doc(d => d.Summary("Indexer."));
        EventBuilder returnedEvent = eventBuilder.Doc(d => d.Summary("Event."));
        DelegateBuilder returnedDelegate = delegateBuilder.Doc(d => d.Summary("Delegate."));
        OperatorBuilder returnedOperator = operatorBuilder.Doc(d => d.Summary("Operator."));

        Assert.Same(method, returnedMethod);
        Assert.Same(field, returnedField);
        Assert.Same(constructor, returnedConstructor);
        Assert.Same(property, returnedProperty);
        Assert.Same(indexer, returnedIndexer);
        Assert.Same(eventBuilder, returnedEvent);
        Assert.Same(delegateBuilder, returnedDelegate);
        Assert.Same(operatorBuilder, returnedOperator);
    }

    private static DocBuilder CaptureDoc()
    {
        DocBuilder? captured = null;
        CSharp.Class("C", c => c.Doc(d => captured = d));
        return captured!;
    }

    private static IEnumerable<Diagnostic> DocumentationDiagnostics(string source) =>
        CSharpSyntaxTree.ParseText(source,
            new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose)).GetDiagnostics();
}
