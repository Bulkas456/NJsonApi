using System;
using System.Collections.Generic;
using System.Text;
using NJsonApi.Serialization.Documents;

namespace NJsonApi.Formatter.Input
{
    public interface IJsonApiInputMapper
    {
        object Map(CompoundDocument input);
    }
}
