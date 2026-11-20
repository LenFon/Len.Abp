using Microsoft.Extensions.DependencyInjection;

namespace Len.Abp.Wpf.Mvvm;

/// <summary>
/// 标记视图类，并为 <c>MvvmGenerator</c> 提供视图模型、视图名称等信息。
/// </summary>
/// <param name="viewModel">视图模型类型。</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ViewAttribute(Type viewModel) : Attribute
{
    /// <summary>
    /// 获取或设置该视图是否为 Shell 主窗口。
    /// </summary>
    public bool IsShell { get; set; } = false;

    /// <summary>
    /// 获取或设置视图模型类型。
    /// </summary>
    public Type ViewModel { get; set; } = viewModel;

    /// <summary>
    /// 获取或设置视图名称，导航时用于定位视图。
    /// </summary>
    public string? ViewName { get; set; }

    /// <summary>
    /// 获取或设置视图在容器中的注册生命周期。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 默认值 <see cref="ServiceLifetime.Transient"/>；Shell 视图固定为单例，该设置会被忽略。
    /// </para>
    /// <para>
    /// 每次导航都会创建一个独立作用域，视图由该作用域解析，因此
    /// <see cref="ServiceLifetime.Scoped"/> 视图每次导航都得到新实例，
    /// 它依赖的 Scoped 服务也来自同一个导航作用域，视图被拆除时作用域随之释放。
    /// 需要跨导航复用请用 <see cref="ServiceLifetime.Singleton"/>，
    /// 需要每次新建请用 <see cref="ServiceLifetime.Transient"/>。
    /// </para>
    /// </remarks>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;

    /// <summary>
    /// 获取或设置导航到本视图时必须提供的参数。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 每项支持两种写法：<c>"Id"</c> 只校验参数存在；<c>"Id:int"</c> 同时校验存在性与类型。
    /// 冒号两侧允许空白，两种写法可在同一数组中混用。
    /// </para>
    /// <para>
    /// 这里声明的是**输入契约**，不是调用约束：调用方可以多传参数（多余的被忽略），
    /// 但缺少必需参数时导航会在激活视图之前失败。
    /// </para>
    /// <para>
    /// 生成器会把它翻译成**视图模型**上的
    /// <see cref="Navigation.INavigationContract.RequiredNavigationParameters"/> 只读字典。
    /// 契约放在视图模型上，因为视图模型才是参数的消费者；字典内容放在静态字段里
    /// 由所有实例共享，通过实例属性转发给接口成员。
    /// </para>
    /// <para>
    /// 视图模型必须声明为 <c>partial</c>，否则生成器无法追加成员。
    /// 声明了必需参数却无法生成时报告 <c>LENABP009</c>（错误级）——
    /// 契约缺失会让参数校验静默失效，这比"不能自动注册"严重。
    /// 仅需要自动注册时是 <c>LENABP006</c>（警告级）。
    /// </para>
    /// </remarks>
    public string[]? RequiredParameters { get; set; }

    /// <summary>
    /// 获取或设置视图模型的注册生命周期。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 默认值 <see cref="ServiceLifetime.Transient"/>，但**只有显式赋值时才覆盖视图的 <see cref="Lifetime"/>**。
    /// 未赋值时视图模型跟随视图的生命周期。
    /// </para>
    /// <para>
    /// 不能用 <c>ServiceLifetime?</c>：特性命名参数只接受常量类型，
    /// 可空枚举不是合法类型，编译器会把它当成错误常量。
    /// 因此这里用 <see cref="HasViewModelLifetime"/> 标记是否显式赋值。
    /// </para>
    /// </remarks>
    public ServiceLifetime ViewModelLifetime { get; set; } = ServiceLifetime.Transient;

    /// <summary>
    /// 获取或设置是否显式指定了 <see cref="ViewModelLifetime"/>。
    /// </summary>
    /// <remarks>
    /// 为 <see langword="false"/> 时视图模型跟随视图的 <see cref="Lifetime"/>。
    /// </remarks>
    public bool HasViewModelLifetime { get; set; }
}

/// <summary>
/// 标记视图类，并指定视图模型类型。
/// </summary>
/// <typeparam name="T">视图模型类型。</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ViewAttribute<T>() : ViewAttribute(typeof(T));
