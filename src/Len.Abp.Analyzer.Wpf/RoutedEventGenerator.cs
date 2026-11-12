using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis;

[Generator]
internal class RoutedEventGenerator()
    : RoutedEventGeneratorBase("REG", "Len.Abp.Wpf.RoutedEventAttribute", "Len.Abp.Wpf.RoutedEventAttribute`1")
{
    /// <inheritdoc/>
    protected override string GeneratorName => nameof(RoutedEventGenerator);

    /// <inheritdoc/>
    protected override string EventKindDescription => "路由事件";

    /// <inheritdoc/>
    protected override string GenerateMembers((ClassData Class, RoutedEventData Data, string Version) context)
    {
        var documentation = context.Data.Description is { } description
            ? GeneratedCode.Lines("/// <summary>", $"/// {description.ToXmlCommentText()}", "/// </summary>")
            : string.Empty;

        return GeneratedCode.Join(
            documentation + GeneratedCode.Lines(
                "[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]",
                $"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{context.Version}\")]",
                context.Data.Category is { } category ? $"[global::System.ComponentModel.Category(\"{category.ToCSharpStringLiteral()}\")]" : null,
                $"public event {context.Data.Type} {context.Data.Name}",
                "{",
                $"    add {{ AddHandler({context.Data.Name}Event, value); }}",
                $"    remove {{ RemoveHandler({context.Data.Name}Event, value); }}",
                "}"
            ),
            GeneratedCode.Lines(
                "[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]",
                $"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{context.Version}\")]",
                $"protected void Raise{context.Data.Name}Event({context.Data.EventArgs} args)",
                "{",
                $"    args.RoutedEvent = {context.Data.Name}Event;",
                "    RaiseEvent(args);",
                "}"
            )
        );
    }
}
