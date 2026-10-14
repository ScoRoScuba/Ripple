namespace Ripple.Api.Endpoints;

/// <summary>
/// One HTTP endpoint. Implementations are discovered and registered automatically by
/// <see cref="EndpointExtensions.AddEndpoints"/>; add a new class, nothing else needs changing.
/// </summary>
public interface IEndpoint
{
    /// <summary>
    /// Maps the endpoint. <paramref name="app"/> is the versioned <c>/api/v{version}</c> group, so map a
    /// route relative to it (e.g. <c>"events/{id:guid}"</c>) and declare its version with <c>MapToApiVersion</c>.
    /// </summary>
    void Map(IEndpointRouteBuilder app);
}
