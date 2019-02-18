using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NJsonApi.Formatter.Input;
using NJsonApi.HelloWorld.Models;
using NJsonApi.Serialization.Documents;
using NJsonApi.Serialization.Representations.Resources;

namespace NJsonApi.HelloWorld
{
    public static class NJsonApiConfig
    {
        public static void Configure(ConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.WithJsonApiInputFor<World>(new WorldInputMapper())
                .WithPreOutputSerializationAction(context => 
                {
                })
                .WithOverrideResponseHeadersAction(context => 
                {
                    context.HttpContext.Response.ContentType = "from action";
                })
                .WithSupportedOutputContentTypes("contentType1", "contentType2");

            configurationBuilder
                .Resource<World>()
                .WithAllProperties()
                .WithLinkTemplate("/worlds/{id}")
                .WithOutputContentType("customContentType");

            configurationBuilder
                .Resource<Continent>()
                .WithAllProperties()
                .WithLinkTemplate("/continents/{id}");
        }
    }

    internal class WorldInputMapper : IJsonApiInputMapper
    {
        public IEnumerable<string> SupportedContentTypes => new string[] { "supported input content type" };

        public object Map(CompoundDocument input)
        {
            SingleResource data = (SingleResource)input.Data;
            return new World()
            {
                Id = int.Parse(data.Id, NumberStyles.Integer, CultureInfo.InvariantCulture),
                Name = (string)data.Attributes["name"],
                //Continents = a.Relationships["continents"]
            };
        }
    }
}
