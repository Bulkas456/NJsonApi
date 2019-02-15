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
using Newtonsoft.Json.Serialization;

namespace NJsonApi
{
    public class Configuration : IConfiguration
    {
        private readonly HashSet<string> supportedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/vnd.api+json"
        };
        private readonly Dictionary<string, IResourceMapping> resourcesMappingsByResourceType = new Dictionary<string, IResourceMapping>();
        private readonly Dictionary<Type, IResourceMapping> resourcesMappingsByType = new Dictionary<Type, IResourceMapping>();
        private readonly Lazy<JsonSerializer> serializer;
        private readonly IJsonApiTransformer jsonApiTransformer = new JsonApiTransformer();
        private readonly Dictionary<Type, IJsonApiInputMapper> inputMappers = new Dictionary<Type, IJsonApiInputMapper>();
        private readonly List<Func<PreSerializationContext, Task>> preSerializationActions = new List<Func<PreSerializationContext, Task>>();
        private readonly Lazy<IReadOnlyList<string>> supportedContentTypesList;

        public Configuration()
        {
            this.serializer = new Lazy<JsonSerializer>(GetJsonSerializer);
            this.supportedContentTypesList = new Lazy<IReadOnlyList<string>>(() => this.supportedContentTypes.ToList());
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

                if (pair.Value.SupportedContentTypes != null)
                {
                    foreach (string contentType in pair.Value.SupportedContentTypes)
                    {
                        this.supportedContentTypes.Add(contentType);
                    }
                }
            }
        }

        public void AddPreSerializationAction(IEnumerable<Func<PreSerializationContext, Task>> actions)
        {
            this.preSerializationActions.AddRange(actions);
        }

        public JsonSerializer Serializer => this.serializer.Value;

        public IJsonApiTransformer JsonApiTransformer => this.jsonApiTransformer;

        public IReadOnlyList<string> SupportedContentTypes => this.supportedContentTypesList.Value;

        public bool SupportInputConversionFromJsonApi => this.inputMappers.Count > 0;

        public Func<JsonSerializer> JsonSerializerFactory
        {
            get;
            set;
        }

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
            JsonSerializer serializer = this.JsonSerializerFactory == null
                                          ? JsonSerializer.Create()
                                          : this.JsonSerializerFactory();
#if DEBUG
            serializer.Formatting = Formatting.Indented;
#endif
            serializer.Converters.Add(new IsoDateTimeConverter());
            serializer.Converters.Add(new StringEnumConverter() { NamingStrategy = new CamelCaseNamingStrategy() });
            serializer.Converters.Add(new RelationshipConverter());
            serializer.Converters.Add(new ResourceConverter());
            serializer.Converters.Add(new LinkConverter());
            serializer.Converters.Add(new ResourceLinkageConverter());
            return serializer;
        }
    }
}
