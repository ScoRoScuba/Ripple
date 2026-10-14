using Ripple.Events;

namespace Ripple.Api.Endpoints.Events;

public class GetAllEventsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/events", Handle)
            .WithTags("Events")
            .WithName("GetAllEvents")
            .WithSummary("List all events");

    private static async Task<IResult> Handle(IEventService service) =>
        Results.Ok(await service.GetAllAsync());
}
