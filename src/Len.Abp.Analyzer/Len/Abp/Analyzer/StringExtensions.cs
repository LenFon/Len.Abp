using System.Buffers;

namespace Len.Abp.Analyzer;

public static class StringExtensions
{
    extension(string text)
    {
        public string NormalizeLineEndings(string? newLine = null)
        {
            if (text.IndexOfAny(['\r', '\n']) == -1 || newLine is null) return text;

            var estimatedLength = text.Length * 2;
            char[]? rentedBuffer = null;
            try
            {
                rentedBuffer = ArrayPool<char>.Shared.Rent(estimatedLength);
                var outputBuffer = rentedBuffer.AsMemory(0, estimatedLength);
                int outputIndex = 0;
                var inputSpan = text.AsSpan();
                var inputLength = inputSpan.Length;
                var i = 0;

                while (i < inputLength)
                {
                    if (inputSpan[i] is '\r' && i + 1 < inputLength && inputSpan[i + 1] is '\n')
                    {
                        newLine.AsSpan().CopyTo(outputBuffer.Span[outputIndex..]);
                        outputIndex += newLine.Length;
                        i += 2;
                    }
                    else if (inputSpan[i] is '\r' or '\n')
                    {
                        newLine.AsSpan().CopyTo(outputBuffer.Span[outputIndex..]);
                        outputIndex += newLine.Length;
                        i++;
                    }
                    else
                    {
                        outputBuffer.Span[outputIndex] = inputSpan[i];
                        outputIndex++;
                        i++;
                    }
                }

                return new string(rentedBuffer, 0, outputIndex);
            }
            finally
            {
                if (rentedBuffer != null)
                    ArrayPool<char>.Shared.Return(rentedBuffer);
            }
        }

        public string RemoveBlankLines()
        {
            if (string.IsNullOrEmpty(text)) return text;

            var normalizedText = text.NormalizeLineEndings("\n");

            var textSpan = normalizedText.AsSpan();
            var sb = new System.Text.StringBuilder(normalizedText.Length); // 预分配容量
            int startIndex = 0;
            int length = textSpan.Length;

            for (int i = 0; i <= length; i++)
            {
                if (i == length || textSpan[i] == '\n')
                {
                    var lineSpan = textSpan[startIndex..i];

                    if (!IsAllWhiteSpaceOrEmpty(lineSpan))
                    {
                        if (sb.Length > 0)
                            sb.Append('\n');
                        sb.Append(lineSpan.ToArray());
                    }

                    startIndex = i + 1;
                }
            }

            return sb.ToString();
        }
    }

    private static bool IsAllWhiteSpaceOrEmpty(ReadOnlySpan<char> span)
    {
        if (span.IsEmpty)
            return true;

        foreach (char c in span)
        {
            if (!char.IsWhiteSpace(c))
                return false;
        }
        return true;
    }
}