using Len.Abp.Wpf.Mvvm;
using MinimalApp.ViewModels;
using System.Windows.Controls;

namespace MinimalApp.Views;

/// <summary>
/// 问候视图，演示导航必需参数的声明与校验。
/// </summary>
/// <remarks>
/// <c>RequiredParameters = ["name"]</c> 只校验参数存在性；
/// 写成 <c>["name:string"]</c> 会同时校验类型。
/// 校验发生在激活之前，失败时界面保持原状。
/// </remarks>
[View<GreetingViewModel>(ViewName = "greeting", RequiredParameters = ["name"])]
public partial class GreetingView : UserControl
{
    public GreetingView()
    {
        InitializeComponent();
    }
}
