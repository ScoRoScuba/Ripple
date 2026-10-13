using FluentValidation;
using Ripple.Tickets;

namespace Ripple.Api.Endpoints;

public static class TicketEndpoints
{
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events/{eventId:guid}").WithTags("Tickets");

        group.MapGet("/availability", async (Guid eventId, ITicketService service) =>
            Results.Ok(await service.GetAvailabilityAsync(eventId)))
            .WithName("GetAvailability")
            .WithSummary("Get ticket availability for an event");

        group.MapPost("/tickets", async (
            Guid eventId,
            PurchaseTicketRequest request,
            IValidator<PurchaseTicketRequest> validator,
            ITicketService service) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary(), statusCode: StatusCodes.Status422UnprocessableEntity);

            var result = await service.PurchaseAsync(eventId, request);
            return Results.Created($"/api/events/{eventId}/tickets/{result.TicketId}", result);
        })
        .WithName("PurchaseTickets")
        .WithSummary("Purchase tickets for an event");

        return app;
    }
}
