using Microsoft.CodeAnalysis;

namespace Len.Abp.Analyzer;

/// <summary>
/// 校验路由事件特性的处理程序类型：必须是委托，且签名可被生成器使用。
/// </summary>
/// <remarks>
/// <para>
/// 生成器从委托的第二个参数取事件参数类型（<c>DelegateInvokeMethod.Parameters[1]</c>），
/// 因此委托必须至少有两个参数。参数不足时提前报告，避免生成器抛
/// <see cref="IndexOutOfRangeException"/> 并退化成 <c>LENABP005</c>。
/// </para>
/// <para>
/// 第二个参数还必须派生自 <c>System.Windows.RoutedEventArgs</c>：生成的
/// <c>Raise</c> 方法会写 <c>args.RoutedEvent = ...</c> 并调用
/// <c>element.RaiseEvent(args)</c>，两者都要求该类型。不校验时使用者看到的是
/// CS1061 与 CS1503 两条错误，位置指向生成文件，很难反推到特性声明。
/// </para>
/// </remarks>
internal static class RoutedEventHandlerDiagnostics
{
    /// <summary>
    /// 校验路由事件特性的处理程序类型。
    /// </summary>
    /// <param name="attribute">路由事件特性数据。</param>
    /// <returns>合法时返回 <see langword="null"/>；否则返回可读的错误说明。</returns>
    public static string? Validate(AttributeData attribute)
    {
        if (AttributeFamily.GetHandlerType(attribute) is not { } handlerType)
        {
            return null;
        }

        var attributeName = attribute.AttributeClass?.Name ?? "RoutedEventAttribute";

        if (!AttributeFamily.IsDelegate(handlerType))
        {
            return $"{attributeName} 的处理程序类型 {handlerType.ToDisplayString()} 不是委托类型。";
        }

        var parameters = ((INamedTypeSymbol)handlerType).DelegateInvokeMethod?.Parameters;

        if (parameters is not { Length: >= 2 } list)
        {
            return $"{attributeName} 的处理程序类型 {handlerType.ToDisplayString()} 必须至少有两个参数（sender 与事件参数）。";
        }

        var eventArgs = list[1].Type;

        return AttributeFamily.IsRoutedEventArgs(eventArgs)
            ? null
            : $"{attributeName} 的处理程序类型 {handlerType.ToDisplayString()} 的第二个参数是 {eventArgs.ToDisplayString()}，"
                + "必须派生自 System.Windows.RoutedEventArgs。生成的 Raise 方法需要写入 args.RoutedEvent 并调用 RaiseEvent。";
    }
}
