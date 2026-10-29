using Xunit;
using Xunit.Sdk;

namespace Codrio.Tests;

public class CompilationAssertTests
{
    [Fact]
    public void GetErrors_AcceptsFileScopedNamespaceRecordAndInit()
    {
        const string source = """
            namespace Example;

            public record Person
            {
                public string Name { get; init; } = "";
            }
            """;

        Assert.Empty(CompilationAssert.GetErrors(source));
    }

    [Fact]
    public void GetErrors_ReturnsTypeMismatchDiagnostic()
    {
        const string source = "class C { void M() { int x = \"s\"; } }";

        Assert.Contains(CompilationAssert.GetErrors(source), diagnostic => diagnostic.Id == "CS0029");
    }

    [Fact]
    public void NoErrors_FailsWithDiagnosticsAndSource()
    {
        const string source = "class C { void M() { int x = \"s\"; } }";

        var exception = Assert.Throws<TrueException>(() => CompilationAssert.NoErrors(source));

        Assert.Contains("CS0029", exception.Message);
        Assert.Contains(source, exception.Message);
    }
}
