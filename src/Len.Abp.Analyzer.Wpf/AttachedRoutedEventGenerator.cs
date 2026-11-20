using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis;

[Generator]
internal class AttachedRoutedEventGenerator()
    : RoutedEventGeneratorBase(
        "Len.Abp.Wpf.AttachedRoutedEventAttribute",
        "Len.Abp.Wpf.AttachedRoutedEventAttribute`1"
    )
{
    /// <inheritdoc/>
    protected override string GeneratorName => nameof(AttachedRoutedEventGenerator);

    /// <inheritdoc/>
    /// <remarks>
    /// 附加版本只生成静态的 <c>Add</c>/<c>Remove</c>/<c>Raise</c> 方法，
    /// 静态类宿主完全合法，因此不产出实例成员。
    /// </remarks>
    protected override bool GeneratesInstanceMembers => false;

    /// <inheritdoc/>
    protected override string EventKindDescription => "附加路由事件";

    /// <inheritdoc/>
    protected override string GenerateMembers((ClassData Class, RoutedEventData Data, string Version) context)
    {
        var description = context.Data.Description is { } value
            ? GeneratedCode.Lines("/// <summary>", $"/// {value.ToXmlCommentText()}", "/// </summary>")
            : string.Empty;

        return GeneratedCode.Join(
            description + GeneratedCode.Lines(
                GeneratedCode.GeneratedMemberAttributes(GeneratorName, context.Version),
                $"public static void Add{context.Data.Name}Handler(global::System.Windows.DependencyObject d, {context.Data.Type} handler)",
                "{",
                "    if (d is not global::System.Windows.UIElement element) return;",
                string.Empty,
                $"    element.AddHandler({context.Data.Name}Event, handler);",
                "}"
            ),
            description + GeneratedCode.Lines(
                GeneratedCode.GeneratedMemberAttributes(GeneratorName, context.Version),
                $"public static void Remove{context.Data.Name}Handler(global::System.Windows.DependencyObject d, {context.Data.Type} handler)",
                "{",
                "    if (d is not global::System.Windows.UIElement element) return;",
                string.Empty,
                $"    element.RemoveHandler({context.Data.Name}Event, handler);",
                "}"
            ),
            GeneratedCode.Lines(
                GeneratedCode.GeneratedMemberAttributes(GeneratorName, context.Version),
                $"public static void Raise{context.Data.Name}Event(global::System.Windows.DependencyObject d, {context.Data.EventArgs}? args = default)",
                "{",
                "    if (d is not global::System.Windows.UIElement element) return;",
                string.Empty,
                // 不能生成 args ??= new()：WPF 的事件参数类型大多没有公共无参构造
                // （MouseButtonEventArgs、KeyEventArgs 等由 WPF 内部构造并传入），
                // 生成 new() 会让所有实用场景编译失败（CS7036）。
                "    global::System.ArgumentNullException.ThrowIfNull(args);",
                string.Empty,
                $"    args.RoutedEvent = {context.Data.Name}Event;",
                "    element.RaiseEvent(args);",
                "}"
            )
        );
    }
}
