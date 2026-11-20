using Len.Abp.Wpf.Mvvm;
using System.Windows.Controls;
using WpfApp.ViewModels;

namespace WpfApp.Views;

/// <summary>
/// 详情页，演示导航必需参数的声明与校验。
/// </summary>
/// <remarks>
/// <c>RequiredParameters</c> 支持两种写法：<c>"id"</c> 只校验存在性，
/// <c>"id:int"</c> 同时校验类型。生成器把它翻译成视图模型上的
/// <see cref="Len.Abp.Wpf.Navigation.INavigationContract.RequiredNavigationParameters"/>，
/// 导航服务在激活视图之前校验。
/// </remarks>
[View<DetailViewModel>(ViewName = "detail", RequiredParameters = ["id:int", "source"])]
public partial class DetailView : UserControl
{
    public DetailView()
    {
        InitializeComponent();
    }
}
