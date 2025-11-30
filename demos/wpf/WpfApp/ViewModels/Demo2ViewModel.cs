using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Len.Abp.Wpf;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using System.Windows.Controls;
using System.Windows.Media;
using Volo.Abp.DependencyInjection;

namespace WpfApp.ViewModels;

public partial class Demo2ViewModel : ObservableObject, ITransientDependency, INavigationAware, IRegionMemberLifetime
{
    [ObservableProperty]
    private string _message = "Hello, World! Demo2.";

    public bool KeepAlive => false;

    public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext)
    {
        return Task.FromResult(true);
    }

    public Task OnNavigatedFromAsync(INavigationContext navigationContext)
    {
        return Task.CompletedTask;
    }

    public Task OnNavigatedToAsync(INavigationContext navigationContext)
    {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Click()
    {
        Message = "Demo2 Button Clicked!";
    }
}

[DependencyProperty<double>("Age", DefaultValue = 10D, Description = "年龄", Coerce = true, Validate = true, Browsable = false, Bindable = true)]
[DependencyProperty<Color>("Color", DefaultValueExpression = "System.Windows.Media.Colors.Red", Description = "颜色", IsReadOnly = true)]
public sealed partial class TestControl : Control
{
    private static partial double CoerceAge(double? value)
    {
        throw new NotImplementedException();
    }

    private static partial bool IsAgeValid(double? value)
    {
        throw new NotImplementedException();
    }
}