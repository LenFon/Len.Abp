using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 涉及 <see cref="Len.Abp.Core.AbpServiceProvider"/> 的测试会修改进程级全局状态，
/// 因此这些测试之间不能并行执行。
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GlobalServiceProviderCollection
{
    /// <summary>
    /// 集合名称。
    /// </summary>
    public const string Name = "GlobalServiceProvider";
}
