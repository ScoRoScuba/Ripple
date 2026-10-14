using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ripple.Api.Endpoints;

public static class EndpointExtensions
{
    /// <summary>
    /// Registers every concrete <see cref="IEndpoint"/> in the assembly with DI.
    /// </summary>
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var endpoints = assembly.DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } && type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type));

        services.TryAddEnumerable(endpoints);
        return services;
    }

    /// <summary>
    /// Maps every registered <see cref="IEndpoint"/>.
    /// </summary>
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        foreach (var endpoint in app.ServiceProvider.GetServices<IEndpoint>())
            endpoint.Map(app);

        return app;
    }
}
