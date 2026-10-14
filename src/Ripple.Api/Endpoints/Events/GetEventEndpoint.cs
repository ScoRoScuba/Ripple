using Ripple.Events;
using Ripple.Api.Versioning;

namespace Ripple.Api.Endpoints.Events;

public class GetEventEndpoint : IEndpoint
{
    public const string Name = "GetEvent";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("events/{id:guid}", Handle)
            .MapToApiVersion(ApiVersions.V1)
            .WithTags("Events")
            .WithName(Name)
            .WithSummary("Get event by ID")
            .Produces<EventResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> Handle(Guid id, IEventService service) =>
        Results.Ok(await service.GetByIdAsync(id));
}
