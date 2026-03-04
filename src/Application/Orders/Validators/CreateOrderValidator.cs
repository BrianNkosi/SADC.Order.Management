using FluentValidation;
using SADC.Order.Management.Application.Orders.Commands;
using SADC.Order.Management.Domain.ValueObjects;

namespace SADC.Order.Management.Application.Orders.Validators;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Currency code is required.")
            .Length(3).WithMessage("Currency code must be a 3-letter ISO 4217 code.")
            .Must(code => SadcCountryCurrency.AllCurrencies.Contains(code.ToUpperInvariant()))
            .WithMessage(x => $"'{x.CurrencyCode}' is not a valid SADC currency.");

        RuleFor(x => x.LineItems)
            .NotEmpty().WithMessage("At least one line item is required.");

        RuleForEach(x => x.LineItems).ChildRules(li =>
        {
            // li.RuleFor(x => x.ProductSku)
            //     .NotEmpty().WithMessage("Product SKU is required.")
            //     .MaximumLength(100).WithMessage("Product SKU must not exceed 100 characters.");

            li.RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0.");

            li.RuleFor(x => x.UnitPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Unit price must be ≥ 0.");
        });
    }
}
