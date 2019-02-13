using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using NJsonApi.Serialization.Representations;
using NJsonApi.Serialization.Representations.Resources;

namespace NJsonApi.Serialization.Converters
{
    public class ResourceConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                return serializer.Deserialize<SingleResource>(reader);
            }

            if (reader.TokenType == JsonToken.StartArray)
            {
                return serializer.Deserialize<ResourceCollection>(reader);
            }

            throw new JsonSerializationException("Unsupported structure for IResourceRepresentation");
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IResourceRepresentation);
        }
    }
}
