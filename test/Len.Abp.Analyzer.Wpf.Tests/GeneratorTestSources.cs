namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 生成器测试共用的桩类型与源码拼装方法。
/// </summary>
/// <remarks>
/// 桩类型只保留生成器读取特性与类型信息所需的最小成员，
/// 目的是让 <c>ForAttributeWithMetadataName</c> 能解析到特性，而不是模拟 WPF。
/// </remarks>
internal static class GeneratorTestSources
{
    /// <summary>
    /// 生成器依赖的全部桩类型。
    /// </summary>
    public const string Stubs = """
        namespace System.Windows
        {
            public class DependencyObject
            {
                public object GetValue(DependencyProperty dp) => null!;
                public void SetValue(DependencyProperty dp, object value) { }
            }

            public class DependencyProperty { }

            public class PropertyMetadata
            {
                public PropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback, CoerceValueCallback coerceValueCallback) { }
            }

            public class FrameworkPropertyMetadata : PropertyMetadata
            {
                public FrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions flags, PropertyChangedCallback propertyChangedCallback, CoerceValueCallback coerceValueCallback, bool isAnimationProhibited, System.Windows.Data.UpdateSourceTrigger defaultUpdateSourceTrigger)
                    : base(defaultValue, propertyChangedCallback, coerceValueCallback) { }
            }

            [System.Flags]
            public enum FrameworkPropertyMetadataOptions { None = 0, AffectsRender = 1, BindsTwoWayByDefault = 2 }

            public delegate void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e);
            public delegate object CoerceValueCallback(DependencyObject d, object baseValue);
            public delegate void ValidateValueCallback(object value);
            public struct DependencyPropertyChangedEventArgs { public object OldValue; public object NewValue; }

            public static class DependencyProperty
            {
                public static DependencyProperty Register(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata typeMetadata, ValidateValueCallback validateValueCallback) => new();
                public static DependencyPropertyKey RegisterReadOnly(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata typeMetadata, ValidateValueCallback validateValueCallback) => new();
            }

            public class DependencyPropertyKey { public DependencyProperty DependencyProperty => new(); }

            public class UIElement : DependencyObject
            {
                public void AddHandler(RoutedEvent routedEvent, System.Delegate handler) { }
                public void RemoveHandler(RoutedEvent routedEvent, System.Delegate handler) { }
                public void RaiseEvent(RoutedEventArgs e) { }
            }

            public class RoutedEvent { }
            public class RoutedEventArgs { public RoutedEvent? RoutedEvent { get; set; } }
            public enum RoutingStrategy { Tunnel = 0, Bubble = 1, Direct = 2 }
            public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);

            public static class EventManager
            {
                public static RoutedEvent RegisterRoutedEvent(string name, RoutingStrategy routingStrategy, System.Type handlerType, System.Type ownerType) => new();
            }
        }

        namespace System.Windows.Data
        {
            public enum UpdateSourceTrigger { Default = 0, PropertyChanged = 1 }
        }

        namespace Microsoft.Extensions.DependencyInjection
        {
            public enum ServiceLifetime { Singleton = 0, Scoped = 1, Transient = 2 }
        }

        namespace Len.Abp.Wpf
        {
            public interface IShell { }
            public class FrameworkElement { public object? DataContext { get; set; } }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class DependencyPropertyAttribute : System.Attribute
            {
                public DependencyPropertyAttribute(string name, System.Type type) { }
                public string? Category { get; set; }
                public object? DefaultValue { get; set; }
                public string? DefaultValueExpression { get; set; }
                public string? Description { get; set; }
                public System.Windows.FrameworkPropertyMetadataOptions Flags { get; set; }
                public bool IsReadOnly { get; set; }
                public System.Windows.Data.UpdateSourceTrigger UpdateSourceTrigger { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class DependencyPropertyAttribute<T> : DependencyPropertyAttribute
            {
                public DependencyPropertyAttribute(string name) : base(name, typeof(T)) { }
                public new T? DefaultValue { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class AttachedDependencyPropertyAttribute : System.Attribute
            {
                public AttachedDependencyPropertyAttribute(string name, System.Type type) { }
                public string? Category { get; set; }
                public object? DefaultValue { get; set; }
                public string? DefaultValueExpression { get; set; }
                public string? Description { get; set; }
                public System.Windows.FrameworkPropertyMetadataOptions Flags { get; set; }
                public bool IsReadOnly { get; set; }
                public System.Windows.Data.UpdateSourceTrigger UpdateSourceTrigger { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class AttachedDependencyPropertyAttribute<T> : AttachedDependencyPropertyAttribute
            {
                public AttachedDependencyPropertyAttribute(string name) : base(name, typeof(T)) { }
                public new T? DefaultValue { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class RoutedEventAttribute : System.Attribute
            {
                public RoutedEventAttribute(string name, System.Windows.RoutingStrategy strategy, System.Type? handlerType = null) { }
                public string? Category { get; set; }
                public string? Description { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class RoutedEventAttribute<T> : RoutedEventAttribute
            {
                public RoutedEventAttribute(string name, System.Windows.RoutingStrategy strategy) : base(name, strategy, typeof(T)) { }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class AttachedRoutedEventAttribute : System.Attribute
            {
                public AttachedRoutedEventAttribute(string name, System.Windows.RoutingStrategy strategy, System.Type? handlerType = null) { }
                public string? Category { get; set; }
                public string? Description { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class AttachedRoutedEventAttribute<T> : AttachedRoutedEventAttribute
            {
                public AttachedRoutedEventAttribute(string name, System.Windows.RoutingStrategy strategy) : base(name, strategy, typeof(T)) { }
            }
        }

        namespace Len.Abp.Wpf.Mvvm
        {
            public interface IView { object ViewModel { get; } string ViewName { get; } }
            public interface IView<out TViewModel> : IView where TViewModel : class { new TViewModel ViewModel { get; } }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
            public class ViewAttribute : System.Attribute
            {
                public ViewAttribute(System.Type viewModel) { }
                public bool IsShell { get; set; }
                public System.Type ViewModel { get; set; } = null!;
                public string? ViewName { get; set; }
                public Microsoft.Extensions.DependencyInjection.ServiceLifetime Lifetime { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
            public class ViewAttribute<T> : ViewAttribute { public ViewAttribute() : base(typeof(T)) { } }
        }

        namespace Volo.Abp.DependencyInjection
        {
            public interface ITransientDependency { }
            public interface ISingletonDependency { }
            public interface IScopedDependency { }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class ExposeServicesAttribute : System.Attribute
            {
                public ExposeServicesAttribute(params System.Type[] serviceTypes) { }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class ExposeKeyedServiceAttribute<T> : System.Attribute
            {
                public ExposeKeyedServiceAttribute(string key) { }
            }
        }
        """;

    /// <summary>
    /// 把桩类型与待测源码拼成完整输入。
    /// </summary>
    /// <param name="snippet">待测类型声明。</param>
    /// <returns>可直接交给生成器的源码。</returns>
    public static string With(string snippet) => Stubs + "\n\n" + snippet.TrimEnd() + "\n";
}
