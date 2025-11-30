namespace Len.Abp.Wpf;

/// <summary>
/// Attribute to generate a DependencyProperty
/// </summary>
/// <param name="name">The name of the dependency property</param>
/// <param name="type">The type of the dependency property</param>
/// <exception cref="ArgumentNullException">The dependency property name or dependency property type is empty</exception>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DependencyPropertyAttribute(string name, Type type) : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether the property is bindable.
    /// </summary>
    public bool Bindable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the property is browsable in a property browsing control.
    /// </summary>
    public bool Browsable { get; set; }

    /// <summary>
    /// Gets or sets the category of the dependency property.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Whether to generate the partial method for CoerceValueCallback.
    /// </summary>
    public bool Coerce { get; set; }

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
    /// Whether to generate the partial method for validateValueCallback.
    /// </summary>
    public bool Validate { get; set; }
}

/// <summary>
/// Attribute to generate a DependencyProperty
/// </summary>
/// <typeparam name="T">The type of the dependency property</typeparam>
/// <param name="name">The name of the dependency property</param>
/// <exception cref="ArgumentNullException">The dependency property name or dependency property type is empty</exception>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DependencyPropertyAttribute<T>(string name) : DependencyPropertyAttribute(name, typeof(T))
{
    /// <summary>
    /// Gets or sets the default value of the dependency property.
    /// </summary>
    public new T? DefaultValue { get; set; }
}