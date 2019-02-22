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
using NJsonApi.Formatter.Output;
using NJsonApi.Formatter.Input;

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

                if (configuration.SupportInputConversionFromJsonApi)
                {
                    mvcOptions.InputFormatters.Insert(0, new JsonApiInputFormatter(configuration));
                }

                mvcOptions.InputFormatters.OfType<JsonInputFormatter>().First().SupportedMediaTypes.Add(new MediaTypeHeaderValue(Constants.JsonApiContentType));
            });

            mvcBuilder.Services.AddSingleton(configuration.Serializer);
            mvcBuilder.Services.AddSingleton<IJsonApiTransformer>(configuration.JsonApiTransformer);
            mvcBuilder.Services.AddSingleton(configuration);
            return mvcBuilder;
        }
    }
}
