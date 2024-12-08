using System.Windows;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf;

public interface IWpfService: ISingletonDependency
{
    /// <summary>
    /// Do whatever you need to do to initialize WPF, this is called from the UI thread
    /// </summary>
    /// <param name="application">application</param>
    void Initialize(Application application);
}
