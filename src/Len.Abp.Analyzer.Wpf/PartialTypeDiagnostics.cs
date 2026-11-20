using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Len.Abp.Analyzer;

/// <summary>
/// 检查应用了生成器特性的类型是否声明为 <c>partial</c>，不满足时报告 <c>LENABP002</c>。
/// </summary>
/// <remarks>
/// 生成代码会逐层展开 <c>partial</c> 声明，因此外层类型也必须声明。
/// 只在当前特性是该类型的第一个匹配特性时报告，避免同一问题重复出现。
/// </remarks>
internal static class PartialTypeDiagnostics
{
    /// <summary>
    /// 检查类型自身与所有外层类型是否都声明了 <c>partial</c>，不满足时报告 <c>LENABP002</c>。
    /// </summary>
    /// <param name="context">源生成上下文。</param>
    /// <param name="classContext">类上下文。</param>
    public static void Report(SourceProductionContext context, ClassContext classContext)
    {
        if (!AttributeDeduplication.IsFirstOfType(classContext))
        {
            return;
        }

        if (FindNonPartialType(classContext.ClassSyntax) is not { } missing)
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.TypeMustBePartial,
                missing.Identifier.GetLocation(),
                missing.Identifier.Text
            )
        );
    }

    /// <summary>
    /// 判断类型自身与所有外层类型是否都声明了 <c>partial</c>。
    /// </summary>
    /// <param name="classContext">类上下文。</param>
    /// <returns>全部声明了 <c>partial</c> 时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// 生成器在 <c>PrepareData</c> 里用它决定是否跳过生成：只报告诊断而不跳过，
    /// 使用者会同时看到 <c>LENABP002</c> 与编译器报的 <c>CS0260</c>。
    /// </remarks>
    public static bool IsPartial(ClassContext classContext) => FindNonPartialType(classContext.ClassSyntax) is null;

    /// <summary>
    /// 找出第一个未声明 <c>partial</c> 的类型声明。
    /// </summary>
    /// <param name="syntax">最内层类型声明。</param>
    /// <returns>未声明 <c>partial</c> 的类型；全部满足时返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// 只返回最内层缺失的那一个：外层缺失时内层的生成同样失败，
    /// 但一次报一串只会淹没真正需要修改的位置。
    /// </remarks>
    private static TypeDeclarationSyntax? FindNonPartialType(TypeDeclarationSyntax syntax)
    {
        for (var current = syntax; current is not null; current = current.Parent as TypeDeclarationSyntax)
        {
            if (!current.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return current;
            }
        }

        return null;
    }
}
