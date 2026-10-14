using Microsoft.OpenApi;

namespace Ripple.Api.Documentation;

public static class ApiDocumentationExtensions
{
    private const string Title = "Ripple Ticketing API";

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = Title, Version = "v1" });

            // Honour C# nullable annotations, so `string` is non-nullable and `string?` is nullable.
            c.SupportNonNullableReferenceTypes();

            // Money is decimal; without this Swashbuckle describes it as a double.
            c.MapType<decimal>(() => new OpenApiSchema { Type = JsonSchemaType.Number, Format = "decimal" });
        });
        return services;
    }

    public static IApplicationBuilder UseApiDocumentation(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{Title} v1"));
        return app;
    }
}
