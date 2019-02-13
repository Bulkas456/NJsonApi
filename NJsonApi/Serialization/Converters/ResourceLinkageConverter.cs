using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using NJsonApi.Serialization.Representations;
using NJsonApi.Serialization.Representations.Relationships;

namespace NJsonApi.Serialization.Converters
{
    public class ResourceLinkageConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                return serializer.Deserialize<SingleResourceIdentifier>(reader);
            }

            if (reader.TokenType == JsonToken.StartArray)
            {
                return serializer.Deserialize<MultipleResourceIdentifiers>(reader);
            }

            if (reader.TokenType == JsonToken.Null)
            {
                return new NullResourceIdentifier();
            }

            throw new JsonSerializationException("JSON is not correct structure for IResourceLinkage");
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IResourceLinkage);
        }
    }
}
