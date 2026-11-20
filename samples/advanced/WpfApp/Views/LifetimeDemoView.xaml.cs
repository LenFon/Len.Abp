using System.Windows.Controls;

namespace WpfApp.Views;

/// <summary>
/// 生命周期演示视图的公共外观，四个派生视图共用同一份 XAML。
/// </summary>
/// <remarks>
/// 派生视图只负责声明 <c>[View&lt;T&gt;]</c> 与生命周期，界面内容由本类提供，
/// 避免为每种生命周期组合复制一份相同的 XAML。
/// </remarks>
public partial class LifetimeDemoView : UserControl
{
    public LifetimeDemoView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 设置标题与生命周期说明。
    /// </summary>
    /// <param name="title">视图标题。</param>
    /// <param name="lifetime">生命周期说明文本。</param>
    protected void Configure(string title, string lifetime)
    {
        TitleText.Text = title;
        LifetimeText.Text = lifetime;
    }
}
