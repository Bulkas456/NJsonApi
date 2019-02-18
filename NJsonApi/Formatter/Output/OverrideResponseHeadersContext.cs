using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace NJsonApi.Formatter.Output
{
    public class OverrideResponseHeadersContext
    {
        public HttpContext HttpContext { get; set; }
        public Type Type { get; set; }
        public object Value { get; set; }
    }
}
