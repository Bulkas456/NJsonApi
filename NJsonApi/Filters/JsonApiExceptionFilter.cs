using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NJsonApi.Serialization;

namespace NJsonApi.Filters
{
    public class JsonApiExceptionFilter : ExceptionFilterAttribute
    {
        private readonly IJsonApiTransformer jsonApiTransformer;

        public JsonApiExceptionFilter(IJsonApiTransformer jsonApiTransformer)
        {
            this.jsonApiTransformer = jsonApiTransformer;
        }

        public override void OnException(ExceptionContext context)
        {
            context.Result = new ObjectResult(jsonApiTransformer.Transform(context.Exception));
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
    }
}
