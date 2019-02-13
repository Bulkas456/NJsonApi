using System;
using System.Linq;
using System.Linq.Expressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonApi.Common.Infrastructure;
using NJsonApi.Serialization.Documents;

namespace NJsonApi.Serialization
{
    public class JsonApiTransformer : IJsonApiTransformer
    {
        private readonly JsonSerializer serializer;
        private readonly TransformationHelper transformationHelper;

        public JsonApiTransformer(
            JsonSerializer serializer,
            TransformationHelper transformationHelper)
        {
            this.serializer = serializer;
            this.transformationHelper = transformationHelper;
        }

        public CompoundDocument Transform(Exception error)
        {
            return this.transformationHelper.HandleException(error);
        }

        public CompoundDocument Transform(object objectGraph, Context context)
        {
            CompoundDocument result = objectGraph as CompoundDocument;

            if (result != null)
            {
                return result;
            }

            Type innerObjectType = this.transformationHelper.GetObjectType(objectGraph);
            this.transformationHelper.VerifyTypeSupport(innerObjectType);
            this.transformationHelper.AssureAllMappingsRegistered(innerObjectType, context.Configuration);

            result = new CompoundDocument
            {
                Meta = this.transformationHelper.GetMetadata(objectGraph)
            };

            var resource = this.transformationHelper.UnwrapResourceObject(objectGraph);
            var resourceMapping = context.Configuration.GetMapping(innerObjectType);

            var resourceList = this.transformationHelper.UnifyObjectsToList(resource);
            var representationList = resourceList.Select(o => this.transformationHelper.CreateResourceRepresentation(o, resourceMapping, context));
            var primaryResource = this.transformationHelper.ChooseProperResourceRepresentation(resource, representationList);

            result.Data = primaryResource;

            if (resourceMapping.Relationships.Any())
            {
                result.Included = this.transformationHelper.CreateIncludedRepresentations(resourceList, resourceMapping, context);
            }

            return result;
        }

        public IDelta TransformBack(UpdateDocument updateDocument, Type type, Context context)
        {
            var mapping = context.Configuration.GetMapping(type);
            var openGeneric = typeof(Delta<>);
            var closedGenericType = openGeneric.MakeGenericType(type);
            var delta = Activator.CreateInstance(closedGenericType) as IDelta;

            if (delta == null)
            {
                return null;
            }

            var resourceKey = mapping.ResourceType;
            if (!updateDocument.Data.ContainsKey(resourceKey))
            {
                return delta;
            }

            var resource = updateDocument.Data[resourceKey] as JObject;
            if (resource == null)
            {
                return delta;
            }

            // Scan the data for which properties are only set
            foreach (var propertySetter in mapping.PropertySettersExpressions)
            {
                JToken value;
                resource.TryGetValue(propertySetter.Key, StringComparison.CurrentCultureIgnoreCase, out value);
                if (value == null)
                {
                    continue;
                }
                // Set only the properties that are present
                var methodCallExpression = propertySetter.Value.Body as MethodCallExpression;
                if (methodCallExpression != null)
                {
                    Type returnType = methodCallExpression.Arguments[0].Type;

                    var resultValue = this.transformationHelper.GetValue(value, returnType);

                    string key = propertySetter.Key.TrimStart('_');
                    delta.ObjectPropertyValues.Add(key, resultValue);
                }
            }

            JToken linksToken;
            resource.TryGetValue("links", StringComparison.CurrentCultureIgnoreCase, out linksToken);
            JObject links = linksToken as JObject;

            if (links != null)
            {
                foreach (var link in mapping.Relationships)
                {
                    JToken value;
                    links.TryGetValue(link.RelationshipName, StringComparison.CurrentCultureIgnoreCase, out value);
                    if (value == null)
                    {
                        continue;
                    }

                    if (link.IsCollection)
                    {
                        var property = link.RelatedCollectionProperty;
                        if (property != null)
                        {
                            var resultValue = this.transformationHelper.GetCollection(value, link);

                            string key = link.RelationshipName.TrimStart('_');
                            delta.ObjectPropertyValues.Add(key, resultValue);
                        }    
                    }
                    else
                    {
                        delta.ObjectPropertyValues.Add(link.ParentResourceNavigationPropertyName, this.transformationHelper.GetValue(value, link.ParentResourceNavigationPropertyType));
                    }
                }
            }

            return delta;
        }
    }

}