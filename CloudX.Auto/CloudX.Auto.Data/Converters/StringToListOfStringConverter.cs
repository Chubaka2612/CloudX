using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using JsonException = Newtonsoft.Json.JsonException;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace CloudX.Auto.AWS.Core.Converters
{
    public class StringToListOfStringConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(bool);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {

            if (reader.TokenType == JsonToken.String)
            {
                return new List<string> { reader.Value?.ToString() };
            }
            else if (reader.TokenType == JsonToken.StartArray)
            {
                return new JsonSerializer().Deserialize<List<string>>(reader);
            }

            throw new JsonException("Invalid JSON format for field.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
        }
    }
}