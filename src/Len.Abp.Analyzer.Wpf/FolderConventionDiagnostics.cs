using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Len.Abp.Analyzer;

/// <summary>
/// 校验视图与视图模型是否放在约定的目录下。
/// </summary>
/// <remarks>
/// <para>
/// 约定：视图在 <c>Views</c> 目录，视图模型在 <c>ViewModels</c> 目录。
/// 两者混放时，读代码的人无法从目录判断某个类型属于哪一类，
/// 生成器产出的两个文件也会与源文件的组织方式对不上。
/// </para>
/// <para>
/// 只检查路径中是否出现对应的目录段，不要求它是项目根下的第一层：
/// 按功能分目录（<c>Features/Orders/Views/OrderView.cs</c>）同样满足约定。
/// 比较忽略大小写——Windows 上目录名大小写不敏感，<c>views</c> 与 <c>Views</c> 是同一个目录。
/// </para>
/// <para>
/// 源文件没有路径时（生成器测试的内存源码、从标准输入编译）不检查：
/// 那种情况下无从判断目录，报告诊断只会让所有生成器测试都命中。
/// </para>
/// </remarks>
internal static class FolderConventionDiagnostics
{
    /// <summary>
    /// 视图应位于的目录名。
    /// </summary>
    private const string ViewsFolder = "Views";

    /// <summary>
    /// 视图模型应位于的目录名。
    /// </summary>
    private const string ViewModelsFolder = "ViewModels";

    /// <summary>
    /// 报告不在 <c>Views</c> 目录下的视图。
    /// </summary>
    /// <param name="context">源生成上下文。</param>
    /// <param name="classContext">类上下文。</param>
    public static void ReportViewFolder(SourceProductionContext context, ClassContext classContext)
    {
        if (!AttributeDeduplication.IsFirstOfType(classContext))
        {
            return;
        }

        if (GetFilePath(classContext) is not { } path || IsInFolder(path, ViewsFolder))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.ViewMustBeInViewsFolder,
                classContext.ClassSyntax.Identifier.GetLocation(),
                classContext.ClassSymbol.Name,
                path
            )
        );
    }

    /// <summary>
    /// 报告不在 <c>ViewModels</c> 目录下的视图模型。
    /// </summary>
    /// <param name="context">源生成上下文。</param>
    /// <param name="classContext">类上下文。</param>
    /// <param name="viewModel">视图模型类型符号。</param>
    /// <remarks>
    /// 只对 <c>partial</c> 的视图模型报告：非 <c>partial</c> 时生成器不会为它追加任何成员，
    /// 目录问题与生成器无关，报告只会增加噪音（那种情况已由 <c>LENABP006</c>/<c>LENABP009</c> 覆盖）。
    /// </remarks>
    public static void ReportViewModelFolder(
        SourceProductionContext context,
        ClassContext classContext,
        INamedTypeSymbol viewModel)
    {
        if (!AttributeDeduplication.IsFirstOfType(classContext))
        {
            return;
        }

        if (GetFilePath(viewModel) is not { } path || IsInFolder(path, ViewModelsFolder))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.ViewModelMustBeInViewModelsFolder,
                viewModel.Locations.FirstOrDefault() ?? classContext.ClassSyntax.Identifier.GetLocation(),
                viewModel.Name,
                path
            )
        );
    }

    /// <summary>
    /// 判断路径中是否包含指定名称的目录段。
    /// </summary>
    /// <param name="path">源文件路径。</param>
    /// <param name="folderName">目录名。</param>
    /// <returns>包含该目录段时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// 按路径分隔符切分后逐段比较，而不是用 <c>Contains</c>：
    /// 后者会让 <c>MyViews/OrderView.cs</c>、<c>ViewsArchive/</c> 这类名字误判为合规。
    /// </remarks>
    private static bool IsInFolder(string path, string folderName)
    {
        var segments = path.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);

        // 最后一段是文件名，不参与目录比较。
        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (string.Equals(segments[i], folderName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 取类型声明所在源文件的路径。
    /// </summary>
    /// <param name="classContext">类上下文。</param>
    /// <returns>源文件路径；取不到或为空时返回 <see langword="null"/>。</returns>
    private static string? GetFilePath(ClassContext classContext) =>
        GetFilePath(classContext.ClassSyntax.SyntaxTree);

    /// <summary>
    /// 取类型符号所在源文件的路径。
    /// </summary>
    /// <param name="type">类型符号。</param>
    /// <returns>源文件路径；取不到或为空时返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// 分部类型可能有多处声明，取第一处即可：同一类型的所有分部声明必须放在同一个项目里，
    /// 目录不一致本身就是需要修的问题，报告其中一处足以定位。
    /// </remarks>
    private static string? GetFilePath(INamedTypeSymbol type) =>
        type.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree is { } tree ? GetFilePath(tree) : null;

    /// <summary>
    /// 取语法树对应的源文件路径。
    /// </summary>
    /// <param name="tree">语法树。</param>
    /// <returns>源文件路径；为空时返回 <see langword="null"/>。</returns>
    private static string? GetFilePath(SyntaxTree tree) =>
        string.IsNullOrEmpty(tree.FilePath) ? null : tree.FilePath;
}
