using Ripple.Events;

namespace Ripple.Api.Endpoints.Events;

public class GetEventEndpoint : IEndpoint
{
    public const string Name = "GetEvent";

    public void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/events/{id:guid}", Handle)
            .WithTags("Events")
            .WithName(Name)
            .WithSummary("Get event by ID");

    private static async Task<IResult> Handle(Guid id, IEventService service) =>
        Results.Ok(await service.GetByIdAsync(id));
}
