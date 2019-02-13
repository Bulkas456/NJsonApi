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

namespace NJsonApi.Formatter
{
    public class JsonApiOutputFormatter : TextOutputFormatter
    {
        private readonly Configuration configuration;

        public JsonApiOutputFormatter(Configuration configuration)
        {
            this.configuration = configuration;

            foreach (string contentType in this.configuration.SupportedContentTypes)
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
            return this.configuration.SupportedContentTypes;
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            CompoundDocument value = this.configuration.JsonApiTransformer.Transform(context.Object, context.HttpContext.ToContext(this.configuration));
            await this.configuration.BeforeSerialization(new PreSerializationContext()
            {
                CompoundDocument = value,
                Type = context.ObjectType,
                Value = context.Object
            });

            using (StreamWriter streamWriter = new StreamWriter(context.HttpContext.Response.Body))
            {
                using (JsonTextWriter jsonWriter = new JsonTextWriter(streamWriter))
                {
                    this.configuration.Serializer.Serialize(jsonWriter, value);
                }
            }
        }
    }
}
