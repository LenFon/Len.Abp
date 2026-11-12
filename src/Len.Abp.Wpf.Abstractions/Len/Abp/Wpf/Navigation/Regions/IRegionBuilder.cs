using System.Windows;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

public interface IRegionBuilder : ITransientDependency
{
    void Build(DependencyObject element);
}