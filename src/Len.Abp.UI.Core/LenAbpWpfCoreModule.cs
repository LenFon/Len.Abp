using Len.Abp.Core;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace Len.Abp.Wpf.Core;

[DependsOn(typeof(LenAbpCoreModule), typeof(AbpAutofacModule))]
public class LenAbpWpfCoreModule : AbpModule { }
