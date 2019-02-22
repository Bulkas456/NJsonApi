using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using NJsonApi.Serialization.Documents;
using System.Linq;

namespace NJsonApi.Formatter.Output
{
    public class JsonApiOutputFormatter : TextOutputFormatter
    {
        private readonly IConfiguration configuration;

        public JsonApiOutputFormatter(IConfiguration configuration)
        {
            this.configuration = configuration;

            foreach (string contentType in this.configuration.SupportedOutputContentTypes)
            {
                this.SupportedMediaTypes.Add(contentType);
            }

            this.SupportedEncodings.Add(Encoding.UTF8);
            this.SupportedEncodings.Add(Encoding.Unicode);
        }

        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            return this.configuration.IsTypeSupportedForJsonApiOutput(context.ObjectType);
        }

        public override IReadOnlyList<string> GetSupportedContentTypes(string contentType, Type objectType)
        {
            return this.configuration.SupportedOutputContentTypes;
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            CompoundDocument value = this.configuration.JsonApiTransformer.Transform(context.Object, context.HttpContext.ToContext(this.configuration));
            this.configuration.BeforeSerialization(new PreSerializationContext()
            {
                CompoundDocument = value,
                Type = context.ObjectType,
                Value = context.Object
            });

            using (StreamWriter streamWriter = new StreamWriter(context.HttpContext.Response.Body, selectedEncoding, 1024, true))
            {
                using (JsonTextWriter jsonWriter = new JsonTextWriter(streamWriter))
                {
                    this.configuration.Serializer.Serialize(jsonWriter, value);
                }
            }

            return Task.CompletedTask;
        }

        public override void WriteResponseHeaders(OutputFormatterWriteContext context)
        {
            base.WriteResponseHeaders(context);
            this.configuration.OverrideResponseHeaders(new OverrideResponseHeadersContext()
            {
                HttpContext = context.HttpContext,
                Type = context.ObjectType,
                Value = context.Object
            });

            IResourceMapping mapping = this.configuration.GetMapping(context.ObjectType);

            if (mapping != null
                && !string.IsNullOrEmpty(mapping.OutputContentType))
            {
                context.HttpContext.Response.ContentType = mapping.OutputContentType;
            }
        }
    }
}
