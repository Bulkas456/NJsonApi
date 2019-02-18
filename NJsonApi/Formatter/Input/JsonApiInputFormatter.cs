using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using NJsonApi.Common.Infrastructure;
using NJsonApi.Serialization;
using NJsonApi.Serialization.Documents;

namespace NJsonApi.Formatter.Input
{
    public class JsonApiInputFormatter : TextInputFormatter
    {
        private readonly IConfiguration configuration;

        public JsonApiInputFormatter(IConfiguration configuration)
        {
            this.configuration = configuration;

            foreach (string contentType in this.configuration.SupportedInputContentTypes)
            {
                this.SupportedMediaTypes.Add(contentType);
            }

            this.SupportedEncodings.Add(new UTF8Encoding());
            this.SupportedEncodings.Add(new UnicodeEncoding());
        }

        public override bool CanRead(InputFormatterContext context)
        {
            return this.configuration.IsTypeSupportedForJsonApiInput(context.ModelType);
        }

        public override IReadOnlyList<string> GetSupportedContentTypes(string contentType, Type objectType)
        {
            return this.configuration.SupportedInputContentTypes;
        }

        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            CompoundDocument document = this.configuration.Serializer.RequestBodyTo<CompoundDocument>(context.HttpContext);
            object convertedObject = this.configuration.GetInputMapper(context.ModelType).Map(document);
            return InputFormatterResult.SuccessAsync(convertedObject);
        }
    }
}
