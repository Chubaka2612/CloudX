using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CloudX.Auto.Core.Utils
{
    public static class CommonUtils
    {

        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            if (sequence == null)
                return;
            foreach (var obj in sequence)
                action(obj);
        }

        public static string WrapToJson(object value)
        {
            return JsonConvert.SerializeObject(value, Formatting.Indented);
        }

        public static T PopulateFromJson<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value, new JsonSerializerSettings { DateParseHandling = DateParseHandling.None });
        }
    }
}