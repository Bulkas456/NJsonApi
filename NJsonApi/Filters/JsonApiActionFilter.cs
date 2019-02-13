using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using NJsonApi.Serialization;
using System.Linq;

namespace NJsonApi.Filters
{
    public class JsonApiActionFilter : ActionFilterAttribute
    {
        private readonly IJsonApiTransformer jsonApiTransformer;
        private readonly Configuration configuration;
        private readonly JsonSerializer serializer;

        public JsonApiActionFilter(
            IJsonApiTransformer jsonApiTransformer,
            Configuration configuration,
            JsonSerializer serializer)
        {
            this.jsonApiTransformer = jsonApiTransformer;
            this.configuration = configuration;
            this.serializer = serializer;
        }

        public bool AllowMultiple => false;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!this.configuration.SupportContentType(context.HttpContext.Request.ContentType))
            {
                return;
            }

            KeyValuePair<string, UpdateDocumentTypeWrapper> updateDocument = context
                    .ActionArguments
                    .Select(argument => new KeyValuePair<string, UpdateDocumentTypeWrapper>(argument.Key, argument.Value as UpdateDocumentTypeWrapper))
                    .FirstOrDefault();

            if (updateDocument.Value != null)
            {
                Type resultType = updateDocument.Value.Type.GetGenericArguments()[0];
                Context jsonApiContext = context.HttpContext.ToContext();
                jsonApiContext.Configuration = this.configuration;
                var result = jsonApiTransformer.TransformBack(updateDocument.Value.UpdateDocument, resultType, jsonApiContext);
                context.ActionArguments[updateDocument.Key] = result;
            }



            /*
            bool isValidContentType = context.HttpContext.Request.ContentType == configuration.DefaultJsonApiMediaType;
            var controllerType = context.Controller.GetType();
            var isControllerRegistered = configuration.IsControllerRegistered(controllerType);
            var actionDescriptorForBody = context.ActionDescriptor.Parameters.SingleOrDefault(
                    x => x.BindingInfo != null && x.BindingInfo.BindingSource == BindingSource.Body);

            if (isControllerRegistered)
            {
                if (!isValidContentType && context.HttpContext.Request.Method != "GET")
                {
                    context.Result = new UnsupportedMediaTypeResult();
                    return;
                }

                if (!ValidateAcceptHeader(context.HttpContext.Request.Headers))
                {
                    context.Result = new StatusCodeResult(406);
                    return;
                }

                if (actionDescriptorForBody != null)
                {
                    var json = (string)context.ActionDescriptor.Properties[actionDescriptorForBody.Name];
                    using (var stringReader = new StringReader(json))
                    {
                        using (var jsonReader = new JsonTextReader(stringReader))
                        {
                            var updateDocument = serializer.Deserialize(jsonReader, typeof(UpdateDocument)) as UpdateDocument;
                            if (updateDocument != null)
                            {
                                var typeInsideDeltaGeneric = actionDescriptorForBody
                                    .ParameterType
                                    .GenericTypeArguments
                                    .Single();

                                var jsonApiContext = new Context(new Uri(context.HttpContext.Request.Host.Value, UriKind.Absolute));
                                var transformed = jsonApiTransformer.TransformBack(updateDocument, typeInsideDeltaGeneric, jsonApiContext);
                                if (context.ActionArguments.ContainsKey(actionDescriptorForBody.Name))
                                {
                                    context.ActionArguments[actionDescriptorForBody.Name] = transformed;
                                }
                                else
                                {
                                    context.ActionArguments.Add(actionDescriptorForBody.Name, transformed);
                                }

                                context.ModelState.Clear();
                            }
                        }
                    }
                }
            }
            else
            {
                if (actionDescriptorForBody != null)
                {
                    var type = (context.Controller as Controller).ControllerContext.ActionDescriptor.Parameters.First().ParameterType;
                    var json = (string)context.ActionDescriptor.Properties[actionDescriptorForBody.Name];
                    using (var stringReader = new StringReader(json))
                    {
                        using (var jsonReader = new JsonTextReader(stringReader))
                        {
                            var obj = serializer.Deserialize(jsonReader, type);
                            if (context.ActionArguments.ContainsKey(actionDescriptorForBody.Name))
                            {
                                context.ActionArguments[actionDescriptorForBody.Name] = obj;
                            }
                            else
                            {
                                context.ActionArguments.Add(actionDescriptorForBody.Name, obj);
                            }
                        }
                    }
                }

                if (isValidContentType)
                {
                    context.Result = new ContentResult()
                    {
                        StatusCode = 406,
                        Content = $"The Content-Type provided was {context.HttpContext.Request.ContentType} but there was no NJsonApiCore configuration mapping for {controllerType.FullName}"
                    };
                }
            }*/
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            //var objectContent = actionExecutedContext.Response.Content as ObjectContent;
            /*if (objectContent != null && objectContent.Formatter.GetType() == typeof(JsonApiFormatter))
            {
                var value = objectContent.Value;
                var context = new Context
                {
                    Configuration = configuration,
                    RoutePrefix = GetRoutePrefix(actionExecutedContext)
                };
                var transformed = jsonApiTransformer.Transform(value, context);

                var jsonApiFormatter = new JsonApiFormatter(configuration, jsonApiTransformer.Serializer);
                actionExecutedContext.Response.Content = new ObjectContent(transformed.GetType(), transformed, jsonApiFormatter);

                HandlePostRequests(actionExecutedContext, transformed);
            }*/





            /*if (context.Result == null || context.Result is NoContentResult)
            {
                return;
            }

            if (BadActionResultTransformer.IsBadAction(context.Result))
            {
                var transformed = BadActionResultTransformer.Transform(context.Result);

                context.Result = new ObjectResult(transformed)
                {
                    StatusCode = transformed.Errors.First().Status
                };
                return;
            }

            var controllerType = context.Controller.GetType();
            var isControllerRegistered = configuration.IsControllerRegistered(controllerType);

            if (isControllerRegistered)
            {
                var responseResult = (ObjectResult)context.Result;
                var relationshipPaths = FindRelationshipPathsToInclude(context.HttpContext.Request);

                if (!configuration.ValidateIncludedRelationshipPaths(relationshipPaths, responseResult.Value))
                {
                    context.Result = new StatusCodeResult(400);
                    return;
                }

                var jsonApiContext = new Context(
                    new Uri(context.HttpContext.Request.GetDisplayUrl()),
                    relationshipPaths)
                {
                    HttpContext = context.HttpContext
                };
                responseResult.Value = jsonApiTransformer.Transform(responseResult.Value, jsonApiContext);
            }*/
        }

        /*private string[] FindRelationshipPathsToInclude(HttpRequest request)
        {
            var includeQueryParameter = request.Query["include"].FirstOrDefault();

            return configuration.FindRelationshipPathsToInclude(includeQueryParameter);
        }

        private bool ValidateAcceptHeader(IHeaderDictionary headers)
        {
            return configuration.ValidateAcceptHeader(headers["Accept"].FirstOrDefault());
        }*/
    }
}
