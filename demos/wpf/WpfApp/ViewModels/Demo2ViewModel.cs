using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Len.Abp.Wpf;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using System.Windows;
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

[DependencyProperty<double>("Age", DefaultValue = 10D, Description = "年龄", Flags = FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)]
[DependencyProperty<Color>("Color", DefaultValueExpression = "System.Windows.Media.Colors.Red", Description = "颜色", IsReadOnly = true)]
public partial class TestControl : Control
{
    static partial void CoerceAge(double value, ref double result)
    {
        throw new NotImplementedException();
    }

    static partial void CoerceColor(Color value, ref Color result)
    {
        throw new NotImplementedException();
    }

    static partial void IsAgeValid(double value, ref bool result)
    {
        throw new NotImplementedException();
    }

    partial void OnAgeChanged(double oldValue, double newValue)
    {
        throw new NotImplementedException();
    }

    partial void OnColorChanged(Color oldValue, Color newValue)
    {
        throw new NotImplementedException();
    }
}

[AttachedDependencyProperty<double>(
    "Age",
    DefaultValue = 10D,
    Description = "年龄",
    IsReadOnly = true,
    Flags = FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged
)]
public partial class TestClass
{
    static partial void CoerceAge(DependencyObject element, double value, ref double result)
    {
        throw new NotImplementedException();
    }

    static partial void IsAgeValid(double value, ref bool result)
    {
        throw new NotImplementedException();
    }

    static partial void OnAgeChanged(DependencyObject element, double oldValue, double newValue)
    {
        throw new NotImplementedException();
    }
}