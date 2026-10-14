using FluentValidation;
using Ripple.Events;
using Ripple.Api.Versioning;

namespace Ripple.Api.Endpoints.Events;

public class UpdateEventEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("events/{id:guid}", Handle)
            .MapToApiVersion(ApiVersions.V1)
            .WithTags("Events")
            .WithName("UpdateEvent")
            .WithSummary("Replace an event")
            .Produces<EventResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);

    private static async Task<IResult> Handle(
        Guid id,
        UpdateEventRequest request,
        IValidator<UpdateEventRequest> validator,
        IEventService service)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary(), statusCode: StatusCodes.Status422UnprocessableEntity);

        return Results.Ok(await service.UpdateAsync(id, request));
    }
}
