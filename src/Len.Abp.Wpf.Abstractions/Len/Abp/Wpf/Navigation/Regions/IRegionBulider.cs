using System.Windows;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

public interface IRegionBulider : ITransientDependency
{
    void Bulid(DependencyObject element);
}