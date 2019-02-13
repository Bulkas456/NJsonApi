using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NJsonApi.Conventions;
using NJsonApi.Conventions.Impl;
using NJsonApi.Formatter.Input;

namespace NJsonApi
{
    public class ConfigurationBuilder
    {
        public readonly Dictionary<Type, IResourceConfigurationBuilder> ResourceConfigurationsByType = new Dictionary<Type, IResourceConfigurationBuilder>();

        private readonly Stack<IConvention> conventions = new Stack<IConvention>();
        private readonly Dictionary<Type, IJsonApiInputMapper> inputMappers = new Dictionary<Type, IJsonApiInputMapper>();
        private readonly List<Func<PreSerializationContext, Task>> preSerializationActions = new List<Func<PreSerializationContext, Task>>();
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

        public ConfigurationBuilder WithPreSerializationAction(Func<PreSerializationContext, Task> action)
        {
            this.preSerializationActions.Add(action);
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

        public Configuration Build()
        {
            var configuration = new Configuration();
            configuration.AddInputMapper(this.inputMappers);
            configuration.AddPreSerializationAction(this.preSerializationActions);
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
                            links.RemoveAt(i);
                    }
                    else
                        link.ResourceMapping = resourceConfigurationOutput.ConstructedMetadata;
                }

                configuration.AddMapping(resourceConfiguration.Value.ConstructedMetadata);
            }

            return configuration;
        }
    }
}
