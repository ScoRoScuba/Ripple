namespace Ripple.Api.Endpoints;

/// <summary>
/// One HTTP endpoint. Implementations are discovered and registered automatically by
/// <see cref="EndpointExtensions.AddEndpoints"/>; add a new class, nothing else needs changing.
/// </summary>
public interface IEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
