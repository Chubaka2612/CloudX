using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CloudX.Auto.Core.Extensions
{
    public static class CollectionExtension
    {
        public static async Task ForEachAsync<T>(this IList<T> list, Func<T, Task> func)
        {
            foreach (var value in list)
            {
                await func(value);
            }
        }

        public static string ToJoinString<T>(this IList<T> list)
        {
            return string.Join(", ", list);
        }
    }
}
