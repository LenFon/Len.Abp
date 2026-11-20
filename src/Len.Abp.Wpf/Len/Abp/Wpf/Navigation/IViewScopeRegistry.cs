using Len.Abp.Wpf.Mvvm;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// 视图与导航作用域的关联表，负责在视图被拆除时释放其作用域。
/// </summary>
/// <remarks>
/// <para>
/// 每次导航创建一个 <see cref="IServiceScope"/>，视图由该作用域解析，
/// 因此视图与作用域必须成对释放：作用域会跟踪它创建的 <see cref="IDisposable"/> 实例，
/// 单独释放视图而不释放作用域会留下作用域内的其他实例。
/// </para>
/// <para>
/// 声明为公开类型是因为 <see cref="Regions.Behaviors.RegionMemberLifetimeBehavior"/>
/// 是公开的扩展点，其构造函数不能接收 internal 类型。使用者通常不需要直接使用它。
/// </para>
/// </remarks>
public interface IViewScopeRegistry
{
    /// <summary>
    /// 关联视图与其作用域。
    /// </summary>
    /// <param name="view">由该作用域解析的视图。</param>
    /// <param name="scope">视图所属的导航作用域。</param>
    void Attach(IView view, IServiceScope scope);

    /// <summary>
    /// 取出并移除视图关联的作用域。
    /// </summary>
    /// <param name="view">要查询的视图。</param>
    /// <param name="scope">关联的作用域；不存在时为 <see langword="null"/>。</param>
    /// <returns>存在关联作用域时返回 <see langword="true"/>。</returns>
    bool TryTake(IView view, out IServiceScope? scope);
}

/// <summary>
/// <see cref="IViewScopeRegistry"/> 的默认实现。
/// </summary>
/// <remarks>
/// 用 <see cref="ConditionalWeakTable{TKey, TValue}"/> 而不是字典：键是视图，
/// 弱引用保证异常路径下未被显式取出的条目不会永久阻止视图被回收。
/// 正常路径总是通过 <see cref="TryTake"/> 显式移除，不依赖 GC。
/// </remarks>
internal sealed class ViewScopeRegistry : IViewScopeRegistry, ISingletonDependency
{
    private readonly ConditionalWeakTable<IView, IServiceScope> _scopes = [];

    /// <inheritdoc/>
    public void Attach(IView view, IServiceScope scope)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(scope);

        // 同一视图只会被一次导航解析，重复关联说明调用方有缺陷，直接覆盖即可。
        _scopes.AddOrUpdate(view, scope);
    }

    /// <inheritdoc/>
    public bool TryTake(IView view, out IServiceScope? scope)
    {
        ArgumentNullException.ThrowIfNull(view);

        if (_scopes.TryGetValue(view, out var found))
        {
            _scopes.Remove(view);
            scope = found;
            return true;
        }

        scope = null;
        return false;
    }
}
