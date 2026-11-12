using Len.Abp.Wpf;
using Len.Abp.Wpf.Navigation;
using WpfApp.ViewModels;

namespace WpfApp;

/// <summary>
/// 嵌套区域的回收自检：不经过 UI 自动化，由应用自己导航并统计实例存活情况。
/// </summary>
/// <remarks>
/// 设置环境变量 <c>LEN_ABP_NESTED_SELFTEST=1</c> 后启动应用即可触发。
/// 之所以不用 UI 自动化驱动，是因为自动化客户端会持有元素的引用，
/// 导致弱引用统计失真，无法判断框架是否真的释放了实例。
/// </remarks>
public static class NestedRegionDiagnostics
{
    /// <summary>
    /// 环境变量名，设置后启动即运行自检。
    /// </summary>
    public const string EnvironmentVariable = "LEN_ABP_NESTED_SELFTEST";

    /// <summary>
    /// 判断是否需要运行自检。
    /// </summary>
    public static bool IsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable(EnvironmentVariable), "1", StringComparison.Ordinal);

    /// <summary>
    /// 反复执行「进入八层 → 切到其他视图 → 切回」，并在每个阶段统计实例存活情况。
    /// </summary>
    /// <param name="navigationService">导航服务。</param>
    /// <param name="log">日志输出。</param>
    public static async Task RunAsync(INavigationService navigationService, Action<string> log)
    {
        const int rounds = 3;

        for (var round = 1; round <= rounds; round++)
        {
            await EnterEightLevelsAsync(navigationService, log, round);
            log($"自检：第 {round} 轮八层就位 " + Describe());

            await navigationService.NavigateAsync("MainRegion", "gallery", IParameters.Create());
            await Task.Delay(600);
            log($"自检：第 {round} 轮切到画廊 " + Describe());

            // 切回嵌套视图，重新走完八层，验证上一轮的实例是否已经释放。
            await EnterEightLevelsAsync(navigationService, log, round);
            log($"自检：第 {round} 轮切回并重建八层 " + Describe());
        }

        await navigationService.NavigateAsync("MainRegion", "gallery", IParameters.Create());
        await Task.Delay(600);
        log("自检：最终切到画廊 " + Describe());
    }

    private static async Task EnterEightLevelsAsync(INavigationService navigationService, Action<string> log, int round)
    {
        var result = await navigationService.NavigateAsync("MainRegion", "nested-root", IParameters.Create());
        if (!result.Result)
        {
            log($"自检：第 {round} 轮进入第 1 层失败 {result.Exception?.Message}");
            return;
        }

        // 子区域在视图 Loaded 时才创建，必须等它加载完再往下导航。
        await Task.Delay(400);

        for (var depth = 1; depth < 8; depth++)
        {
            result = await navigationService.NavigateAsync(
                $"NestedLevel{depth}Region",
                $"nested-level{depth}",
                IParameters.Create()
            );

            if (!result.Result)
            {
                log($"自检：第 {round} 轮进入第 {depth + 1} 层失败 {result.Exception?.Message}");
                return;
            }

            await Task.Delay(400);
        }
    }

    private static string Describe()
    {
        var (viewsCreated, viewsAlive) = NestedInstanceTracker.CollectViews();
        var (viewModelsCreated, viewModelsAlive) = NestedInstanceTracker.CollectViewModels();

        return $"视图 {viewsAlive}/{viewsCreated} 存活，视图模型 {viewModelsAlive}/{viewModelsCreated} 存活";
    }
}
