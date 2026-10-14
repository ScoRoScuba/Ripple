using Ripple.Tickets;

namespace Ripple.Api.Endpoints.Tickets;

public class GetAvailabilityEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/events/{eventId:guid}/availability", Handle)
            .WithTags("Tickets")
            .WithName("GetAvailability")
            .WithSummary("Get ticket availability for an event");

    private static async Task<IResult> Handle(Guid eventId, ITicketService service) =>
        Results.Ok(await service.GetAvailabilityAsync(eventId));
}
