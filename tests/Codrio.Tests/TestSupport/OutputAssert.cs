using Xunit;

namespace Codrio.Tests;

internal static class OutputAssert
{
    internal static void Equal(string expected, string actual)
    {
        Assert.Equal(expected.Replace("\r\n", "\n"), actual);
    }
}
