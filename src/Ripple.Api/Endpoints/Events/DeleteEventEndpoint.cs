using Ripple.Events;
using Ripple.Api.Versioning;

namespace Ripple.Api.Endpoints.Events;

public class DeleteEventEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("events/{id:guid}", Handle)
            .MapToApiVersion(ApiVersions.V1)
            .WithTags("Events")
            .WithName("DeleteEvent")
            .WithSummary("Delete an event");

    private static async Task<IResult> Handle(Guid id, IEventService service)
    {
        await service.DeleteAsync(id);
        return Results.NoContent();
    }
}
