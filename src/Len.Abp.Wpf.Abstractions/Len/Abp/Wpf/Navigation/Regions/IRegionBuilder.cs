using System.Windows;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 区域构建器，在控件加载后为其创建区域。
/// </summary>
public interface IRegionBuilder : ITransientDependency
{
    /// <summary>
    /// 为指定元素构建区域。元素加载完成时才真正创建区域。
    /// </summary>
    /// <param name="element">声明了区域名称的宿主元素。</param>
    void Build(DependencyObject element);
}
