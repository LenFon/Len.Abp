using Volo.Abp;

namespace Len.Abp.Core.Extensions;

public static class EnumerableExtensions
{
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        Check.NotNull(source, nameof(source));
        Check.NotNull(action, nameof(action));

        foreach (var item in source)
        {
            action(item);
        }
    }
}