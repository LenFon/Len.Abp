namespace Len.Abp.Analyzer;

public readonly record struct ClassData(
    string Namespace,
    string Name,
    string FullName,
    string Type,
    string Modifiers,
    bool IsStatic,
    IReadOnlyList<string> Methods);