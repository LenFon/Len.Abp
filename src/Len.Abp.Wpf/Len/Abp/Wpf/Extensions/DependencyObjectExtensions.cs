using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Len.Abp.Wpf.Extensions;

/// <summary>
/// <see cref="DependencyObject"/> 的扩展方法。
/// </summary>
internal static class DependencyObjectExtensions
{
    extension(DependencyObject dependencyObject)
    {
        /// <summary>
        /// 判断指定依赖属性是否设置了绑定。
        /// </summary>
        /// <param name="property">要检查的依赖属性。</param>
        /// <returns>存在绑定返回 true，否则返回 false。</returns>
        /// <remarks>
        /// 使用 <see cref="BindingOperations.GetBindingBase"/> 而非 <see cref="BindingOperations.GetBinding"/>，
        /// 后者对 <see cref="MultiBinding"/>、<see cref="PriorityBinding"/> 返回 null，会漏判。
        /// </remarks>
        public bool HasBinding(DependencyProperty property) =>
            BindingOperations.GetBindingBase(dependencyObject, property) != null;

        /// <summary>
        /// 沿逻辑树或视觉树向上查找指定类型的父级元素。
        /// </summary>
        /// <typeparam name="T">要查找的父级类型。</typeparam>
        /// <returns>找到的父级元素；不存在时返回 null。</returns>
        public T? GetParent<T>() where T : class
        {
            var parent = LogicalTreeHelper.GetParent(dependencyObject)
                ?? (dependencyObject is Visual or System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(dependencyObject)
                    : null);

            return parent switch
            {
                null => null,
                T tParent => tParent,
                _ => parent.GetParent<T>(),
            };
        }
    }
}
