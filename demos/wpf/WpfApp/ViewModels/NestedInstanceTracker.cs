namespace WpfApp.ViewModels;

/// <summary>
/// 记录嵌套示例创建的视图与视图模型实例，用于验证离开区域后是否被回收。
/// </summary>
/// <remarks>
/// 只保存弱引用，不会阻止实例被 GC 回收；统计前主动触发一次完整回收。
/// 这是演示用的诊断工具，生产代码不需要。
/// </remarks>
public static class NestedInstanceTracker
{
    private static readonly object Gate = new();
    private static readonly List<WeakReference> Views = [];
    private static readonly List<WeakReference> ViewModels = [];

    /// <summary>
    /// 记录一个视图实例。
    /// </summary>
    /// <param name="view">视图实例。</param>
    public static void TrackView(object view)
    {
        lock (Gate)
        {
            Views.Add(new WeakReference(view));
        }
    }

    /// <summary>
    /// 记录一个视图模型实例。
    /// </summary>
    /// <param name="viewModel">视图模型实例。</param>
    public static void TrackViewModel(object viewModel)
    {
        lock (Gate)
        {
            ViewModels.Add(new WeakReference(viewModel));
        }
    }

    /// <summary>
    /// 触发完整回收并统计视图的存活情况。
    /// </summary>
    /// <returns>创建总数与仍然存活的实例数。</returns>
    public static (int Created, int Alive) CollectViews() => Collect(Views);

    /// <summary>
    /// 触发完整回收并统计视图模型的存活情况。
    /// </summary>
    /// <returns>创建总数与仍然存活的实例数。</returns>
    public static (int Created, int Alive) CollectViewModels() => Collect(ViewModels);

    private static (int Created, int Alive) Collect(List<WeakReference> references)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        lock (Gate)
        {
            return (references.Count, references.Count(reference => reference.IsAlive));
        }
    }
}
