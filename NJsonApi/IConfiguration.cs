using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NJsonApi.Formatter.Input;
using NJsonApi.Formatter.Output;
using NJsonApi.Serialization;

namespace NJsonApi
{
    public interface IConfiguration
    {
        JsonSerializer Serializer { get; }

        IJsonApiTransformer JsonApiTransformer { get; }

        IReadOnlyList<string> SupportedInputContentTypes { get; }
        IReadOnlyList<string> SupportedOutputContentTypes { get; }

        bool SupportInputConversionFromJsonApi { get; }

        bool IsTypeSupportedForJsonApiInput(Type type);

        IJsonApiInputMapper GetInputMapper(Type type);

        bool IsTypeSupportedForJsonApiOutput(Type type);

        bool IsMappingRegistered(Type type);

        IResourceMapping GetMapping(Type type);

        bool SupportContentType(string mimeType);

        void BeforeSerialization(PreSerializationContext context);

        void OverrideResponseHeaders(OverrideResponseHeadersContext context);

        bool CreateResponseInJsonApiForUnhandedExceptions { get; }
    }
}
