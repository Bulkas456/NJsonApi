﻿using System;
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
        public CompoundDocument Transform(Exception error)
        {
            return TransformationHelper.HandleException(error);
        }

        public CompoundDocument Transform(object objectGraph, Context context)
        {
            CompoundDocument result = objectGraph as CompoundDocument;

            if (result != null)
            {
                return result;
            }

            Type innerObjectType = TransformationHelper.GetObjectType(objectGraph);
            TransformationHelper.VerifyTypeSupport(innerObjectType);
            TransformationHelper.AssureAllMappingsRegistered(innerObjectType, context.Configuration);

            result = new CompoundDocument
            {
                Meta = TransformationHelper.GetMetadata(objectGraph)
            };

            var resource = TransformationHelper.UnwrapResourceObject(objectGraph);
            var resourceMapping = context.Configuration.GetMapping(innerObjectType);

            var resourceList = TransformationHelper.UnifyObjectsToList(resource);
            var representationList = resourceList.Select(o => TransformationHelper.CreateResourceRepresentation(o, resourceMapping, context));
            var primaryResource = TransformationHelper.ChooseProperResourceRepresentation(resource, representationList);

            result.Data = primaryResource;

            if (resourceMapping.Relationships.Any())
            {
                result.Included = TransformationHelper.CreateIncludedRepresentations(resourceList, resourceMapping, context);
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

                    var resultValue = TransformationHelper.GetValue(value, returnType);

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
                            var resultValue = TransformationHelper.GetCollection(value, link);

                            string key = link.RelationshipName.TrimStart('_');
                            delta.ObjectPropertyValues.Add(key, resultValue);
                        }    
                    }
                    else
                    {
                        delta.ObjectPropertyValues.Add(link.ParentResourceNavigationPropertyName, TransformationHelper.GetValue(value, link.ParentResourceNavigationPropertyType));
                    }
                }
            }

            return delta;
        }
    }

}