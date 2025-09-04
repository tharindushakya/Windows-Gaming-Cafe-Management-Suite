using System;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;

namespace GamingCafe.API.Swagger;

/// <summary>
/// Configures Swagger generation options to create one Swagger doc per API version.
/// </summary>
public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            var info = new OpenApiInfo
            {
                Title = $"GamingCafe API {description.GroupName}",
                Version = description.ApiVersion.ToString(),
                Description = "GamingCafe Management Suite API"
            };

            if (description.IsDeprecated)
            {
                info.Description += "\n\nNote: This API version has been deprecated.";
            }

            options.SwaggerDoc(description.GroupName, info);
        }
    }
}
