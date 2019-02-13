using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using NJsonApi.Serialization.Representations;
using NJsonApi.Serialization.Representations.Relationships;

namespace NJsonApi.Serialization.Converters
{
    public class RelationshipConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize<Relationship>(reader);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IRelationship);
        }
    }
}
