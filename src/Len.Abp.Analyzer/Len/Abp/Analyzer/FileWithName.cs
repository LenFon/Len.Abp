namespace Len.Abp.Analyzer;

/// <summary>
/// 表示一个带名称及其对应文本内容的文件。
/// </summary>
/// <param name="Name">文件名。</param>
/// <param name="Text">文件内容。</param>
public readonly record struct FileWithName(string Name, string Text)
{
    /// <summary>
    /// 获取一个空的 <see cref="FileWithName"/> 实例，其中 <see cref="Name"/> 和 <see cref="Text"/> 均为空字符串。
    /// </summary>
    public static FileWithName Empty => new(string.Empty, string.Empty);

    /// <summary>
    /// 指示该 <see cref="FileWithName"/> 实例是否为空，即 <see cref="Name"/> 或 <see cref="Text"/> 为 null 或空字符串。
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Text);
}