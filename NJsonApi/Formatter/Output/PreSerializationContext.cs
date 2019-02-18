using System;
using System.Collections.Generic;
using System.Text;
using NJsonApi.Serialization.Documents;

namespace NJsonApi.Formatter.Output
{
    public class PreSerializationContext
    {
        public CompoundDocument CompoundDocument { get; set; }
        public Type Type { get; set; }
        public object Value { get; set; }
    }
}
