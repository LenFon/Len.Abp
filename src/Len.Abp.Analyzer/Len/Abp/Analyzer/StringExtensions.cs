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
    }
}