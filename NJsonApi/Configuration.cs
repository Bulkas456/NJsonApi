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
using Microsoft.AspNetCore.Mvc.Formatters;
using NJsonApi.Formatter.Output;

namespace NJsonApi
{
    public class Configuration : IConfiguration
    {
        private readonly HashSet<string> supportedInputContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Constants.JsonApiContentType
        };
        private readonly Lazy<IReadOnlyList<string>> supportedInputContentTypesList;
        private readonly Dictionary<string, IResourceMapping> resourcesMappingsByResourceType = new Dictionary<string, IResourceMapping>();
        private readonly Dictionary<Type, IResourceMapping> resourcesMappingsByType = new Dictionary<Type, IResourceMapping>();
        private readonly Lazy<JsonSerializer> serializer;
        private readonly IJsonApiTransformer jsonApiTransformer = new JsonApiTransformer();
        private readonly Dictionary<Type, IJsonApiInputMapper> inputMappers = new Dictionary<Type, IJsonApiInputMapper>();
        private readonly List<Action<PreSerializationContext>> preSerializationActions = new List<Action<PreSerializationContext>>();
        private readonly List<Action<OverrideResponseHeadersContext>> overrideResponseHeadersActions = new List<Action<OverrideResponseHeadersContext>>();
        private readonly HashSet<string> supportedOutputContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Constants.JsonApiContentType
        };
        private readonly Lazy<IReadOnlyList<string>> supportedOutputContentTypesList;

        public Configuration()
        {
            this.serializer = new Lazy<JsonSerializer>(GetJsonSerializer);
            this.supportedInputContentTypesList = new Lazy<IReadOnlyList<string>>(() => this.supportedInputContentTypes.ToList());
            this.supportedOutputContentTypesList = new Lazy<IReadOnlyList<string>>(() => this.supportedOutputContentTypes.ToList());
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

        public void AddPreSerializationAction(IEnumerable<Action<PreSerializationContext>> actions)
        {
            this.preSerializationActions.AddRange(actions);
        }

        public void AddOverrideResponseHeadersAction(IEnumerable<Action<OverrideResponseHeadersContext>> actions)
        {
            this.overrideResponseHeadersActions.AddRange(actions);
        }

        public JsonSerializer Serializer => this.serializer.Value;

        public IJsonApiTransformer JsonApiTransformer => this.jsonApiTransformer;

        public IReadOnlyList<string> SupportedInputContentTypes => this.supportedInputContentTypesList.Value;

        public IReadOnlyList<string> SupportedOutputContentTypes => this.supportedOutputContentTypesList.Value;

        public bool SupportInputConversionFromJsonApi => this.inputMappers.Count > 0;

        public bool CreateResponseInJsonApiForUnhandedExceptions { get; set; } = true;
        public Func<JsonSerializer> JsonSerializerFactory
        {
            get;
            set;
        }

        public void AddSupportedOutputContentTypes(IEnumerable<string> contentTypes)
        {
            foreach (string contentType in contentTypes)
            {
                this.supportedOutputContentTypes.Add(contentType);
            }
        }

        public void AddSupportedInputContentTypes(IEnumerable<string> contentTypes)
        {
            foreach (string contentType in contentTypes)
            {
                this.supportedInputContentTypes.Add(contentType);
            }
        }

        public bool IsTypeSupportedForJsonApiInput(Type type)
        {
            if (typeof(IEnumerable).IsAssignableFrom(type) 
                && type.IsGenericType)
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
            return type != null
                   && (this.IsMappingRegistered(type)
                       || type.IsAssignableFrom(typeof(CompoundDocument)));
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
            return this.supportedInputContentTypes.Any(contentType => string.Equals(mimeType, contentType, StringComparison.OrdinalIgnoreCase));
        }

        public void BeforeSerialization(PreSerializationContext context)
        {
            foreach (Action<PreSerializationContext> action in this.preSerializationActions)
            {
                action(context);
            }
        }

        public void OverrideResponseHeaders(OverrideResponseHeadersContext context)
        {
            foreach (Action<OverrideResponseHeadersContext> action in this.overrideResponseHeadersActions)
            {
                action(context);
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
