using Microsoft.Extensions.DependencyInjection;

namespace Len.Abp.Wpf.Mvvm;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ViewAttribute(Type viewModel) : Attribute
{
    public Type ViewModel { get; set; } = viewModel;

    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;
}
