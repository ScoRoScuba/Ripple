using FluentValidation;
using Ripple.Tickets;

namespace Ripple.Api.Validators;

public class PurchaseTicketRequestValidator : AbstractValidator<PurchaseTicketRequest>
{
    public PurchaseTicketRequestValidator()
    {
        RuleFor(x => x.PricingTierId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(100)
            .WithMessage("Quantity must be between 1 and 100.");
        RuleFor(x => x.PurchaserName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.PurchaserEmail).NotEmpty().EmailAddress().MaximumLength(255);
    }
}
