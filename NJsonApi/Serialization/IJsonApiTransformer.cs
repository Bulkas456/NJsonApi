using System;
using Newtonsoft.Json;
using NJsonApi.Common.Infrastructure;
using NJsonApi.Serialization.Documents;

namespace NJsonApi.Serialization
{
    public interface IJsonApiTransformer
    {
        CompoundDocument Transform(Exception error);
        CompoundDocument Transform(object objectGraph, Context context);
        IDelta TransformBack(UpdateDocument updateDocument, Type type, Context context);
    }
}