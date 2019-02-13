using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace NJsonApi
{
    public static class Extensions
    {
        public static Context ToContext(this HttpContext httpContext)
        {
            return new Context()
            {
                RequestUri = new Uri(httpContext.Request.GetDisplayUrl())
            };
        }
    }
}
