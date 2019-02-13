using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using NJsonApi.Serialization;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace NJsonApi.Filters
{
    public class JsonApiResourceFilter : Attribute, IResourceFilter
    {
        private readonly IJsonApiTransformer jsonApiTransformer;
        private readonly JsonSerializer serializer;

        public JsonApiResourceFilter(
            IJsonApiTransformer jsonApiTransformer,
            JsonSerializer serializer)
        {
            this.jsonApiTransformer = jsonApiTransformer;
            this.serializer = serializer;
        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            ParameterDescriptor actionDescriptorForBody = context.ActionDescriptor
                                                                 .Parameters
                                                                 .SingleOrDefault(x => 
                                                                    x.BindingInfo != null 
                                                                    && x.BindingInfo.BindingSource == BindingSource.Body);
            if (actionDescriptorForBody == null)
            {
                return;
            }

            using (StreamReader reader = new StreamReader(context.HttpContext.Request.Body))
            {
                context.ActionDescriptor.Properties[actionDescriptorForBody.Name] = reader.ReadToEnd();
            }
        }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
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
            }*/
        }
    }
}
