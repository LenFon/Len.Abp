using System.Text;

namespace Len.Abp.Analyzer;

/// <summary>
/// 生成器所需的类型信息。
/// </summary>
/// <param name="Namespace">类型所在的命名空间；类型位于全局命名空间时为空字符串。</param>
/// <param name="Name">类型的声明名，含泛型参数（如 <c>MyControl&lt;T&gt;</c>）。</param>
/// <param name="SimpleName">类型的简单名，不含命名空间与泛型参数，用于生成构造函数名。</param>
/// <param name="FullName">类型的显示名，用于诊断信息与文件名。</param>
/// <param name="Type">类型的完全限定名，可直接用于生成代码。</param>
/// <param name="Methods">类型中所有方法的显示名。</param>
/// <param name="MethodNames">类型中所有方法的简单名称，用于精确匹配分部钩子。</param>
/// <param name="ContainingTypes">外层类型的声明文本（含 <c>partial class</c> 等关键字），由最外层到最内层排列；顶层类型为空集合。</param>
/// <param name="IsRecord">类型是否为记录声明，生成 partial 声明时必须保留 <c>record</c> 关键字。</param>
/// <param name="HasAnyConstructor">类型是否显式声明了实例构造函数（不含编译器合成的构造函数）。</param>
/// <param name="HasParameterlessConstructor">类型是否显式声明了无参实例构造函数。</param>
public readonly record struct ClassData(
    string Namespace,
    string Name,
    string SimpleName,
    string FullName,
    string Type,
    IReadOnlyList<string> Methods,
    IReadOnlyList<string> MethodNames,
    IReadOnlyList<string> ContainingTypes,
    bool IsRecord,
    bool HasAnyConstructor,
    bool HasParameterlessConstructor)
{
    /// <summary>
    /// 命名空间声明；类型位于全局命名空间时返回空字符串。
    /// </summary>
    public string NamespaceDeclaration =>
        string.IsNullOrEmpty(Namespace) ? string.Empty : $"namespace {Namespace};";

    /// <summary>
    /// 用于源文件 hint name 的类型名。hint name 不允许包含 <c>&lt;</c> 等字符，
    /// 因此这里去掉泛型参数列表。
    /// </summary>
    public string HintName => RemoveGenericArguments(FullName);

    /// <summary>
    /// 生成的构造函数能否通过 <c>: this()</c> 链到无参构造函数。
    /// </summary>
    /// <remarks>
    /// 没有显式构造函数时编译器会合成无参构造函数，链式调用同样成立；
    /// 只有「声明了构造函数但没有无参版本」时才会编译失败。
    /// </remarks>
    public bool CanChainToParameterlessConstructor => !HasAnyConstructor || HasParameterlessConstructor;

    /// <summary>
    /// 判断类型是否声明了指定名称的方法。
    /// </summary>
    /// <param name="methodName">方法名。</param>
    /// <returns>存在同名方法返回 true。</returns>
    /// <remarks>
    /// 必须按简单名称精确匹配：用显示名做 <c>Contains</c> 判断时，
    /// <c>CoerceTitle</c> 会被 <c>CoerceTitle2</c> 误命中，生成对不存在方法的调用。
    /// </remarks>
    public bool HasMethod(string methodName) =>
        MethodNames.Any(name => string.Equals(name, methodName, StringComparison.Ordinal));

    /// <summary>
    /// 把 <paramref name="body"/> 包进类型的 partial 声明中，嵌套类型会逐层展开。
    /// </summary>
    /// <param name="body">类型体内容，各行相对类型体左对齐；缩进由本方法统一添加。</param>
    /// <param name="selfDeclaration">
    /// 最内层类型的声明文本，用于附加基类型与接口；为 null 时按类型种类生成 <c>partial class</c> 或 <c>partial record</c>。
    /// </param>
    /// <returns>完整的 partial 类型声明。</returns>
    public string WrapInTypeDeclarations(string body, string? selfDeclaration = null)
    {
        var keyword = IsRecord ? "record" : "class";
        var declarations = new List<string>(ContainingTypes.Count + 1);
        declarations.AddRange(ContainingTypes);
        declarations.Add(selfDeclaration ?? $"partial {keyword} {Name}");

        var builder = new StringBuilder();
        var indent = string.Empty;

        foreach (var declaration in declarations)
        {
            AppendDeclaration(builder, declaration, indent);
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
        var lines = SplitLines(text);
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

    private static string[] SplitLines(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

    private static string RemoveGenericArguments(string value)
    {
        var builder = new StringBuilder(value.Length);
        var depth = 0;

        foreach (var character in value)
        {
            if (character == '<')
            {
                depth++;
                continue;
            }

            if (character == '>')
            {
                depth--;
                continue;
            }

            if (depth > 0)
            {
                continue;
            }

            builder.Append(character is ',' or ' ' ? '_' : character);
        }

        return builder.ToString();
    }
}
