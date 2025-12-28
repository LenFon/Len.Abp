using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Len.Abp.Wpf.Extensions;

internal static class DependencyObjectExtensions
{
    extension(DependencyObject dependencyObject)
    {
        public bool HasBinding(DependencyProperty property) => BindingOperations.GetBinding(dependencyObject, property) != null;

        public T? GetParent<T>() where T : class
        {
            var parent = LogicalTreeHelper.GetParent(dependencyObject) ?? VisualTreeHelper.GetParent(dependencyObject);

            return parent switch
            {
                null => null,
                T tParent => tParent,
                _ => parent.GetParent<T>(),
            };
        }
    }
}