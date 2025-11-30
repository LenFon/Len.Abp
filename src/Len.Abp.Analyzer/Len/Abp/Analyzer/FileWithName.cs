namespace Len.Abp.Analyzer;

/// <summary>
/// Represents a file with a name and its corresponding text content.
/// </summary>
/// <param name="Name"></param>
/// <param name="Text"></param>
public readonly record struct FileWithName(string Name, string Text)
{
    /// <summary>
    /// Gets an empty <see cref="FileWithName"/> instance, where both <see cref="Name"/> and <see cref="Text"/> are empty strings.
    /// </summary>
    public static FileWithName Empty => new(string.Empty, string.Empty);

    /// <summary>
    /// Indicates whether the <see cref="FileWithName"/> instance is empty, meaning either the <see cref="Name"/> or <see cref="Text"/> is null or an empty string.
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Text);
}