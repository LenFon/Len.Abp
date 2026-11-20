using Len.Abp.Wpf.Mvvm;
using MinimalApp.ViewModels;
using System.Windows.Controls;

namespace MinimalApp.Views;

/// <summary>
/// Goodbye 视图。
/// </summary>
[View<GoodbyeViewModel>(ViewName = "goodbye")]
public partial class GoodbyeView : UserControl
{
    public GoodbyeView()
    {
        InitializeComponent();
    }
}
