using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using NJsonApi.Conventions;
using NJsonApi.Conventions.Impl;
using NJsonApi.Formatter.Input;
using NJsonApi.Formatter.Output;

namespace NJsonApi
{
    public class ConfigurationBuilder
    {
        public readonly Dictionary<Type, IResourceConfigurationBuilder> ResourceConfigurationsByType = new Dictionary<Type, IResourceConfigurationBuilder>();
        private readonly Stack<IConvention> conventions = new Stack<IConvention>();
        private readonly Dictionary<Type, IJsonApiInputMapper> inputMappers = new Dictionary<Type, IJsonApiInputMapper>();
        private readonly List<Action<PreSerializationContext>> preSerializationActions = new List<Action<PreSerializationContext>>();
        private readonly List<Action<OverrideResponseHeadersContext>> overrideResponseHeadersActios = new List<Action<OverrideResponseHeadersContext>>();
        private readonly HashSet<string> supportedOutputTypes = new HashSet<string>();
        private Func<JsonSerializer> jsonSerialzierFactory;
        public ConfigurationBuilder()
        {
            //add the default conventions
            WithConvention(new PluralizedCamelCaseTypeConvention());
            WithConvention(new CamelCaseLinkNameConvention());
            WithConvention(new SimpleLinkedIdConvention());
            WithConvention(new DefaultPropertyScanningConvention());
        }

        public ConfigurationBuilder WithConvention<T>(T convention) where T : class, IConvention
        {
            conventions.Push(convention);
            return this;
        }

        public ConfigurationBuilder WithJsonApiInputFor<TResource>(IJsonApiInputMapper mapper)
        {
            this.inputMappers[typeof(TResource)] = mapper;
            return this;
        }

        public ConfigurationBuilder WithPreOutputSerializationAction(Action<PreSerializationContext> action)
        {
            this.preSerializationActions.Add(action);
            return this;
        }

        public ConfigurationBuilder WithOverrideResponseHeadersAction(Action<OverrideResponseHeadersContext> action)
        {
            this.overrideResponseHeadersActios.Add(action);
            return this;
        }

        public ConfigurationBuilder WithJsonSerializerFactory(Func<JsonSerializer> factory)
        {
            this.jsonSerialzierFactory = factory;
            return this;
        }

        public ConfigurationBuilder WithSupportedOutputContentTypes(params string[] contentTypes)
        {
            foreach (string contentType in contentTypes)
            {
                this.supportedOutputTypes.Add(contentType);
            }

            return this;
        }

        public T GetConvention<T>() where T : class, IConvention
        {
            var firstMatchingConvention = conventions
                .OfType<T>()
                .FirstOrDefault();
            if (firstMatchingConvention == null)
                throw new InvalidOperationException(string.Format("No convention found for type {0}", typeof(T).Name));
            return firstMatchingConvention;
        }

        public ResourceConfigurationBuilder<TResource> Resource<TResource>()
        {
            if (!ResourceConfigurationsByType.ContainsKey(typeof(TResource)))
            {
                var newResourceConfiguration = new ResourceConfigurationBuilder<TResource>(this) { ConfigurationBuilder = this };
                ResourceConfigurationsByType[typeof(TResource)] = newResourceConfiguration;
                return newResourceConfiguration;
            }
            else
            {
                return ResourceConfigurationsByType[typeof(TResource)] as ResourceConfigurationBuilder<TResource>;
            }
        }

        public IConfiguration Build()
        {
            Configuration configuration = new Configuration()
            {
                JsonSerializerFactory = this.jsonSerialzierFactory
            };
            configuration.AddInputMapper(this.inputMappers);
            configuration.AddPreSerializationAction(this.preSerializationActions);
            configuration.AddOverrideResponseHeadersAction(this.overrideResponseHeadersActios);
            configuration.AddSupportedOutputContentTypes(this.supportedOutputTypes);
            var propertyScanningConvention = GetConvention<IPropertyScanningConvention>();

            // Each link needs to be wired to full metadata once all resources are registered
            foreach (var resourceConfiguration in ResourceConfigurationsByType)
            {
                var links = resourceConfiguration.Value.ConstructedMetadata.Relationships;
                for (int i = links.Count - 1; i >= 0; i--)
                {
                    var link = links[i];
                    IResourceConfigurationBuilder resourceConfigurationOutput;
                    if (!ResourceConfigurationsByType.TryGetValue(link.RelatedBaseType, out resourceConfigurationOutput))
                    {
                        if (propertyScanningConvention.ThrowOnUnmappedLinkedType)
                        {
                            throw new InvalidOperationException(
                                string.Format(
                                    "Type {0} was registered to have a linked resource {1} of type {2} which was not registered. Register resource type {2} or disable serialization of that property.",
                                    link.ParentType.Name,
                                    link.RelationshipName,
                                    link.RelatedBaseType.Name));
                        }
                        else
                        {
                            links.RemoveAt(i);
                        }
                    }
                    else
                    {
                        link.ResourceMapping = resourceConfigurationOutput.ConstructedMetadata;
                    }
                }

                configuration.AddMapping(resourceConfiguration.Value.ConstructedMetadata);
            }

            return configuration;
        }
    }
}
