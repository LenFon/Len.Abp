using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Len.Abp.Analyzer;

/// <summary>
/// 跨文件的视图名称诊断。
/// </summary>
/// <remarks>
/// 导航按 <c>ViewName</c> 解析视图，重名会让其中一个静默失效。
/// 这类检查必须聚合所有视图才能判断，因此与单类的生成逻辑分开。
/// </remarks>
internal static class ViewNameDiagnostics
{
    /// <summary>
    /// 报告重复的视图名称。
    /// </summary>
    /// <param name="context">源生成上下文。</param>
    /// <param name="views">所有视图的类上下文。</param>
    /// <remarks>
    /// <para>
    /// 空名称不参与判定——它由 <c>LENABP004</c> 报告。
    /// </para>
    /// <para>
    /// 按 <see cref="StringComparer.Ordinal"/> 分组，即大小写敏感：<c>"Demo"</c> 与
    /// <c>"demo"</c> 不算重复。这与键控服务的解析一致（MS DI 的键比较同样区分大小写），
    /// 两个视图都能各自导航到。注意 <c>LENABP004</c> 的成员名重复检查是不敏感的，
    /// 两处口径不同是因为它们约束的对象不同：那里是同一类型上的成员名，
    /// 这里是跨类型的导航键。
    /// </para>
    /// <para>
    /// 先按类型去重：<c>GetClasses</c> 为每个特性产出一个条目，
    /// 虽然 <c>ViewAttribute</c> 不允许重复应用，但两个特性名
    /// （<c>ViewAttribute</c> 与 <c>ViewAttribute&lt;T&gt;</c>）都可能命中同一个类。
    /// </para>
    /// </remarks>
    public static void ReportDuplicateViewNames(SourceProductionContext context, IReadOnlyList<ClassContext> views)
    {
        var entries = views
            .Select(view => (Context: view, Data: MvvmData.Parse(view.Attributes.First())))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Data.ViewName))
            .GroupBy(entry => entry.Context.ClassSymbol, SymbolEqualityComparer.Default)
            .Select(group => group.First())
            .ToArray();

        foreach (var group in entries.GroupBy(entry => entry.Data.ViewName!, StringComparer.Ordinal))
        {
            if (group.Count() <= 1)
            {
                continue;
            }

            var names = string.Join("、", group.Select(entry => entry.Context.ClassSymbol.ToDisplayString()));
            var location = group
                .Select(entry => GetViewNameLocation(entry.Context))
                .FirstOrDefault(candidate => candidate is not null)
                ?? group.First().Context.ClassSyntax.Identifier.GetLocation();

            context.ReportDiagnostic(
                Diagnostic.Create(DiagnosticDescriptors.DuplicateViewName, location, group.Key, names)
            );
        }
    }

    /// <summary>
    /// 取 <c>ViewName</c> 命名实参的位置。
    /// </summary>
    /// <param name="classContext">类上下文。</param>
    /// <returns>命名实参位置；取不到时返回 <see langword="null"/>。</returns>
    private static Location? GetViewNameLocation(ClassContext classContext)
    {
        if (classContext.Attributes.FirstOrDefault()?.ApplicationSyntaxReference?.GetSyntax()
            is not AttributeSyntax syntax)
        {
            return null;
        }

        return syntax.ArgumentList?.Arguments
            .FirstOrDefault(argument =>
                string.Equals(argument.NameEquals?.Name.Identifier.Text, "ViewName", StringComparison.Ordinal)
            )
            ?.GetLocation();
    }
}
