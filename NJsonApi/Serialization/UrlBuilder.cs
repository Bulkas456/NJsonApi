using System;
using System.Web;

namespace NJsonApi.Serialization
{
    public static class UrlBuilder
    {
        public static string GetFullyQualifiedUrl(this Context context, string urlTemplate)
        {
            if (Uri.TryCreate(urlTemplate, UriKind.Absolute, out Uri fullyQualiffiedUrl))
            {
                return fullyQualiffiedUrl.ToString();
            }

            if (!Uri.TryCreate(context.BaseUri, new Uri(urlTemplate, UriKind.Relative), out fullyQualiffiedUrl))
            {
                throw new ArgumentException(string.Format("Unable to create fully qualified url for urltemplate = '{0}'", urlTemplate));
            }

            return fullyQualiffiedUrl.ToString();
        }
    }
}
