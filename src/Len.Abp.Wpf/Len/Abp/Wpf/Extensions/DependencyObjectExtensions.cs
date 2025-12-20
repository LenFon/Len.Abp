using System.Windows;
using System.Windows.Data;

namespace Len.Abp.Wpf.Extensions;

internal static class DependencyObjectExtensions
{
    extension(DependencyObject dependencyObject)
    {
        public bool HasBinding(DependencyProperty property) =>
            BindingOperations.GetBinding(dependencyObject, property) != null;
    }
}