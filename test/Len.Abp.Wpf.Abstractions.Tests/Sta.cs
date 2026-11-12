namespace Len.Abp.Wpf.Abstractions.Tests;

/// <summary>
/// 在独立的 STA 线程上执行测试代码，WPF 控件与 <see cref="System.Windows.Application"/> 需要 STA。
/// </summary>
internal static class Sta
{
    /// <summary>
    /// 在 STA 线程上执行 <paramref name="action"/>，并把其中的异常回抛到调用线程。
    /// </summary>
    public static void Run(Action action)
    {
        Exception? captured = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            throw captured;
        }
    }

    /// <summary>
    /// 在 STA 线程上执行 <paramref name="func"/> 并返回结果。
    /// </summary>
    public static T Run<T>(Func<T> func)
    {
        T result = default!;
        Run(() => result = func());
        return result;
    }
}
