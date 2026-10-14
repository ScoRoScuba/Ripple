namespace Ripple.Api.Cors;

public static class CorsExtensions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Response headers browser clients need to read: the URL of a created resource and the
    /// supported API versions. Browsers hide non-safelisted headers unless they are exposed.
    /// </summary>
    private static readonly string[] ExposedHeaders = ["Location", "api-supported-versions"];

    /// <summary>
    /// Default CORS policy allowing only the origins listed in <c>Cors:AllowedOrigins</c>.
    /// With no origins configured, no cross-origin requests are allowed.
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = (configuration.GetSection($"{SectionName}:AllowedOrigins").Get<string[]>() ?? [])
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim().TrimEnd('/')) // "https://app.example.com/" never matches the Origin header
            .ToArray();

        services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders(ExposedHeaders)));

        return services;
    }

    public static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app) => app.UseCors();
}
