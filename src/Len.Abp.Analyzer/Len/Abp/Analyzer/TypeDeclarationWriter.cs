using System.Text;

namespace Len.Abp.Analyzer;

/// <summary>
/// 把生成器产出的成员代码包进类型的 partial 声明中。
/// </summary>
/// <remarks>
/// 生成器只输出相对类型体左对齐的片段，缩进与嵌套展开统一由这里处理，
/// 避免各生成器使用不同缩进基准。
/// </remarks>
internal static class TypeDeclarationWriter
{
    /// <summary>
    /// 把 <paramref name="body"/> 包进类型的 partial 声明中，嵌套类型会逐层展开。
    /// </summary>
    /// <param name="class">目标类型信息。</param>
    /// <param name="body">类型体内容，各行相对类型体左对齐；缩进由本方法统一添加。</param>
    /// <param name="selfDeclaration">
    /// 最内层类型的声明文本，用于附加基类型与接口；为 null 时按类型种类生成
    /// <c>partial class</c> 或 <c>partial record</c>。
    /// </param>
    /// <param name="leadingAttributes">
    /// 紧贴最内层类型声明的特性文本；为 null 时省略。
    /// </param>
    /// <returns>完整的 partial 类型声明。</returns>
    /// <remarks>
    /// 静态类必须保留 <c>static</c> 关键字：partial 声明的修饰符不一致时，
    /// 编译器会报「静态类不能包含实例成员」或「部分声明冲突」。
    /// <para>
    /// 特性必须写在最内层声明之前而不是整个块之前：拼在块首时编译器会把它绑定到
    /// 第一个类型声明（外层类型），嵌套视图的注册特性会因此挂错目标。
    /// </para>
    /// </remarks>
    public static string Write(
        ClassData @class,
        string body,
        string? selfDeclaration = null,
        string? leadingAttributes = null
    )
    {
        var declarations = new List<string>(@class.ContainingTypes.Count + 1);
        declarations.AddRange(@class.ContainingTypes);
        declarations.Add(selfDeclaration ?? @class.SelfDeclaration());

        var builder = new StringBuilder();
        var indent = string.Empty;

        for (var i = 0; i < declarations.Count; i++)
        {
            // 特性只贴最内层声明，且必须紧贴类型声明，中间不能有空行。
            if (
                i == declarations.Count - 1
                && leadingAttributes is { } attributes
                && !string.IsNullOrWhiteSpace(attributes)
            )
            {
                AppendAttributes(builder, attributes, indent);
            }

            AppendDeclaration(builder, declarations[i], indent);
            builder.Append(indent).Append("{\n");
            indent += "    ";
        }

        AppendBody(builder, body, indent);

        for (var i = declarations.Count - 1; i >= 0; i--)
        {
            indent = indent[..^4];
            builder.Append(indent).Append("}\n");
        }

        return builder.ToString();
    }

    /// <summary>
    /// 写入紧贴类型声明的特性行。
    /// </summary>
    /// <param name="builder">输出缓冲。</param>
    /// <param name="attributes">左对齐的特性文本，可含多行。</param>
    /// <param name="indent">当前嵌套层级的缩进。</param>
    private static void AppendAttributes(StringBuilder builder, string attributes, string indent)
    {
        foreach (var line in StripCommonIndent(attributes))
        {
            if (line.Length == 0)
            {
                continue;
            }

            builder.Append(indent).Append(line).Append('\n');
        }
    }

    /// <summary>
    /// 写入类型声明；多行声明（如带基类型列表）的续行保留相对缩进。
    /// </summary>
    private static void AppendDeclaration(StringBuilder builder, string declaration, string indent)
    {
        foreach (var line in StripCommonIndent(declaration))
        {
            if (line.Length == 0)
            {
                continue;
            }

            builder.Append(indent).Append(line).Append('\n');
        }
    }

    /// <summary>
    /// 写入类型体，去掉首尾空行后按当前嵌套层级缩进。
    /// </summary>
    /// <remarks>
    /// 生成器用原始字符串字面量拼装代码，字面量自身的缩进会残留在输出里。
    /// 这里以非空行的最小缩进为基准剥离，既消除字面量缩进，又保留成员内部的相对缩进。
    /// </remarks>
    private static void AppendBody(StringBuilder builder, string body, string indent)
    {
        var lines = StripCommonIndent(body);
        var start = 0;
        var end = lines.Length;

        while (start < end && lines[start].Length == 0)
        {
            start++;
        }

        while (end > start && lines[end - 1].Length == 0)
        {
            end--;
        }

        for (var i = start; i < end; i++)
        {
            if (lines[i].Length == 0)
            {
                builder.Append('\n');
                continue;
            }

            builder.Append(indent).Append(lines[i]).Append('\n');
        }
    }

    /// <summary>
    /// 去掉各行末尾空白，并剥离所有非空行的公共缩进。
    /// </summary>
    private static string[] StripCommonIndent(string text)
    {
        var lines = GeneratedCode.SplitLines(text);
        var commonIndent = int.MaxValue;

        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].TrimEnd();

            if (lines[i].Length == 0)
            {
                continue;
            }

            commonIndent = Math.Min(commonIndent, CountLeadingSpaces(lines[i]));
        }

        if (commonIndent is 0 or int.MaxValue)
        {
            return lines;
        }

        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Length > commonIndent)
            {
                lines[i] = lines[i][commonIndent..];
            }
            else if (lines[i].Length > 0)
            {
                lines[i] = string.Empty;
            }
        }

        return lines;
    }

    private static int CountLeadingSpaces(string line)
    {
        var count = 0;

        while (count < line.Length && char.IsWhiteSpace(line[count]))
        {
            count++;
        }

        return count;
    }
}
