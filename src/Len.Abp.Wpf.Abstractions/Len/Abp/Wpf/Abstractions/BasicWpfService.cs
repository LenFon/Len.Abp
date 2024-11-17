using System.IO;
using System.Reflection;
using System.Windows;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Len.Abp.Wpf.Abstractions;

public class BasicWpfService : IWpfService
{
    private readonly IAbpApplication _app;

    public BasicWpfService(IAbpApplication app)
    {
        _app = app;
    }

    public void Initialize(Application application)
    {
        var startupModuleType = _app.StartupModuleType;
        var wpfassemblyNames = AbpModuleHelper
            .FindDependedModuleTypes(startupModuleType)
            .Where(w => IsWpfAssembly(w.Assembly))
            .Select(s => s.Assembly.GetName().Name)
            .ToList();

        wpfassemblyNames.Insert(0, startupModuleType.Assembly.GetName().Name);

        foreach (var wpfassemblyName in wpfassemblyNames)
        {
            try
            {
                application.Resources.MergedDictionaries.Add(
                    new ResourceDictionary
                    {
                        Source = new Uri($"pack://application:,,,/{wpfassemblyName};component/default.xaml")
                    }
                );
            }
            catch (IOException)
            {
                continue;
            }
            catch
            {
                throw;
            }

            break;
        }
    }

    private static bool IsWpfAssembly(Assembly assembly)
    {
        // WPF 核心程序集
        string[] wpfAssemblies = ["PresentationFramework", "WindowsBase", "PresentationCore"];

        // 获取该 DLL 引用的所有程序集
        var referencedAssemblies = assembly.GetReferencedAssemblies();

        // 检查是否引用了 WPF 相关程序集
        return referencedAssemblies.Any(r => wpfAssemblies.Contains(r.Name));
    }
}
