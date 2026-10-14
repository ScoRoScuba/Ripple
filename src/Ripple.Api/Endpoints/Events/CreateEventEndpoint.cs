using FluentValidation;
using Ripple.Events;
using Ripple.Api.Versioning;

namespace Ripple.Api.Endpoints.Events;

public class CreateEventEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost("events", Handle)
            .MapToApiVersion(ApiVersions.V1)
            .WithTags("Events")
            .WithName("CreateEvent")
            .WithSummary("Create a new event");

    private static async Task<IResult> Handle(
        CreateEventRequest request,
        IValidator<CreateEventRequest> validator,
        IEventService service)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary(), statusCode: StatusCodes.Status422UnprocessableEntity);

        var created = await service.CreateAsync(request);
        return Results.CreatedAtRoute(GetEventEndpoint.Name, new { id = created.Id }, created);
    }
}
