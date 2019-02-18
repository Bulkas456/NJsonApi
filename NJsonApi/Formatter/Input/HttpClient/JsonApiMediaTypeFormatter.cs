using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using NJsonApi.Serialization.Converters;
using NJsonApi.Serialization.Documents;

namespace NJsonApi.Formatter.Input.HttpClient
{
    public class JsonApiMediaTypeFormatter<TData> : BufferedMediaTypeFormatter
    {
        private readonly JsonSerializer jsonSerializer;
        private readonly IJsonApiInputMapper mapper;

        public JsonApiMediaTypeFormatter(IJsonApiInputMapper mapper, params string[] supportedMediaTypes)
        {
            this.mapper = mapper;
            this.jsonSerializer = new JsonSerializer
            {
                Converters =
                {
                    new RelationshipConverter(),
                    new ResourceConverter(),
                    new LinkConverter(),
                    new ResourceLinkageConverter(),
                }
            };

            SupportedMediaTypes.Add(new MediaTypeHeaderValue(Constants.JsonApiContentType));

            foreach (string mediaType in supportedMediaTypes)
            {
                SupportedMediaTypes.Add(new MediaTypeHeaderValue(mediaType));
            }

            SupportedEncodings.Add(new UTF8Encoding(false, true));
        }

        public override object ReadFromStream(Type type, Stream readStream, HttpContent content, IFormatterLogger formatterLogger)
        {
            using (StreamReader reader = new StreamReader(readStream))
            {
                using (JsonTextReader jsonReader = new JsonTextReader(reader))
                {
                    CompoundDocument compoundDocument = this.jsonSerializer.Deserialize<CompoundDocument>(jsonReader);

                    if (compoundDocument == null)
                    {
                        return null;
                    }

                    return this.mapper.Map(compoundDocument);
                }
            }
        }

        public override bool CanReadType(Type type)
        {
            return type == typeof(TData);
        }

        public override bool CanWriteType(Type type)
        {
            return false;
        }
    }
}
