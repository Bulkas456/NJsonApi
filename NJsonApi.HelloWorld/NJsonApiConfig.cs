using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NJsonApi.HelloWorld.Models;

namespace NJsonApi.HelloWorld
{
    public static class NJsonApiConfig
    {
        public static void Configure(ConfigurationBuilder configurationBuilder)
        {
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
}
