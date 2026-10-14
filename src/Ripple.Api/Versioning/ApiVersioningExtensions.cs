using Asp.Versioning;

namespace Ripple.Api.Versioning;

public static class ApiVersioningExtensions
{
    /// <summary>
    /// URL-segment versioning (<c>/api/v1/...</c>). Responses report supported versions in the
    /// <c>api-supported-versions</c> header; Swagger groups are named <c>v1</c>, <c>v2</c>, ...
    /// </summary>
    public static IServiceCollection AddVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = ApiVersions.V1;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
