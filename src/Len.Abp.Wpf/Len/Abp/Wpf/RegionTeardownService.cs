using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf;

/// <summary>
/// 应用退出时拆除所有区域并释放其中的视图。
/// </summary>
/// <remarks>
/// 区域与视图由单例 <see cref="IRegionProvider"/> 持有。进程退出时它们随进程消失，
/// 但在测试或嵌入式宿主中应用可能被反复启动，不清理会留下旧区域，
/// 视图模型持有的连接与订阅也不会被释放。
/// 容器管理的实例不在这里释放，与区域行为的保活语义保持一致。
/// </remarks>
internal sealed class RegionTeardownService(IAbpLazyServiceProvider abpLazyServiceProvider)
    : ISingletonDependency
{
    private readonly IAbpLazyServiceProvider _abpLazyServiceProvider = abpLazyServiceProvider;

    /// <summary>
    /// 拆除所有区域。
    /// </summary>
    public void TearDownAll()
    {
        var regionProvider = _abpLazyServiceProvider.LazyGetService<IRegionProvider>();

        if (regionProvider is null)
        {
            return;
        }

        // 退出时容器可能已开始销毁，视图的 Dispose 里解析服务会抛异常。
        // 逐个区域兜底，一个失败不影响其余区域的拆除。
        foreach (var region in regionProvider.Regions.ToArray())
        {
            try
            {
                // RemoveAll 会走区域行为的统一拆除路径，释放不保活的视图及其嵌套子树。
                region.RemoveAll();
            }
            catch (Exception exception)
            {
                _abpLazyServiceProvider
                    .LazyGetService<ILogger<RegionTeardownService>>()
                    ?.LogWarning(exception, "拆除区域 {regionName} 时发生异常。", region.Name);
            }
        }

        regionProvider.Clear();
    }
}
