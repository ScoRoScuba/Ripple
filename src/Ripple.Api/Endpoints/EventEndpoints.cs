using FluentValidation;
using Ripple.Events;

namespace Ripple.Api.Endpoints;

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events").WithTags("Events");

        group.MapGet("/", async (IEventService service) =>
            Results.Ok(await service.GetAllAsync()))
            .WithName("GetAllEvents")
            .WithSummary("List all events");

        group.MapGet("/{id:guid}", async (Guid id, IEventService service) =>
            Results.Ok(await service.GetByIdAsync(id)))
            .WithName("GetEvent")
            .WithSummary("Get event by ID");

        group.MapPost("/", async (CreateEventRequest request, IValidator<CreateEventRequest> validator, IEventService service) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary(), statusCode: StatusCodes.Status422UnprocessableEntity);

            var created = await service.CreateAsync(request);
            return Results.CreatedAtRoute("GetEvent", new { id = created.Id }, created);
        })
        .WithName("CreateEvent")
        .WithSummary("Create a new event");

        group.MapPut("/{id:guid}", async (Guid id, UpdateEventRequest request, IValidator<UpdateEventRequest> validator, IEventService service) =>
        {
            var validation = await validator.ValidateAsync(request);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary(), statusCode: StatusCodes.Status422UnprocessableEntity);

            return Results.Ok(await service.UpdateAsync(id, request));
        })
        .WithName("UpdateEvent")
        .WithSummary("Replace an event");

        group.MapDelete("/{id:guid}", async (Guid id, IEventService service) =>
        {
            await service.DeleteAsync(id);
            return Results.NoContent();
        })
        .WithName("DeleteEvent")
        .WithSummary("Delete an event");

        return app;
    }
}
