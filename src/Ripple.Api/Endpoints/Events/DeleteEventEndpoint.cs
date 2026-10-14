using Ripple.Events;

namespace Ripple.Api.Endpoints.Events;

public class DeleteEventEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/events/{id:guid}", Handle)
            .WithTags("Events")
            .WithName("DeleteEvent")
            .WithSummary("Delete an event");

    private static async Task<IResult> Handle(Guid id, IEventService service)
    {
        await service.DeleteAsync(id);
        return Results.NoContent();
    }
}
