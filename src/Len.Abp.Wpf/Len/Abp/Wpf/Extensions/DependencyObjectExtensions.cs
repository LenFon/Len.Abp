using System.Windows;
using System.Windows.Data;

namespace Len.Abp.Wpf.Extensions;

internal static class DependencyObjectExtensions
{
    public static bool HasBinding(this DependencyObject instance, DependencyProperty property) =>
        BindingOperations.GetBinding(instance, property) != null;
}