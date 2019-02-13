using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NJsonApi.HelloWorld.Models;
using NJsonApi.Serialization.Documents;

namespace NJsonApi.HelloWorld
{
    public static class NJsonApiConfig
    {
        public static void Configure(ConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.WithJsonApiInputFor<World>(new WorldInputMapper());

            configurationBuilder
                .Resource<World>()
                .WithAllProperties()
                .WithLinkTemplate("/worlds/{id}");

            configurationBuilder
                .Resource<Continent>()
                .WithAllProperties()
                .WithLinkTemplate("/continents/{id}");
        }
    }

    internal class WorldInputMapper : Formatter.Input.IJsonApiInputMapper
    {
        public object Map(CompoundDocument input)
        {
            var a = input.Data as Serialization.Representations.Resources.SingleResource;
            return new World()
            {
                Id = int.Parse(a.Id, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture),
                Name = (string)a.Attributes["name"],
                //Continents = a.Relationships["continents"]
            };
        }
    }
}
