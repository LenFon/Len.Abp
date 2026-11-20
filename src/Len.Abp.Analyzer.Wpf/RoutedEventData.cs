using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Len.Abp.Analyzer;

/// <summary>
/// 生成路由事件所需的全部数据。
/// </summary>
/// <param name="Name">事件名称。</param>
/// <param name="Type">事件处理程序委托的完全限定类型名。</param>
/// <param name="EventArgs">事件参数类型的完全限定名。</param>
/// <param name="Strategy">路由策略。</param>
/// <param name="Description">事件的描述。</param>
/// <param name="Category">事件的分类。</param>
internal readonly record struct RoutedEventData(
    string Name,
    string Type,
    string EventArgs,
    string Strategy,
    string? Description,
    string? Category
) : INamedMemberData
{
    /// <summary>
    /// 从路由事件特性数据解析事件数据。
    /// </summary>
    /// <param name="attribute">路由事件特性数据。</param>
    /// <returns>解析后的事件数据。</returns>
    public static RoutedEventData Parse(AttributeData attribute)
    {
        if (attribute is null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }

        var name = attribute.GetNameArgument() ?? string.Empty;
        var strategy = $"global::{attribute.ConstructorArguments.ElementAt(1).ToCSharpString()}";
        var typeSymbol =
            attribute.GetTypeArgument(0, 2);

        string type;
        string eventArgs;
        if (typeSymbol is null)
        {
            type = "global::System.Windows.RoutedEventHandler";
            eventArgs = "global::System.Windows.RoutedEventArgs";
        }
        else
        {
            type = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // 事件参数取委托的第二个参数。参数不足时不能取下标：
            // 校验层（LENABP004）会拦截这种声明，这里退化为默认事件参数，
            // 保证即使校验被绕过也不会抛 IndexOutOfRangeException。
            var parameters = (typeSymbol as INamedTypeSymbol)?.DelegateInvokeMethod?.Parameters;

            eventArgs = parameters is { } list && list.Length >= 2
                ? list[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                : "global::System.Windows.RoutedEventArgs";
        }

        return new RoutedEventData(
            name,
            type,
            eventArgs,
            strategy,
            attribute.GetNamedArgumentString("Description"),
            attribute.GetNamedArgumentString("Category")
        );
    }
}
