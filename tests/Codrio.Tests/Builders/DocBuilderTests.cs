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
