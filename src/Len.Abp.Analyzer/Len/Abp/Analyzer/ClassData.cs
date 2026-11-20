using System.Text;

namespace Len.Abp.Analyzer;

/// <summary>
/// 生成器所需的类型信息。
/// </summary>
/// <param name="Namespace">类型所在的命名空间；类型位于全局命名空间时为空字符串。</param>
/// <param name="DeclarationName">
/// 类型在 <c>partial</c> 声明中使用的名称，泛型类型带<b>未闭合</b>的类型参数
/// （如 <c>MyControl&lt;T&gt;</c>）。只能用于声明位置。
/// </param>
/// <param name="SimpleName">类型的简单名，不含命名空间与泛型参数，用于生成构造函数名。</param>
/// <param name="FullName">类型的显示名，用于诊断信息与文件名。</param>
/// <param name="Type">类型的完全限定名，可直接用于生成代码。</param>
/// <param name="MethodNames">类型中所有方法的简单名称，用于精确匹配分部钩子。</param>
/// <param name="ContainingTypes">外层类型的声明文本（含 <c>partial class</c> 等关键字），由最外层到最内层排列；顶层类型为空集合。</param>
/// <param name="IsRecord">类型是否为记录声明，生成 partial 声明时必须保留 <c>record</c> 关键字。</param>
/// <param name="HasAnyConstructor">类型是否显式声明了实例构造函数（不含编译器合成的构造函数）。</param>
/// <param name="HasParameterlessConstructor">类型是否显式声明了无参实例构造函数。</param>
/// <param name="IsStatic">类型是否为静态类，生成 partial 声明时必须保留 <c>static</c> 关键字。</param>
/// <param name="IsSealed">类型是否为密封类，生成 <c>protected</c> 成员时必须降级为 <c>public</c>。</param>
/// <remarks>
/// <b><see cref="DeclarationName"/> 与 <see cref="Type"/> 不能互换</b>：
/// 前者是未闭合的声明名（<c>PageViewModel&lt;T&gt;</c>），只能出现在 <c>partial</c> 声明里；
/// 后者是闭合的完全限定名（<c>global::Demo.PageViewModel&lt;int&gt;</c>），
/// 用于 <c>typeof()</c> 与特性参数。混用会分别得到 CS1001/CS0264 与 CS0416。
/// 不要合并成一个字段：一个名称无法同时满足两种语义，泛型类型上必然出错。
/// </remarks>
public readonly record struct ClassData(
    string Namespace,
    string DeclarationName,
    string SimpleName,
    string FullName,
    string Type,
    IReadOnlyList<string> MethodNames,
    IReadOnlyList<string> ContainingTypes,
    bool IsRecord,
    bool HasAnyConstructor,
    bool HasParameterlessConstructor,
    bool IsStatic = false,
    bool IsSealed = false)
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
    /// <remarks>
    /// 泛型类型必须保留参数个数（arity）：<c>Page</c> 与 <c>Page&lt;T&gt;</c> 去掉参数列表后
    /// 都变成 <c>Page</c>，两者会产出同名 hint name，Roslyn 直接抛异常，
    /// 该类一个源文件都不产出（CS8785）。用反引号加 arity 与元数据名保持一致，
    /// 非泛型类型保持原样，不改变既有文件名。
    /// </remarks>
    public string HintName => RemoveGenericArguments(FullName) + GenericAritySuffix;

    /// <summary>
    /// 分部声明的目标标识：同一泛型类型的所有闭合实例共享同一个值。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>PageViewModel&lt;int&gt;</c> 与 <c>PageViewModel&lt;string&gt;</c> 是同一个分部声明
    /// <c>partial class PageViewModel&lt;T&gt;</c> 的两个闭合实例，因此必须按
    /// <see cref="DeclarationName"/>（未闭合的声明名）而不是 <see cref="Type"/>（闭合的完全限定名）判定。
    /// </para>
    /// <para>
    /// 用闭合名做去重键会让两个实例各生成一份分部声明，成员重复（CS0102）；
    /// 用 <see cref="HintName"/> 做键则会丢掉外层类型链，嵌套类型互相覆盖。
    /// </para>
    /// </remarks>
    public string DeclarationKey => $"{Namespace}|{string.Join("|", ContainingTypes)}|{DeclarationName}";

    /// <summary>
    /// 生成的构造函数能否通过 <c>: this()</c> 链到无参构造函数。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 没有显式构造函数时返回 <see langword="true"/>：生成器会自己补一个无参构造函数
    /// （见 <c>ViewSourceWriter.GetConstructor</c>），链式调用成立。
    /// </para>
    /// <para>
    /// 不能依赖"编译器会合成无参构造"：分部类一旦有构造函数（包括生成器自己加的），
    /// 编译器就不再合成，<c>: this()</c> 会找不到目标（CS7036）。
    /// </para>
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
    /// <param name="leadingAttributes">
    /// 紧贴最内层类型声明的特性文本；为 null 时省略。特性必须落在最内层类型上，
    /// 拼在整个块之前会被编译器绑定到外层类型。
    /// </param>
    /// <returns>完整的 partial 类型声明。</returns>
    public string WrapInTypeDeclarations(string body, string? selfDeclaration = null, string? leadingAttributes = null) =>
        TypeDeclarationWriter.Write(this, body, selfDeclaration, leadingAttributes);

    /// <summary>
    /// 生成最内层类型的声明文本，保留 <c>record</c> 与 <c>static</c> 关键字。
    /// </summary>
    /// <param name="baseTypes">基类型与接口列表；为空时不生成冒号。</param>
    /// <returns>形如 <c>partial record Sample : global::System.IDisposable</c> 的声明文本。</returns>
    /// <remarks>
    /// 视图与视图模型都必须用同一段逻辑决定关键字：两处各写一遍时，
    /// 只要有一处漏掉 <c>record</c>，编译器就会报 CS0261「部分声明必须为所有类、所有记录」。
    /// </remarks>
    public string SelfDeclaration(IReadOnlyList<string>? baseTypes = null)
    {
        var keyword = IsRecord ? "record" : "class";
        // 修饰符顺序必须是 `static partial class`，写成 `partial static class` 会报 CS0267。
        var modifier = IsStatic ? "static " : string.Empty;
        var declaration = $"{modifier}partial {keyword} {DeclarationName}";

        return baseTypes is { Count: > 0 }
            ? $"{declaration} : {string.Join(", ", baseTypes)}"
            : declaration;
    }

    /// <summary>
    /// 泛型参数个数后缀；非泛型类型为空字符串。
    /// </summary>
    /// <remarks>
    /// 从 <see cref="FullName"/> 的显示名统计尖括号层数不够精确（嵌套泛型会重复计数），
    /// 因此按 <see cref="DeclarationName"/> 上的参数列表计算——它是最内层类型的声明名，
    /// 只含自身的泛型参数。
    /// </remarks>
    private string GenericAritySuffix
    {
        get
        {
            var start = DeclarationName.IndexOf('<');

            if (start < 0)
            {
                return string.Empty;
            }

            var depth = 0;
            var arity = 1;

            for (var i = start; i < DeclarationName.Length; i++)
            {
                switch (DeclarationName[i])
                {
                    case '<':
                        depth++;
                        break;
                    case '>':
                        depth--;
                        break;
                    case ',' when depth == 1:
                        arity++;
                        break;
                }
            }

            return $"`{arity}";
        }
    }

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
