using System.Reflection;
using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ripple.Api.Versioning;

namespace Ripple.Api.Endpoints;

public static class EndpointExtensions
{
    private const string VersionedRoutePrefix = "api/v{version:apiVersion}";

    /// <summary>
    /// Registers every concrete <see cref="IEndpoint"/> in the assembly with DI, and makes malformed
    /// requests throw so they are reported as problem details.
    /// </summary>
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var endpoints = assembly.DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } && type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type));

        services.TryAddEnumerable(endpoints);

        // Minimal APIs only throw on a malformed request (e.g. an unreadable JSON body) in Development;
        // elsewhere they return an empty 400. Always throw, so ExceptionHandlingMiddleware turns it into
        // the same problem-details 400 in every environment.
        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
        return services;
    }

    /// <summary>
    /// Maps every registered <see cref="IEndpoint"/> under the versioned route group
    /// <c>/api/v{version}</c>. Requires <see cref="ApiVersioningExtensions.AddVersioning"/>.
    /// </summary>
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet();
        foreach (var version in ApiVersions.All)
            versionSet.HasApiVersion(version);

        var versionedApi = app.MapGroup(VersionedRoutePrefix)
            .WithApiVersionSet(versionSet.ReportApiVersions().Build());

        foreach (var endpoint in app.ServiceProvider.GetServices<IEndpoint>())
            endpoint.Map(versionedApi);

        return app;
    }
}
