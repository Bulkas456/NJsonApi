using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NJsonApi.Common.Infrastructure;
using NJsonApi.Exceptions;
using NJsonApi.Serialization.Documents;
using NJsonApi.Serialization.Representations;
using NJsonApi.Serialization.Representations.Relationships;
using NJsonApi.Serialization.Representations.Resources;

namespace NJsonApi.Serialization
{
    public static class TransformationHelper
    {
        public const int RecursionDepthLimit = 10;
        private const string IdPlaceholder = "{id}";
        private const string ParentIdPlaceholder = "{parentId}";
        private const string RelatedIdPlaceholder = "{relatedId}";
        private const string MetaCountAttribute = "count";
        private const string SelfLinkKey = "self";

        public static CompoundDocument HandleException(Exception exception)
        {
            var scfException = exception is NJsonApiBaseException
                ? exception as NJsonApiBaseException
                : new NJsonApiBaseException(exception.Message, exception);

            var compoundDocument = new CompoundDocument
            {
                Errors = new Dictionary<string, Error>
                {
                    { "error", new Error
                    {
                        Id = scfException.Id.ToString(),
                        Title = scfException.Message,
                        Status = ((int)scfException.GetHttpStatusCode()).ToString(CultureInfo.InvariantCulture),
                    }}
                }
            };

            return compoundDocument;
        }

        public static IResourceRepresentation ChooseProperResourceRepresentation(object resource, IEnumerable<SingleResource> representationList)
        {
            return resource is IEnumerable ?
                (IResourceRepresentation)new ResourceCollection(representationList) :
                representationList.Single();
        }

        public static List<SingleResource> CreateIncludedRepresentations(List<object> primaryResourceList, IResourceMapping resourceMapping, Context context)
        {
            var includedList = new List<SingleResource>();
            var alreadyVisitedObjects = new HashSet<object>(primaryResourceList);

            foreach (var resource in primaryResourceList)
                AppendIncludedRepresentationRecursive(resource, resourceMapping, includedList, alreadyVisitedObjects, context);

            return includedList;
        }

        public static void AppendIncludedRepresentationRecursive(object resource, IResourceMapping resourceMapping, List<SingleResource> includedList, HashSet<object> alreadyVisitedObjects, Context context)
        {
            resourceMapping.Relationships
                .Where(rm => rm.InclusionRule != ResourceInclusionRules.ForceOmit)
                .SelectMany(rm => UnifyObjectsToList(rm.RelatedResource(resource)), (rm, o) => new
                {
                    Mapping = rm,
                    RelatedResourceInstance = o,
                })
                .Where(x => !alreadyVisitedObjects.Contains(x.RelatedResourceInstance))
                .ToList()
                .ForEach(x =>
                {
                    alreadyVisitedObjects.Add(x.RelatedResourceInstance);
                    includedList.Add(CreateResourceRepresentation(x.RelatedResourceInstance, x.Mapping.ResourceMapping, context));
                    AppendIncludedRepresentationRecursive(x.RelatedResourceInstance, x.Mapping.ResourceMapping, includedList, alreadyVisitedObjects, context);
                });
        }


        public static List<object> UnifyObjectsToList(object nestedObject)
        {
            var list = new List<object>();
            if (nestedObject != null)
            {
                if (nestedObject is IEnumerable<object>)
                    list.AddRange((IEnumerable<object>)nestedObject);
                else
                    list.Add(nestedObject);
            }

            return list;
        }

        public static void VerifyTypeSupport(Type innerObjectType)
        {
            if (typeof(IMetaDataWrapper).IsAssignableFrom(innerObjectType))
            {
                throw new NotSupportedException(string.Format("Error while serializing type {0}. IEnumerable<MetaDataWrapper<>> is not supported.", innerObjectType));
            }

            if (typeof(IEnumerable).IsAssignableFrom(innerObjectType) && !innerObjectType.IsGenericType)
            {
                throw new NotSupportedException(string.Format("Error while serializing type {0}. Non generic IEnumerable are not supported.", innerObjectType));
            }
        }

        public static object UnwrapResourceObject(object objectGraph)
        {
            if (!(objectGraph is IMetaDataWrapper))
            {
                return objectGraph;
            }
            var metadataWrapper = objectGraph as IMetaDataWrapper;
            return metadataWrapper.GetValue();
        }

        public static Dictionary<string, object> GetMetadata(object objectGraph)
        {
            if (objectGraph is IMetaDataWrapper)
            {
                var metadataWrapper = objectGraph as IMetaDataWrapper;
                return metadataWrapper.MetaData;
            }
            return null;
        }

        public static Type GetObjectType(object objectGraph)
        {
            Type objectType = objectGraph.GetType();
            if (objectGraph is IMetaDataWrapper)
            {
                objectType = objectGraph.GetType().GetGenericArguments()[0];
            }

            if (typeof(IEnumerable).IsAssignableFrom(objectType) && objectType.IsGenericType)
            {
                objectType = objectType.GetGenericArguments()[0];
            }

            return objectType;
        }

        public static SingleResource CreateResourceRepresentation(object objectGraph, IResourceMapping resourceMapping, Context context)
        {
            var result = new SingleResource();

            result.Id = resourceMapping.IdGetter(objectGraph).ToString();
            result.Type = resourceMapping.ResourceType;

            result.Attributes = resourceMapping.PropertyGetters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value(objectGraph));

            if (resourceMapping.UrlTemplate != null)
                result.Links = CreateLinks(resourceMapping, context, result);

            if (resourceMapping.Relationships.Any())
                result.Relationships = CreateRelationships(objectGraph, result.Id, resourceMapping, context);

            return result;
        }

        private static Dictionary<string, ILink> CreateLinks(IResourceMapping resourceMapping, Context context, SingleResource result)
        {
            return new Dictionary<string, ILink>() { { SelfLinkKey, new SimpleLink { Href = context.GetFullyQualifiedUrl(resourceMapping.UrlTemplate.Replace(IdPlaceholder, result.Id)) } } };
        }

        private static ILink GetUrlFromTemplate(Context context, string urlTemplate, string parentId, string relatedId = null)
        {
            return new SimpleLink
            {
                Href = context.GetFullyQualifiedUrl(urlTemplate.Replace(ParentIdPlaceholder, parentId).Replace(RelatedIdPlaceholder, relatedId))
            };
        }

        public static Dictionary<string, IRelationship> CreateRelationships(object objectGraph, string parentId, IResourceMapping resourceMapping, Context context)
        {
            var relationships = new Dictionary<string, IRelationship>();
            foreach (var linkMapping in resourceMapping.Relationships)
            {
                var relationshipName = linkMapping.RelationshipName;
                var rel = new Relationship();
                var relLinks = new RelationshipLinks();

                // Generating "self" link
                if (linkMapping.SelfUrlTemplate != null)
                    relLinks.Self = GetUrlFromTemplate(context, linkMapping.SelfUrlTemplate, parentId);

                if (!linkMapping.IsCollection)
                {
                    string relatedId = null;
                    object relatedInstance = null;
                    if (linkMapping.RelatedResource != null)
                    {
                        relatedInstance = linkMapping.RelatedResource(objectGraph);
                        if (relatedInstance != null)
                            relatedId = linkMapping.ResourceMapping.IdGetter(relatedInstance).ToString();
                    }
                    if (linkMapping.RelatedResourceId != null && relatedId == null)
                    {
                        var id = linkMapping.RelatedResourceId(objectGraph);
                        if (id != null)
                            relatedId = id.ToString();
                    }

                    // Generating "related" link for to-one relationships
                    if (linkMapping.RelatedUrlTemplate != null && relatedId != null)
                        relLinks.Related = GetUrlFromTemplate(context, linkMapping.RelatedUrlTemplate, parentId, relatedId.ToString());


                    if (linkMapping.InclusionRule != ResourceInclusionRules.ForceOmit)
                    {
                        // Generating resource linkage for to-one relationships
                        if (relatedInstance != null)
                            rel.Data = new SingleResourceIdentifier
                            {
                                Id = relatedId,
                                Type = context.Configuration.GetMapping(relatedInstance.GetType()).ResourceType // This allows polymorphic (subtyped) resources to be fully represented
                            };
                        else if (relatedId == null || linkMapping.InclusionRule == ResourceInclusionRules.ForceInclude)
                            rel.Data = new NullResourceIdentifier(); // two-state null case, see NullResourceIdentifier summary
                    }
                }
                else
                {
                    // Generating "related" link for to-many relationships
                    if (linkMapping.RelatedUrlTemplate != null)
                        relLinks.Related = GetUrlFromTemplate(context, linkMapping.RelatedUrlTemplate, parentId);

                    IEnumerable relatedInstance = null;
                    if (linkMapping.RelatedResource != null)
                        relatedInstance = (IEnumerable)linkMapping.RelatedResource(objectGraph);

                    // Generating resource linkage for to-many relationships
                    if (linkMapping.InclusionRule == ResourceInclusionRules.ForceInclude && relatedInstance == null)
                        rel.Data = new MultipleResourceIdentifiers();
                    if (linkMapping.InclusionRule != ResourceInclusionRules.ForceOmit && relatedInstance != null)
                    {
                        var idGetter = linkMapping.ResourceMapping.IdGetter;
                        var identifiers = relatedInstance
                            .Cast<object>()
                            .Select(o => new SingleResourceIdentifier
                            {
                                Id = idGetter(o).ToString(),
                                Type = context.Configuration.GetMapping(o.GetType()).ResourceType // This allows polymorphic (subtyped) resources to be fully represented
                            });
                        rel.Data = new MultipleResourceIdentifiers(identifiers);
                    }

                    // If data is present, count meta attribute is added for convenience
                    if (rel.Data != null)
                        rel.Meta = new Dictionary<string, string> { { MetaCountAttribute, ((MultipleResourceIdentifiers)rel.Data).Count.ToString() } };
                }

                if (relLinks.Self != null || relLinks.Related != null)
                    rel.Links = relLinks;

                if (rel.Data != null || rel.Links != null)
                    relationships.Add(relationshipName, rel);
            }
            return relationships.Any() ? relationships : null;
        }

        public static void AssureAllMappingsRegistered(Type type, IConfiguration config)
        {
            if (!config.IsMappingRegistered(type))
            {
                throw new MissingMappingException(type);
            }
        }

        public static object GetCollection(JToken value, IRelationshipMapping mapping)
        {
            IList resultValue;

            if (!(value is JArray))
                throw new InvalidOperationException("Json array expected.");

            try
            {
                var array = (JArray)value;
                var listOfItems = array
                    .ToObject<List<string>>()
                    .Select(id =>
                    {
                        var obj = Activator.CreateInstance(mapping.ResourceMapping.ResourceRepresentationType);
                        mapping.ResourceMapping.IdSetter(obj, id);
                        return obj;
                    });

                resultValue = (IList)Activator.CreateInstance(mapping.RelatedCollectionProperty.PropertyType);
                foreach (var item in listOfItems)
                {
                    resultValue.Add(item);
                }
            }
            catch
            {
                resultValue = null;
            }
            return resultValue;
        }

        public static object GetValue(JToken value, Type returnType)
        {
            object resultValue;
            try
            {
                if (returnType == typeof(string) && value.Value<string>() == null)
                {
                    resultValue = null;
                }
                else
                {
                    resultValue = Convert.ChangeType(value, returnType);
                }
            }
            catch
            {
                resultValue = default(ValueType);
            }
            return resultValue;
        }
    }
}