using Volo.Abp;

namespace Len.Abp.Core.Extensions;

public static class EnumerableExtensions
{
    extension<T>(IEnumerable<T> source)
    {
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