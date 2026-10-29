namespace Codrio.Rendering;

internal static class XmlText
{
    internal static string Escape(string text) =>
        text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
