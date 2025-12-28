using Len.Abp.Wpf.Mvvm;
using System.Windows.Controls;
using WpfApp.ViewModels;

namespace WpfApp.Views;

/// <summary>
/// LoginView.xaml 的交互逻辑
/// </summary>
[View<LoginViewModel>(ViewName = "login")]
public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }
}