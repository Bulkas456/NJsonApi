using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace NJsonApi
{
    public class Context
    {
        private readonly Lazy<Uri> baseUri;
        public Context()
        {
            this.baseUri = new Lazy<Uri>(() =>
            {
                UriComponents authority = (UriComponents.Scheme | UriComponents.UserInfo | UriComponents.Host | UriComponents.Port);
                return new Uri(RequestUri.GetComponents(authority, UriFormat.SafeUnescaped));
            });
        }

        public Configuration Configuration { get; set; }
        public Uri RequestUri { get; set; }
        public Uri BaseUri => this.baseUri.Value;
    }
}
