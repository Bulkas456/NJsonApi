using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;
using NJsonApi.Serialization;
using NJsonApi.Serialization.Documents;

namespace NJsonApi
{
    public static class Extensions
    {
        public static Context ToContext(this HttpContext httpContext, IConfiguration configuration)
        {
            return new Context()
            {
                RequestUri = new Uri(httpContext.Request.GetDisplayUrl()),
                Configuration = configuration
            };
        }

        public static TResult RequestBodyTo<TResult>(this JsonSerializer serializer, HttpContext httpContext, Encoding encoding)
        {
            using (StreamReader reader = new StreamReader(httpContext.Request.Body, encoding, true, 1024, true))
            {
                using (JsonTextReader jsonReader = new JsonTextReader(reader))
                {
                    return serializer.Deserialize<TResult>(jsonReader);
                }
            }
        }
    }
}
