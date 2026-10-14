using FluentValidation;
using Ripple.Events;

namespace Ripple.Api.Endpoints.Events;

public class UpdateEventEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/events/{id:guid}", Handle)
            .WithTags("Events")
            .WithName("UpdateEvent")
            .WithSummary("Replace an event");

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
