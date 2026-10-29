using System.Collections.Generic;
using System.Linq;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Codrio.Tests;

internal static class CompilationAssert
{
    internal static IReadOnlyList<Diagnostic> GetErrors(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            "CompilationAssert",
            new[] { syntaxTree },
            Net80.References.All,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
    }

    internal static void NoErrors(string source)
    {
        var errors = GetErrors(source);
        Assert.True(errors.Count == 0, $"Compilation errors:\n{string.Join("\n", errors)}\n\nSource:\n{source}");
    }
}
