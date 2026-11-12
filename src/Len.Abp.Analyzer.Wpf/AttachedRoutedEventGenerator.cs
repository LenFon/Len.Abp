using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis;

[Generator]
internal class AttachedRoutedEventGenerator()
    : RoutedEventGeneratorBase(
        "AREG",
        "Len.Abp.Wpf.AttachedRoutedEventAttribute",
        "Len.Abp.Wpf.AttachedRoutedEventAttribute`1"
    )
{
    /// <inheritdoc/>
    protected override string GeneratorName => nameof(AttachedRoutedEventGenerator);

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
                "[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]",
                $"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{context.Version}\")]",
                $"public static void Add{context.Data.Name}Handler(global::System.Windows.DependencyObject d, {context.Data.Type} handler)",
                "{",
                "    if (d is not global::System.Windows.UIElement element) return;",
                string.Empty,
                $"    element.AddHandler({context.Data.Name}Event, handler);",
                "}"
            ),
            description + GeneratedCode.Lines(
                "[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]",
                $"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{context.Version}\")]",
                $"public static void Remove{context.Data.Name}Handler(global::System.Windows.DependencyObject d, {context.Data.Type} handler)",
                "{",
                "    if (d is not global::System.Windows.UIElement element) return;",
                string.Empty,
                $"    element.RemoveHandler({context.Data.Name}Event, handler);",
                "}"
            ),
            GeneratedCode.Lines(
                "[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]",
                $"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{context.Version}\")]",
                $"public static void Raise{context.Data.Name}Event(global::System.Windows.DependencyObject d, {context.Data.EventArgs}? args = default)",
                "{",
                "    if (d is not global::System.Windows.UIElement element) return;",
                string.Empty,
                "    args ??= new();",
                $"    args.RoutedEvent = {context.Data.Name}Event;",
                "    element.RaiseEvent(args);",
                "}"
            )
        );
    }
}
