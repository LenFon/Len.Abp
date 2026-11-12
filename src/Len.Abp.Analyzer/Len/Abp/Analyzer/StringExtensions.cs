using System.Buffers;

namespace Len.Abp.Analyzer;

/// <summary>
/// 字符串处理扩展方法。
/// </summary>
public static class StringExtensions
{
    extension(string text)
    {
        /// <summary>
        /// 转义为可嵌入 C# 普通字符串字面量的文本。
        /// </summary>
        /// <returns>转义后的文本；反斜杠、引号与换行等字符会被转义。</returns>
        public string ToCSharpStringLiteral()
        {
            if (string.IsNullOrEmpty(text)) return text;

            var builder = new System.Text.StringBuilder(text.Length + 8);

            foreach (var character in text)
            {
                switch (character)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    case '\0':
                        builder.Append("\\0");
                        break;
                    default:
                        builder.Append(character);
                        break;
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// 转义为可安全嵌入 XML 文档注释的文本。
        /// </summary>
        /// <returns>转义后的文本。</returns>
        public string ToXmlCommentText() =>
            text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

        /// <summary>
        /// 将文本中的换行符统一替换为指定的换行符。
        /// </summary>
        /// <param name="newLine">目标换行符；为 null 时原样返回。</param>
        /// <returns>换行符归一化后的文本。</returns>
        public string NormalizeLineEndings(string? newLine = null)
        {
            if (text.IndexOfAny(['\r', '\n']) == -1 || newLine is null) return text;

            // 每个换行符最多被替换为 newLine.Length 个字符，因此最坏情况是
            // 原文长度乘以两者中较大的那个，而不是固定的 2 倍。
            var estimatedLength = text.Length * Math.Max(1, newLine.Length);
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

        /// <summary>
        /// 移除文本中的空白行，并将换行符统一为 <c>\n</c>。
        /// </summary>
        /// <returns>移除空白行后的文本。</returns>
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