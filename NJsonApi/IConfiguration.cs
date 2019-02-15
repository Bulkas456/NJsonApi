using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NJsonApi.Formatter.Input;
using NJsonApi.Serialization;

namespace NJsonApi
{
    public interface IConfiguration
    {
        JsonSerializer Serializer { get; }

        IJsonApiTransformer JsonApiTransformer { get; }

        IReadOnlyList<string> SupportedContentTypes { get; }

        bool SupportInputConversionFromJsonApi { get; }

        bool IsTypeSupportedForJsonApiInput(Type type);

        IJsonApiInputMapper GetInputMapper(Type type);

        bool IsTypeSupportedForJsonApiOutput(Type type);

        bool IsMappingRegistered(Type type);

        IResourceMapping GetMapping(Type type);

        bool SupportContentType(string mimeType);

        Task BeforeSerialization(PreSerializationContext context);
    }
}
