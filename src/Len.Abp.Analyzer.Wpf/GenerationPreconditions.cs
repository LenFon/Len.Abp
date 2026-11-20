namespace Len.Abp.Analyzer;

/// <summary>
/// 判断类上下文是否满足生成代码的前置条件。
/// </summary>
/// <remarks>
/// 视图、依赖属性与路由事件的 <c>PrepareData</c> 都要先做同样的两项检查，
/// 抽出来避免条件写得不一致。
/// 检查失败时由 <c>ReportDiagnostics</c> 报告诊断，这里只负责判断。
/// </remarks>
internal static class GenerationPreconditions
{
    /// <summary>
    /// 判断是否可以继续生成代码。
    /// </summary>
    /// <param name="classContext">类上下文。</param>
    /// <returns>类型已声明 <c>partial</c> 且特性参数合法时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// 参数非法会产出无法编译的源文件；类型未声明 <c>partial</c> 会与编译器的 CS0260 重复报错。
    /// 两种情况都只报告诊断，不生成代码。
    /// </remarks>
    public static bool IsSatisfied(ClassContext classContext) =>
        PartialTypeDiagnostics.IsPartial(classContext)
        && AttributeArgumentDiagnostics.Validate(classContext) is null;
}
