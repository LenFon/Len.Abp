using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation.Regions;
using System.Collections.Specialized;
using Volo.Abp.DependencyInjection;

namespace WpfApp.Regions;

/// <summary>
/// 区域活动日志，记录所有区域的视图激活与失活，供界面展示。
/// </summary>
/// <remarks>
/// 单例服务，由 <see cref="RegionActivityBehavior"/> 写入。
/// 直接暴露 <see cref="System.Collections.ObjectModel.ObservableCollection{T}"/>，
/// 界面绑定后无需额外的变更通知。区域活动都在 UI 线程上发生，因此不需要加锁。
/// 这是演示用的诊断设施，生产代码通常改用 <c>ILogger</c>。
/// </remarks>
public sealed class RegionActivityLog : ISingletonDependency
{
    private const int MaxEntries = 40;

    /// <summary>
    /// 获取日志条目，最新的在前。
    /// </summary>
    public System.Collections.ObjectModel.ObservableCollection<string> Entries { get; } = [];

    /// <summary>
    /// 追加一条日志。
    /// </summary>
    /// <param name="message">日志内容。</param>
    public void Record(string message)
    {
        Entries.Insert(0, $"{DateTime.Now:HH:mm:ss.fff}  {message}");

        while (Entries.Count > MaxEntries)
        {
            Entries.RemoveAt(Entries.Count - 1);
        }
    }

    /// <summary>
    /// 清空日志。
    /// </summary>
    public void Clear() => Entries.Clear();
}

/// <summary>
/// 自定义区域行为：把每次视图激活与失活记录到 <see cref="RegionActivityLog"/>。
/// </summary>
/// <remarks>
/// <para>
/// 以键控方式注册 <see cref="IRegionBehavior"/> 后，<c>LenAbpWpfModule</c> 会在
/// <c>PostConfigureServices</c> 扫描到它并写入 <see cref="RegionOptions.BehaviorKeys"/>，
/// 之后每个新建区域都会自动附加本行为，无需在 XAML 或代码里逐个声明。
/// </para>
/// <para>
/// 这是演示「如何扩展区域语义」的最小例子：行为只需实现 <see cref="IRegionBehavior"/>，
/// 在 <see cref="Attach"/> 里订阅区域集合的变更即可。
/// </para>
/// </remarks>
[ExposeKeyedService<IRegionBehavior>(nameof(RegionActivityBehavior))]
public class RegionActivityBehavior(RegionActivityLog log) : IRegionBehavior, ITransientDependency
{
    private readonly RegionActivityLog _log = log;

    /// <inheritdoc/>
    public IRegion Region { get; set; } = default!;

    /// <inheritdoc/>
    public void Attach()
    {
        _log.Record($"区域 {Region.Name} 已创建（所有者 {Region.Owner}）");

        Region.ActiveViews.CollectionChanged += OnActiveViewsChanged;
    }

    private void OnActiveViewsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is { Count: > 0 })
        {
            foreach (var view in e.NewItems.Cast<IView>())
            {
                _log.Record($"区域 {Region.Name} 激活 {view.ViewName}");
            }
        }

        if (e.OldItems is { Count: > 0 })
        {
            foreach (var view in e.OldItems.Cast<IView>())
            {
                _log.Record($"区域 {Region.Name} 失活 {view.ViewName}");
            }
        }
    }
}
