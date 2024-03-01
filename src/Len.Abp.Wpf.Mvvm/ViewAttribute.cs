using Microsoft.Extensions.DependencyInjection;

namespace Len.Abp.Wpf.Mvvm;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ViewAttribute : Attribute
{
    public Type? ViewModel { get; set; }

    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;
}
