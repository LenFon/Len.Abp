using Len.Abp.Wpf.Mvvm;
using System.Windows.Controls;
using WpfApp.ViewModels;

namespace WpfApp.Views;

/// <summary>
/// 登录视图，演示视图模型的自动注册。
/// </summary>
[View<LoginViewModel>(ViewName = "login")]
public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }
}