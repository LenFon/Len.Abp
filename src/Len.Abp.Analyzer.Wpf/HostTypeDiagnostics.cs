using Microsoft.CodeAnalysis;

namespace Len.Abp.Analyzer;

/// <summary>
/// 宿主类型不满足生成器要求的原因。
/// </summary>
internal enum HostTypeProblem
{
    /// <summary>
    /// 宿主类型合法。
    /// </summary>
    None = 0,

    /// <summary>
    /// 宿主是静态类，无法声明实例成员。
    /// </summary>
    StaticClass,

    /// <summary>
    /// 宿主是结构体（含 <c>record struct</c>），不具备视图所需的引用语义。
    /// </summary>
    Struct,
}

/// <summary>
/// 校验生成器特性的宿主类型是否合法。
/// </summary>
/// <remarks>
/// <para>
/// 视图、依赖属性与路由事件三个生成器都会为宿主类型追加成员，
/// 但各自能接受的宿主不同：附加路由事件只生成静态方法，静态类宿主完全合法；
/// 附加依赖属性同样只生成静态成员，但注册走 <c>DependencyProperty.Register</c>，
/// 运行期要求宿主派生自 <c>DependencyObject</c>——这条限制由文档约束，这里不检查。
/// 非附加版本生成实例成员，静态类无法承载（CS0708/CS1057）。
/// 视图还额外要求引用类型（结构体无法承载 <c>DataContext</c> 与接口的实例成员语义）。
/// </para>
/// <para>
/// 判断集中在这里而不是各生成器各写一遍：漏掉一处就会产出无法编译的代码，
/// 而错误信息会分散在 CS0708、CS0714、CS0261 等多条规则上，使用者很难定位根因。
/// </para>
/// </remarks>
internal static class HostTypeDiagnostics
{
    /// <summary>
    /// 判断宿主类型能否作为视图。
    /// </summary>
    /// <param name="type">视图类型符号。</param>
    /// <returns>合法时返回 <see cref="HostTypeProblem.None"/>。</returns>
    /// <remarks>
    /// <c>record</c> 的 <see cref="TypeKind"/> 是 <see cref="TypeKind.Class"/>，
    /// 因此本检查同时放行 <c>class</c> 与 <c>record</c>，
    /// 拦截 <c>struct</c> 与 <c>record struct</c>。
    /// </remarks>
    public static HostTypeProblem GetViewProblem(INamedTypeSymbol type) =>
        type switch
        {
            { IsStatic: true } => HostTypeProblem.StaticClass,
            { TypeKind: TypeKind.Struct } => HostTypeProblem.Struct,
            _ => HostTypeProblem.None,
        };

    /// <summary>
    /// 判断宿主类型能否承载非附加依赖属性与路由事件。
    /// </summary>
    /// <param name="type">宿主类型符号。</param>
    /// <returns>宿主是静态类或结构体时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// <para>
    /// 静态类无法承载实例成员（CS0708/CS1057）。
    /// </para>
    /// <para>
    /// <b>结构体同样不行</b>：生成器写的是 <c>partial class</c>（或 <c>partial record</c>），
    /// 与用户的 <c>struct</c> / <c>record struct</c> 声明冲突（CS0261），
    /// 且生成的 <c>GetValue</c>/<c>SetValue</c> 调用在结构体上不存在（CS0103）。
    /// 早期版本认为"结构体可以声明实例成员，编译层面合法"而放行，
    /// 但那只对生成器写对类型关键字的情况成立，实际产出的是类声明。
    /// </para>
    /// </remarks>
    public static bool IsInvalidInstanceMemberHost(INamedTypeSymbol type) =>
        type.IsStatic || type.TypeKind == TypeKind.Struct;

    /// <summary>
    /// 把问题翻译成可读的原因说明。
    /// </summary>
    /// <param name="problem">宿主类型问题。</param>
    /// <returns>用于诊断消息的原因说明。</returns>
    public static string Describe(HostTypeProblem problem) =>
        problem switch
        {
            HostTypeProblem.StaticClass => "是静态类",
            HostTypeProblem.Struct => "是结构体",
            _ => "类型不合法",
        };
}
