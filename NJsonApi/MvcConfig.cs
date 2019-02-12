using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NJsonApi.Filters;
using NJsonApi.Serialization;

namespace NJsonApi
{
    public static class MvcConfig
    {
        public static IMvcBuilder UseJsonApi(this IMvcBuilder mvcBuilder, Action<ConfigurationBuilder> setupAction)
        {
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            setupAction(configurationBuilder);
            Configuration configuration = configurationBuilder.Build();

            mvcBuilder.AddMvcOptions(mvcOptions => 
            {
                //mvcOptions.Conventions.Add(new ApiExplorerVisibilityEnabledConvention());
                //mvcOptions.Filters.Add(typeof(JsonApiResourceFilter));
                //mvcOptions.Filters.Add(typeof(JsonApiActionFilter));
                mvcOptions.Filters.Add(typeof(JsonApiExceptionFilter));
                //mvcOptions.OutputFormatters.Insert(0, new JsonApiOutputFormatter(nJsonApiConfig));
                //mvcOptions.InputFormatters.OfType<JsonInputFormatter>().First().SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/vnd.api+json"));
            });
            //services.AddSingleton<ILinkBuilder, LinkBuilder>();
            mvcBuilder.Services.AddSingleton(configuration.GetJsonSerializer());
            mvcBuilder.Services.AddSingleton<IJsonApiTransformer, JsonApiTransformer>();
            //services.AddSingleton(nJsonApiConfig);
            mvcBuilder.Services.AddSingleton<TransformationHelper>();
            return mvcBuilder;
        }
    }
}
