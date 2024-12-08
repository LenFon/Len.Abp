using System.Reflection;
using System.Windows;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Len.Abp.Wpf;

public class DefaultWpfService(IAbpApplication app) : IWpfService
{
    private readonly IAbpApplication _app = app;

    public void Initialize(Application application)
    {
        var startupModuleType = _app.StartupModuleType;
        var wpfassemblies = AbpModuleHelper
            .FindDependedModuleTypes(startupModuleType)
            .Select(s => s.Assembly)
            .Where(w => w.IsWpfAssembly() && w.FullName?.Contains("Len.Abp") == false)
            .ToList();

        if (startupModuleType.Assembly.IsWpfAssembly())
        {
            wpfassemblies.Insert(0, startupModuleType.Assembly);
        }

        foreach (var assembly in wpfassemblies)
        {
            if (assembly.ExistsResource("default.baml"))
            {
                var uri = new Uri($"pack://application:,,,/{assembly.GetName().Name};component/default.xaml");
                application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });
            }
        }
    }
}
