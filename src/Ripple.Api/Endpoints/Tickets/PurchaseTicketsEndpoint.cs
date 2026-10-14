using FluentValidation;
using Ripple.Tickets;
using Ripple.Api.Versioning;

namespace Ripple.Api.Endpoints.Tickets;

public class PurchaseTicketsEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("events/{eventId:guid}/tickets", Handle)
            .MapToApiVersion(ApiVersions.V1)
            .WithTags("Tickets")
            .WithName("PurchaseTickets")
            .WithSummary("Purchase tickets for an event");

    private static async Task<IResult> Handle(
        Guid eventId,
        PurchaseTicketRequest request,
        IValidator<PurchaseTicketRequest> validator,
        ITicketService service)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary(), statusCode: StatusCodes.Status422UnprocessableEntity);

        var result = await service.PurchaseAsync(eventId, request);
        return Results.Created($"/api/v1/events/{eventId}/tickets/{result.TicketId}", result);
    }
}
