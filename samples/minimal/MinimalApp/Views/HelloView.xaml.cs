using Len.Abp.Wpf.Mvvm;
using MinimalApp.ViewModels;
using System.Windows.Controls;

namespace MinimalApp.Views;

/// <summary>
/// Hello 视图。
/// </summary>
[View<HelloViewModel>(ViewName = "hello")]
public partial class HelloView : UserControl
{
    public HelloView()
    {
        InitializeComponent();
    }
}
