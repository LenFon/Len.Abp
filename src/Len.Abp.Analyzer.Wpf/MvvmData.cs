using Microsoft.CodeAnalysis;

namespace Len.Abp.Analyzer;

/// <summary>
/// 视图生成器所需的 MVVM 数据。
/// </summary>
/// <param name="IsShell">是否为 Shell 视图。</param>
/// <param name="ViewModel">视图模型的完全限定类型名。</param>
/// <param name="ViewName">视图名称。</param>
/// <param name="Lifetime">视图的注册生命周期。</param>
/// <param name="HasViewModelConstructor">视图是否已手写接收视图模型的构造函数。</param>
internal readonly record struct MvvmData(
    bool IsShell,
    string ViewModel,
    string? ViewName,
    ServiceLifetimeKind Lifetime = ServiceLifetimeKind.Transient,
    bool HasViewModelConstructor = false)
{
    /// <summary>
    /// 获取导航必需参数；类型为 null 表示只校验存在性。
    /// </summary>
    /// <remarks>
    /// 由 <c>MvvmGenerator.PrepareData</c> 在解析特性后填充，因此不放进主构造函数：
    /// 解析需要语义模型，而隐式转换只拿到 <see cref="AttributeData"/>。
    /// </remarks>
    public IReadOnlyList<RequiredParameter> RequiredParameters { get; init; } = [];

    /// <summary>
    /// 获取视图模型的自动注册信息；为 <see langword="null"/> 表示不生成注册。
    /// </summary>
    public ViewModelRegistration? ViewModelRegistration { get; init; }

    /// <summary>
    /// 获取视图模型契约的生成位置；为 <see langword="null"/> 表示不生成契约成员。
    /// </summary>
    /// <remarks>
    /// 与 <see cref="ViewModelRegistration"/> 分开：自动注册要求视图模型是
    /// <c>partial</c> 且构造函数可注入，而契约成员只要求视图模型是 <c>partial</c>。
    /// 两者条件不同，不能合并。
    /// </remarks>
    public ViewModelContract? ViewModelContract { get; init; }

    /// <summary>
    /// 从视图特性数据解析 MVVM 数据。
    /// </summary>
    /// <param name="attribute">视图特性数据。</param>
    /// <returns>解析后的 MVVM 数据。</returns>
    public static MvvmData Parse(AttributeData attribute)
    {
        if (attribute is null)
        {
            throw new ArgumentNullException(nameof(attribute));
        }

        var isShellConstant = attribute.GetNamedArgument("IsShell");
        var isShell = !isShellConstant.IsNull && isShellConstant.To<bool>();

        var typeSymbol = attribute.GetTypeArgument(0, 0);
        var viewModel = typeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? string.Empty;
        var viewName = attribute.GetNamedArgument("ViewName").Value?.ToString() ?? string.Empty;

        return new MvvmData(isShell, viewModel, viewName, GetLifetime(attribute));
    }

    /// <summary>
    /// 读取生命周期命名参数。
    /// </summary>
    /// <remarks>
    /// 未设置时回退到瞬态；取值越界同样回退。
    /// </remarks>
    private static ServiceLifetimeKind GetLifetime(AttributeData attribute) =>
        attribute.GetNamedArgument("Lifetime").ToServiceLifetimeKind();
}

/// <summary>
/// 导航必需参数。
/// </summary>
/// <param name="Name">参数名。</param>
/// <param name="Type">参数类型；为 null 表示只校验存在性。</param>
/// <param name="DeclaredType">
/// 使用者声明的类型名原文；未声明类型时为 <see langword="null"/>。
/// </param>
/// <remarks>
/// <see cref="Type"/> 为 null 有两种来源：使用者没写类型（合法，只校验存在性），
/// 或写了但解析不出来（多半是笔误）。<see cref="DeclaredType"/> 用来区分两者，
/// 后者要报告诊断而不是静默降级。
/// </remarks>
internal readonly record struct RequiredParameter(string Name, string? Type, string? DeclaredType = null);

/// <summary>
/// 视图模型的自动注册信息。
/// </summary>
/// <param name="Class">视图模型的类型信息，含命名空间、外层类型链与类型种类。</param>
/// <param name="Lifetime">注册生命周期。</param>
/// <remarks>
/// 位置信息用 <see cref="Analyzer.ClassData"/> 而不是"简单名 + 命名空间"：
/// 后者会丢掉外层类型链（嵌套视图模型被声明到幽灵类型上）与类型种类
/// （record 视图模型被生成成 <c>partial class</c>）。
/// </remarks>
internal readonly record struct ViewModelRegistration(ClassData Class, ServiceLifetimeKind Lifetime);

/// <summary>
/// 视图模型契约成员的生成位置。
/// </summary>
/// <param name="Class">视图模型的类型信息，含命名空间、外层类型链与类型种类。</param>
/// <remarks>
/// 契约成员（<c>RequiredNavigationParameters</c>）生成到视图模型上，
/// 因为视图模型才是参数的消费者。与 <see cref="ViewModelRegistration"/> 分开：
/// 契约只要求视图模型是 <c>partial</c>，不要求构造函数可注入。
/// </remarks>
internal readonly record struct ViewModelContract(ClassData Class);

/// <summary>
/// 视图生命周期的生成器内部表示。
/// </summary>
/// <remarks>
/// 源生成器不能引用目标程序集，因此这里镜像
/// <c>Microsoft.Extensions.DependencyInjection.ServiceLifetime</c> 的取值，两处定义必须保持一致。
/// </remarks>
internal enum ServiceLifetimeKind
{
    /// <summary>
    /// 整个应用共享同一实例。
    /// </summary>
    Singleton = 0,

    /// <summary>
    /// 每个作用域内共享同一实例。
    /// </summary>
    Scoped = 1,

    /// <summary>
    /// 每次解析都创建新实例。
    /// </summary>
    Transient = 2,
}

/// <summary>
/// <see cref="ServiceLifetimeKind"/> 与特性常量之间的转换。
/// </summary>
internal static class ServiceLifetimeKindExtensions
{
    /// <summary>
    /// 获取生命周期对应的 ABP 标记接口完全限定名。
    /// </summary>
    /// <param name="lifetime">生命周期。</param>
    /// <returns>标记接口的完全限定名。</returns>
    public static string ToDependencyInterface(this ServiceLifetimeKind lifetime) => lifetime switch
    {
        ServiceLifetimeKind.Scoped => "global::Volo.Abp.DependencyInjection.IScopedDependency",
        ServiceLifetimeKind.Singleton => "global::Volo.Abp.DependencyInjection.ISingletonDependency",
        _ => "global::Volo.Abp.DependencyInjection.ITransientDependency",
    };

    /// <summary>
    /// 把特性中的生命周期常量转换为内部表示。
    /// </summary>
    /// <param name="constant">特性命名参数值。</param>
    /// <returns>生命周期；未设置或取值越界时返回瞬态。</returns>
    /// <remarks>
    /// 枚举常量在特性数据里以底层整数值存放。这里镜像
    /// <c>Microsoft.Extensions.DependencyInjection.ServiceLifetime</c> 的取值：
    /// <c>Singleton = 0</c>、<c>Scoped = 1</c>、<c>Transient = 2</c>。
    /// </remarks>
    public static ServiceLifetimeKind ToServiceLifetimeKind(this TypedConstant constant)
    {
        if (constant.IsNull)
        {
            return ServiceLifetimeKind.Transient;
        }

        return constant.To<int>() switch
        {
            0 => ServiceLifetimeKind.Singleton,
            1 => ServiceLifetimeKind.Scoped,
            _ => ServiceLifetimeKind.Transient,
        };
    }
}
