using FluentValidation;

namespace Ripple.Api.Validators;

public static class ValidatorExtensions
{
    /// <summary>
    /// Registers every FluentValidation validator in the API assembly.
    /// </summary>
    public static IServiceCollection AddValidators(this IServiceCollection services) =>
        services.AddValidatorsFromAssemblyContaining<CreateEventRequestValidator>();
}
