using System.Windows;
using System.Windows.Data;

namespace Len.Abp.Wpf;

/// <summary>
/// Attribute to generate a dependency property.
/// </summary>
/// <param name="name">The name of the dependency property</param>
/// <param name="type">The type of the dependency property</param>
/// <exception cref="ArgumentNullException">The name or type is empty</exception>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DependencyPropertyAttribute(string name, Type type) : Attribute
{
    /// <summary>
    /// Gets or sets the category of the dependency property.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the default value of the dependency property.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the default value expression for the property.<br/>
    /// Use this if the default value is not a constant, e.g. "new()".
    /// </summary>
    public string? DefaultValueExpression { get; set; }

    /// <summary>
    ///  Gets or sets the description of the dependency property.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the flags of the dependency property.
    /// </summary>
    public FrameworkPropertyMetadataOptions Flags { get; set; }

    /// <summary>
    /// Register a read-only dependency property.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// The name of the dependency property.
    /// </summary>
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    /// <summary>
    /// The type of the dependency property.
    /// </summary>
    public Type Type { get; } = type ?? throw new ArgumentNullException(nameof(type));

    /// <summary>
    /// Gets or sets the update source trigger of the dependency property.
    /// </summary>
    public UpdateSourceTrigger UpdateSourceTrigger { get; set; } = UpdateSourceTrigger.Default;
}

/// <summary>
/// Attribute to generate a dependency property.
/// </summary>
/// <typeparam name="T">The type of the dependency property</typeparam>
/// <param name="name">The name of the dependency property</param>
/// <exception cref="ArgumentNullException">The name or type is empty</exception>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DependencyPropertyAttribute<T>(string name) : DependencyPropertyAttribute(name, typeof(T))
{
    /// <summary>
    /// Gets or sets the default value of the dependency property.
    /// </summary>
    public new T? DefaultValue { get; set; }
}