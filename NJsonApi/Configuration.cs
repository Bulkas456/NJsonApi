using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonApi.Serialization;
using NJsonApi.Serialization.Documents;
using System.Linq;
using NJsonApi.Serialization.Converters;
using System.Threading.Tasks;
using NJsonApi.Formatter.Input;

namespace NJsonApi
{
    public class Configuration
    {
        private readonly List<string> supportedContentTypes = new List<string>() { "application/vnd.api+json", "application/json" };
        private readonly Dictionary<string, IResourceMapping> resourcesMappingsByResourceType = new Dictionary<string, IResourceMapping>();
        private readonly Dictionary<Type, IResourceMapping> resourcesMappingsByType = new Dictionary<Type, IResourceMapping>();
        private readonly Lazy<JsonSerializer> serializer;
        private readonly IJsonApiTransformer jsonApiTransformer = new JsonApiTransformer();
        private readonly Dictionary<Type, IJsonApiInputMapper> inputMappers = new Dictionary<Type, IJsonApiInputMapper>();
        private readonly List<Func<PreSerializationContext, Task>> preSerializationActions = new List<Func<PreSerializationContext, Task>>(); 

        public Configuration()
        {
            this.serializer = new Lazy<JsonSerializer>(GetJsonSerializer);
        }

        public void AddMapping(IResourceMapping resourceMapping)
        {
            resourcesMappingsByResourceType[resourceMapping.ResourceType] = resourceMapping;
            resourcesMappingsByType[resourceMapping.ResourceRepresentationType] = resourceMapping;
        }

        public void AddInputMapper(IDictionary<Type, IJsonApiInputMapper> inputMappers)
        {
            foreach (KeyValuePair<Type, IJsonApiInputMapper> pair in inputMappers)
            {
                this.inputMappers[pair.Key] = pair.Value;
            }
        }

        public void AddPreSerializationAction(IEnumerable<Func<PreSerializationContext, Task>> actions)
        {
            this.preSerializationActions.AddRange(actions);
        }

        public JsonSerializer Serializer => this.serializer.Value;

        public IJsonApiTransformer JsonApiTransformer => this.jsonApiTransformer;

        public IReadOnlyList<string> SupportedContentTypes => this.supportedContentTypes;

        public bool SupportInputConversionFromJsonApi => this.inputMappers.Count > 0;

        public bool IsTypeSupportedForJsonApiInput(Type type)
        {
            if (typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType)
            {
                return this.inputMappers.ContainsKey(type.GetGenericArguments()[0]);
            }

            return this.inputMappers.ContainsKey(type);
        }

        public IJsonApiInputMapper GetInputMapper(Type type)
        {
            if (!this.inputMappers.TryGetValue(type, out IJsonApiInputMapper mapper))
            {
                throw new InvalidOperationException($"No input mapper for typ '{type.FullName}'");
            }

            return mapper;
        }

        public bool IsTypeSupportedForJsonApiOutput(Type type)
        {
            return this.IsMappingRegistered(type)
                   || type.IsAssignableFrom(typeof(CompoundDocument));
        }

        public bool IsMappingRegistered(Type type)
        {
            if (typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType)
            {
                return resourcesMappingsByType.ContainsKey(type.GetGenericArguments()[0]);
            }

            return resourcesMappingsByType.ContainsKey(type);
        }

        public IResourceMapping GetMapping(Type type)
        {
            IResourceMapping mapping;
            resourcesMappingsByType.TryGetValue(type, out mapping);
            return mapping;
        }

        public bool SupportContentType(string mimeType)
        {
            return this.supportedContentTypes.Any(contentType => string.Equals(mimeType, contentType, StringComparison.OrdinalIgnoreCase));
        }

        public async Task BeforeSerialization(PreSerializationContext context)
        {
            foreach (Func<PreSerializationContext, Task> action in this.preSerializationActions)
            {
                await action(context);
            }
        }

        private JsonSerializer GetJsonSerializer()
        {
            var serializerSettings = new JsonSerializerSettings();
            serializerSettings.Converters.Add(new IsoDateTimeConverter());
            serializerSettings.Converters.Add(new StringEnumConverter() { CamelCaseText = true });
#if DEBUG
            serializerSettings.Formatting = Formatting.Indented;
#endif
            var jsonSerializer = JsonSerializer.Create(serializerSettings);
            jsonSerializer.Converters.Add(new RelationshipConverter());
            jsonSerializer.Converters.Add(new ResourceConverter());
            jsonSerializer.Converters.Add(new LinkConverter());
            jsonSerializer.Converters.Add(new ResourceLinkageConverter());
            return jsonSerializer;
        }
    }
}
