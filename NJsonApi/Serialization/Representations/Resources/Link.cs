using System;
using System.Collections.Generic;
using System.Text;

namespace NJsonApi.Serialization.Representations.Resources
{
    public class Link : ILink
    {
        public string Href { get; set; }

        public Dictionary<string, object> Meta { get; set; }
    }
}
