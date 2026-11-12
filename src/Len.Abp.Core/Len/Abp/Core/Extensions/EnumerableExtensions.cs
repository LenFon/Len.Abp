using Volo.Abp;

namespace Len.Abp.Core.Extensions;

/// <summary>
/// 集合处理扩展方法。
/// </summary>
public static class EnumerableExtensions
{
    extension<T>(IEnumerable<T> source)
    {
        /// <summary>
        /// 对集合中的每个元素执行指定操作。
        /// </summary>
        /// <param name="action">要执行的操作。</param>
        public void ForEach(Action<T> action)
        {
            Check.NotNull(action, nameof(action));

            foreach (var item in source)
            {
                action(item);
            }
        }
    }
}
