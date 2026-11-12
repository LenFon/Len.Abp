using Len.Abp.Core.Extensions;
using Xunit;

namespace Len.Abp.Core.Tests;

/// <summary>
/// 覆盖 <see cref="EnumerableExtensions.ForEach"/>。
/// </summary>
public class EnumerableExtensionsTests
{
    /// <summary>
    /// ForEach 对每个元素执行一次操作。
    /// </summary>
    [Fact]
    public void ForEach_InvokesActionForEveryItem()
    {
        var visited = new List<int>();

        new[] { 1, 2, 3 }.ForEach(visited.Add);

        Assert.Equal([1, 2, 3], visited);
    }

    /// <summary>
    /// 空集合不执行任何操作。
    /// </summary>
    [Fact]
    public void ForEach_EmptySource_DoesNothing()
    {
        var count = 0;

        Array.Empty<int>().ForEach(_ => count++);

        Assert.Equal(0, count);
    }

    /// <summary>
    /// 传入 null 操作时抛出异常。
    /// </summary>
    [Fact]
    public void ForEach_NullAction_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => SingleItemArray.ForEach(null!));
    }

    private static readonly int[] SingleItemArray = [1];
}
