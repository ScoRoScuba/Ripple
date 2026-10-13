using FluentValidation;
using Ripple.Events;

namespace Ripple.Api.Validators;

public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Venue).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Date).GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Event date must not be in the past.");
        RuleFor(x => x.TotalCapacity).GreaterThan(0);
        RuleFor(x => x.PricingTiers).NotEmpty().WithMessage("At least one pricing tier is required.");
        RuleForEach(x => x.PricingTiers).ChildRules(tier =>
        {
            tier.RuleFor(t => t.Name).NotEmpty().MaximumLength(255);
            tier.RuleFor(t => t.Price).GreaterThanOrEqualTo(0m);
            tier.RuleFor(t => t.Capacity).GreaterThan(0);
        });
    }
}

public class UpdateEventRequestValidator : AbstractValidator<UpdateEventRequest>
{
    public UpdateEventRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Venue).NotEmpty().MaximumLength(255);
        RuleFor(x => x.TotalCapacity).GreaterThan(0);
        RuleFor(x => x.PricingTiers).NotEmpty().WithMessage("At least one pricing tier is required.");
        RuleForEach(x => x.PricingTiers).ChildRules(tier =>
        {
            tier.RuleFor(t => t.Name).NotEmpty().MaximumLength(255);
            tier.RuleFor(t => t.Price).GreaterThanOrEqualTo(0m);
            tier.RuleFor(t => t.Capacity).GreaterThan(0);
        });
    }
}
