using Ripple.Events;
using Ripple.Api.Versioning;

namespace Ripple.Api.Endpoints.Events;

public class GetAllEventsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("events", Handle)
            .MapToApiVersion(ApiVersions.V1)
            .WithTags("Events")
            .WithName("GetAllEvents")
            .WithSummary("List all events")
            .Produces<IEnumerable<EventResponse>>();

    private static async Task<IResult> Handle(IEventService service) =>
        Results.Ok(await service.GetAllAsync());
}
