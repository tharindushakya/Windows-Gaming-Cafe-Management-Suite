using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;

namespace GamingCafe.API.Swagger;

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(SwaggerGenOptions options)
    {
        // Add a swagger document for each discovered API version
        foreach (var desc in _provider.ApiVersionDescriptions)
        {
            var info = new OpenApiInfo()
            {
                Title = "GamingCafe API",
                Version = desc.ApiVersion.ToString(),
                Description = "Gaming Café Management API"
            };

            if (desc.IsDeprecated)
            {
                info.Description += " - DEPRECATED";
            }

            options.SwaggerDoc(desc.GroupName, info);
        }

        // Resolve conflicts by taking first
        options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    }
}
