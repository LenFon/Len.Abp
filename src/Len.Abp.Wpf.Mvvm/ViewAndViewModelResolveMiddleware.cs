using System.Reflection;
using System.Windows;
using Autofac;
using Autofac.Core.Resolving.Pipeline;

namespace Len.Abp.Wpf.Mvvm;

internal class ViewAndViewModelResolveMiddleware : IResolveMiddleware
{
    public PipelinePhase Phase => PipelinePhase.RegistrationPipelineStart;

    public void Execute(ResolveRequestContext context, Action<ResolveRequestContext> next)
    {
        next(context);

        if (context.Instance is not FrameworkElement frameworkElement)
            return;

        if (context.Instance is IView)
        {
            frameworkElement.DataContext = frameworkElement;

            return;
        }

        var @interface = context.Instance.GetType().GetInterface(typeof(IView<>).Name);
        if (@interface is null)
            return;

        var vm = context.Resolve(@interface.GetGenericArguments()[0]);

        frameworkElement.DataContext = vm;

        var property = @interface.GetProperty(
            nameof(IView<object>.ViewModel),
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        property?.SetValue(context.Instance, vm);
    }
}
