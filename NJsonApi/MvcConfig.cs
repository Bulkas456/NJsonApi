using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NJsonApi.Filters;
using NJsonApi.Serialization;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using NJsonApi.Formatter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Internal;

namespace NJsonApi
{
    public static class MvcConfig
    {
        public static IMvcBuilder UseJsonApi(this IMvcBuilder mvcBuilder, Action<ConfigurationBuilder> setupAction)
        {
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            setupAction(configurationBuilder);
            IConfiguration configuration = configurationBuilder.Build();

            mvcBuilder.AddMvcOptions(mvcOptions => 
            {
                //mvcOptions.Conventions.Add(new ApiExplorerVisibilityEnabledConvention());
                //mvcOptions.Filters.Add(typeof(JsonApiResourceFilter));
                //mvcOptions.Filters.Add(typeof(JsonApiActionFilter));
                mvcOptions.Filters.Add(typeof(JsonApiExceptionFilter));
                mvcOptions.OutputFormatters.Insert(0, new JsonApiOutputFormatter(configuration));
                //mvcOptions.InputFormatters.OfType<JsonInputFormatter>().First().SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/vnd.api+json"));
                // mvcOptions.InputFormatters.Insert(0, new JsonApiFormatter(configuration));

                if (configuration.SupportInputConversionFromJsonApi)
                {
                    mvcOptions.InputFormatters.Insert(0, new JsonApiInputFormatter(configuration));
                }
            });

            mvcBuilder.Services.AddSingleton(configuration.Serializer);
            mvcBuilder.Services.AddSingleton<IJsonApiTransformer>(configuration.JsonApiTransformer);
            mvcBuilder.Services.AddSingleton(configuration);
            return mvcBuilder;
        }

        public static IApplicationBuilder UseBufferedRequestBody(this IApplicationBuilder applicationBuilder)
        {
            return applicationBuilder.Use(next => context => { context.Request.EnableRewind(); return next(context); });
        }
    }
}
