namespace Len.Abp.Analyzer;

public static class StringExtensions
{
    public static string NormalizeLineEndings(this string text, string? newLine = null)
    {
        text = text ?? throw new ArgumentNullException(nameof(text));

        var newText = text.Replace("\r\n", "\n").Replace("\r", "\n");
        if (newLine != null)
        {
            newText = newText.Replace("\n", newLine);
        }

        return newText;
    }

    public static string RemoveBlankLines(this string text)
    {
        text = text ?? throw new ArgumentNullException(nameof(text));

        return string.Join(
            separator: "\n",
            values: text.NormalizeLineEndings()
                .Split(['\n'], StringSplitOptions.None)
                .Where(static line => line.Length == 0 || !line.All(char.IsWhiteSpace))
        );
    }

    internal static string RemoveNameof(this string value)
    {
        value = value ?? throw new ArgumentNullException(nameof(value));

        return value.Contains("nameof(") ? value[(value.LastIndexOf('.') + 1)..].TrimEnd(')', ' ') : value;
    }
}