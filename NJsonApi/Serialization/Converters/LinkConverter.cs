using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using NJsonApi.Serialization.Representations;
using NJsonApi.Serialization.Representations.Resources;

namespace NJsonApi.Serialization.Converters
{
    public class LinkConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                return serializer.Deserialize<Link>(reader);
            }

            if (reader.TokenType == JsonToken.String)
            {
                return new Serialization.Representations.SimpleLink
                {
                    Href = (string)reader.Value
                };
            }

            throw new JsonSerializationException("Unsupported structure for ILink");
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ILink);
        }
    }
}
