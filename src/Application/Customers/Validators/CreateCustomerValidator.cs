using FluentValidation;
using SADC.Order.Management.Application.Customers.Commands;
using SADC.Order.Management.Domain.ValueObjects;

namespace SADC.Order.Management.Application.Customers.Validators;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Customer name is required.")
            .MaximumLength(200).WithMessage("Customer name must not exceed 200 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.CountryCode)
            .NotEmpty().WithMessage("Country code is required.")
            .Length(2).WithMessage("Country code must be a 2-letter ISO 3166-1 alpha-2 code.")
            .Must(SadcCountryCurrency.IsValidCountry)
            .WithMessage(x => $"'{x.CountryCode}' is not a valid SADC member country. Valid codes: {string.Join(", ", SadcCountryCurrency.ValidCountryCodes)}.");
    }
}
