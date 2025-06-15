using System.Windows;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions.Behaviors;

public interface IDelayedRegionCreationBehavior : ITransientDependency
{
    DependencyObject? TargetElement { get; set; }

    void Attach();

    void Detach();
}