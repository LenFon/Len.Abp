using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Len.Abp.Wpf;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using System.Collections.ObjectModel;
using System.Windows;
using Volo.Abp.DependencyInjection;
using WpfApp.Regions;

namespace WpfApp.ViewModels;

/// <summary>
/// 区域适配器与导航能力演示的视图模型。
/// </summary>
/// <remarks>
/// 覆盖六类能力：<c>TabControl</c> 区域、<c>ItemsControl</c> 区域、自定义 <c>ListBox</c> 区域、
/// 导航历史回退、必需参数校验与取消、自定义全局异常处理。
/// 操作日志同时接收自定义区域行为 <see cref="RegionActivityBehavior"/> 的记录。
/// </remarks>
/// <param name="navigationService">导航服务。</param>
/// <param name="activityLog">区域活动日志，由自定义区域行为写入。</param>
/// <param name="exceptionLog">全局异常日志，由 <see cref="WpfAppExceptionHandler"/> 写入。</param>
public partial class RegionAdaptersViewModel(
    INavigationService navigationService,
    RegionActivityLog activityLog,
    ExceptionLog exceptionLog
) : ObservableObject, ITransientDependency, IRegionMemberLifetime
{
    /// <inheritdoc/>
    public bool KeepAlive => true;

    /// <summary>
    /// 获取区域活动日志，最新的在前。
    /// </summary>
    public ObservableCollection<string> ActivityLog => activityLog.Entries;

    /// <summary>
    /// 获取全局异常日志，最新的在前。
    /// </summary>
    public ObservableCollection<string> ExceptionLog => exceptionLog.Entries;

    /// <summary>
    /// 获取或设置最近一次操作的结果文本。
    /// </summary>
    [ObservableProperty]
    public partial string Status { get; set; } = "就绪";

    /// <summary>
    /// 在选项卡区域中导航到指定页面。
    /// </summary>
    /// <param name="viewName">页面视图名。</param>
    [RelayCommand]
    private async Task ShowTabAsync(string viewName)
    {
        var result = await navigationService.NavigateAsync("TabRegion", viewName, IParameters.Create());

        Status = result.Result ? $"选项卡区域已导航到 {viewName}" : $"选项卡导航失败：{Describe(result)}";
        Log($"TabRegion → {viewName}（{(result.Result ? "成功" : "失败")}）");
    }

    /// <summary>
    /// 在列表区域中追加一个页面。
    /// </summary>
    /// <param name="viewName">页面视图名。</param>
    [RelayCommand]
    private async Task AddListItemAsync(string viewName)
    {
        var result = await navigationService.NavigateAsync("ListRegion", viewName, IParameters.Create());

        Status = result.Result ? $"列表区域已追加 {viewName}" : $"列表导航失败：{Describe(result)}";
        Log($"ListRegion → {viewName}（{(result.Result ? "成功" : "失败")}）");
    }

    /// <summary>
    /// 在自定义 ListBox 区域中导航到指定页面。
    /// </summary>
    /// <param name="viewName">页面视图名。</param>
    [RelayCommand]
    private async Task ShowListBoxItemAsync(string viewName)
    {
        var result = await navigationService.NavigateAsync("ListBoxRegion", viewName, IParameters.Create());

        Status = result.Result ? $"ListBox 区域已导航到 {viewName}" : $"ListBox 导航失败：{Describe(result)}";
    }

    /// <summary>
    /// 提示清空列表区域的正确做法：多活动区域不会被导航自动失活。
    /// </summary>
    /// <remarks>
    /// 这里刻意不调用 <c>RemoveAll</c>：清空后列表项全部消失，演示就没法继续追加了。
    /// 真正回收视图请用 <c>IRegion.Remove</c> 或 <c>IRegion.RemoveAll</c>。
    /// </remarks>
    [RelayCommand]
    private void ClearList()
    {
        Status = "多活动区域不会被导航自动失活，需要显式调用 IRegion.RemoveAll 才会清空";
    }

    /// <summary>
    /// 在选项卡区域中回退到上一个页面。
    /// </summary>
    [RelayCommand]
    private async Task GoBackTabAsync()
    {
        var result = await navigationService.GoBackAsync("TabRegion");

        Status = result.Result
            ? $"选项卡区域已回退到 {result.Context.Uri.OriginalString}"
            : "选项卡区域没有更早的历史";

        Log($"TabRegion 回退（{(result.Result ? "成功" : "无历史")}）");
    }

    /// <summary>
    /// 带上必需参数导航到详情页，演示参数校验通过。
    /// </summary>
    [RelayCommand]
    private async Task OpenDetailWithParametersAsync()
    {
        var result = await navigationService.NavigateAsync(
            "TabRegion",
            "detail",
            IParameters.Create("?id=42&source=region-demo")
        );

        Status = result.Result ? "参数校验通过，已导航到 detail" : $"参数校验失败：{Describe(result)}";
        Log($"detail 带参数（{(result.Result ? "成功" : "失败")}）");
    }

    /// <summary>
    /// 不带必需参数导航到详情页，演示参数校验失败。
    /// </summary>
    [RelayCommand]
    private async Task OpenDetailWithoutParametersAsync()
    {
        var result = await navigationService.NavigateAsync("TabRegion", "detail", IParameters.Create());

        Status = result.Result ? "导航成功（不应出现）" : $"参数校验失败：{Describe(result)}";
        Log($"detail 缺参数（{(result.Result ? "意外成功" : "按预期失败")}）");
    }

    /// <summary>
    /// 发起一次立即取消的导航，演示取消不会被包装成失败结果。
    /// </summary>
    [RelayCommand]
    private async Task NavigateWithCancelAsync()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        try
        {
            await navigationService.NavigateAsync("TabRegion", "tab-page-1", IParameters.Create(), cts.Token);
            Status = "导航未被取消（不应出现）";
            Log("取消测试：导航未被取消");
        }
        catch (OperationCanceledException)
        {
            Status = "导航已取消：OperationCanceledException 穿透，区域状态未改变";
            Log("取消测试：OperationCanceledException 已穿透");
        }
    }

    /// <summary>
    /// 清空操作日志。
    /// </summary>
    [RelayCommand]
    private void ClearActivityLog()
    {
        activityLog.Clear();
        Status = "操作日志已清空";
    }

    /// <summary>
    /// 在 UI 线程上抛出一个未处理异常，演示全局异常处理。
    /// </summary>
    /// <remarks>
    /// 用 <see cref="Application.Current"/> 的调度器异步投递，而不是直接抛出：
    /// 直接抛会被命令执行链捕获，走不到 <c>DispatcherUnhandledException</c>。
    /// </remarks>
    [RelayCommand]
    private void ThrowUiException()
    {
        Status = "已在 UI 线程抛出异常，请查看弹窗与异常记录";

        Application.Current.Dispatcher.BeginInvoke(
            new Action(() => throw new InvalidOperationException("这是示例抛出的未处理异常。"))
        );
    }

    /// <summary>
    /// 清空异常记录。
    /// </summary>
    [RelayCommand]
    private void ClearExceptionLog()
    {
        exceptionLog.Entries.Clear();
        Status = "异常记录已清空";
    }

    private void Log(string message) => activityLog.Record(message);

    private static string Describe(INavigationResult result)
    {
        if (result.Exception is null)
        {
            return "目标区域不存在";
        }

        var messages = new List<string>();

        for (var current = result.Exception; current is not null; current = current.InnerException)
        {
            messages.Add(current.Message);
        }

        return string.Join(" → ", messages);
    }
}
