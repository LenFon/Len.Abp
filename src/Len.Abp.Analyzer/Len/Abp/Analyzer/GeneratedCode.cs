using System.Text;

namespace Len.Abp.Analyzer;

/// <summary>
/// 拼装生成代码的辅助方法。
/// </summary>
/// <remarks>
/// 生成器只输出相对类型体左对齐的片段，缩进与空行统一由这里和
/// <see cref="ClassData.WrapInTypeDeclarations"/> 处理，避免各生成器使用不同缩进基准。
/// </remarks>
public static class GeneratedCode
{
    /// <summary>
    /// 用空行连接各代码片段，忽略空片段。
    /// </summary>
    /// <param name="sections">代码片段，按输出顺序排列。</param>
    /// <returns>以换行结尾的完整代码。</returns>
    /// <remarks>
    /// 生成器按「文件头、预处理指令、命名空间、特性、类型声明」分块产出，
    /// 由这里统一决定块之间是否留空行，避免模板里的占位行在片段为空时留下多余空行。
    /// </remarks>
    public static string Join(params string?[] sections)
    {
        var builder = new StringBuilder();

        foreach (var section in sections)
        {
            var trimmed = section?.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(trimmed).Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>
    /// 用单个换行连接各代码行，跳过为 null 的可选行。
    /// </summary>
    /// <param name="lines">代码行，按输出顺序排列；null 表示该行不存在，空字符串表示保留一个空行。</param>
    /// <returns>以换行结尾的代码块。</returns>
    /// <remarks>
    /// 参数列表这类不能有空行的位置使用本方法；多行片段（如 lambda）作为单行传入时，
    /// 其内部换行会被保留。可选内容（如描述、分类）传 null，避免留下多余空行。
    /// </remarks>
    public static string Lines(params string?[] lines)
    {
        var builder = new StringBuilder();

        foreach (var line in lines)
        {
            if (line is null)
            {
                continue;
            }

            builder.Append(line.TrimEnd()).Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>
    /// 把多行片段接在 <paramref name="prefix"/> 之后，续行按指定宽度缩进。
    /// </summary>
    /// <param name="prefix">片段首行之前的内容，包含缩进。</param>
    /// <param name="block">左对齐的多行片段。</param>
    /// <param name="continuationIndent">续行需要增加的空格数。</param>
    /// <returns>首行带前缀、续行已缩进的片段。</returns>
    /// <remarks>
    /// 片段被插入到 <c>typeMetadata: </c> 这类前缀之后时，首行由前缀定位，
    /// 后续行必须补上与所在语句层级一致的缩进，否则会退回到行首。
    /// </remarks>
    public static string Inline(string prefix, string block, int continuationIndent)
    {
        var lines = SplitLines(block.TrimEnd());

        if (lines.Length == 1)
        {
            return prefix + lines[0];
        }

        var indent = new string(' ', continuationIndent);
        var builder = new StringBuilder(block.Length + (lines.Length * continuationIndent));

        builder.Append(prefix).Append(lines[0]);

        for (var i = 1; i < lines.Length; i++)
        {
            builder.Append('\n');

            if (lines[i].Length > 0)
            {
                builder.Append(indent);
            }

            builder.Append(lines[i]);
        }

        return builder.ToString();
    }

    private static string[] SplitLines(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
}
