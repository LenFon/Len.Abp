using System.Windows;
using System.Windows.Data;

namespace Len.Abp.Wpf;

/// <summary>
/// 用于生成附加依赖属性的特性。
/// </summary>
/// <param name="name">附加依赖属性的名称。</param>
/// <param name="type">附加依赖属性的类型。</param>
/// <exception cref="ArgumentNullException">名称为空或类型为空时抛出。</exception>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AttachedDependencyPropertyAttribute(string name, Type type) : Attribute
{
    /// <summary>
    /// 获取或设置附加依赖属性的分类。
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// 获取或设置附加依赖属性的默认值。
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// 获取或设置属性的默认值表达式。<br/>
    /// 当默认值不是常量时使用，例如 "new()"。
    /// </summary>
    public string? DefaultValueExpression { get; set; }

    /// <summary>
    /// 获取或设置附加依赖属性的描述。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 获取或设置附加依赖属性的元数据标志。
    /// </summary>
    public FrameworkPropertyMetadataOptions Flags { get; set; }

    /// <summary>
    /// 注册为只读附加依赖属性。
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// 附加依赖属性的名称。
    /// </summary>
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    /// <summary>
    /// 附加依赖属性的类型。
    /// </summary>
    public Type Type { get; } = type ?? throw new ArgumentNullException(nameof(type));

    /// <summary>
    /// 获取或设置附加依赖属性的更新源触发器。
    /// </summary>
    public UpdateSourceTrigger UpdateSourceTrigger { get; set; } = UpdateSourceTrigger.Default;
}

/// <summary>
/// 用于生成附加依赖属性的特性。
/// </summary>
/// <typeparam name="T">附加依赖属性的类型。</typeparam>
/// <param name="name">附加依赖属性的名称。</param>
/// <exception cref="ArgumentNullException">名称为空或类型为空时抛出。</exception>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AttachedDependencyPropertyAttribute<T>(string name) : AttachedDependencyPropertyAttribute(name, typeof(T))
{
    /// <summary>
    /// 获取或设置依赖属性的默认值。
    /// </summary>
    public new T? DefaultValue { get; set; }
}
