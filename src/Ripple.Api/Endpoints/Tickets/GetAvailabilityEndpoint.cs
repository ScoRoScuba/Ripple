using Ripple.Tickets;
using Ripple.Api.Versioning;

namespace Ripple.Api.Endpoints.Tickets;

public class GetAvailabilityEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("events/{eventId:guid}/availability", Handle)
            .MapToApiVersion(ApiVersions.V1)
            .WithTags("Tickets")
            .WithName("GetAvailability")
            .WithSummary("Get ticket availability for an event")
            .Produces<EventAvailabilityResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> Handle(Guid eventId, ITicketService service) =>
        Results.Ok(await service.GetAvailabilityAsync(eventId));
}
