using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;

namespace NJsonApi.Formatter.Input.HttpClient
{
    public static class HttpClientExtensions
    {
        public static Task<TData> ReadAsAsyncFromJsonApi<TData>(this HttpContent httpContent, IJsonApiInputMapper mapper, params string[] supportedMediaTypes)
        {
            return httpContent.ReadAsAsync<TData>(new MediaTypeFormatter[]
            {
                new JsonApiMediaTypeFormatter<TData>(mapper, supportedMediaTypes)
            });
        }
    }
}
