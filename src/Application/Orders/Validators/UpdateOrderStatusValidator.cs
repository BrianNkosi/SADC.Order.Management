using FluentValidation;
using SADC.Order.Management.Application.Orders.Commands;

namespace SADC.Order.Management.Application.Orders.Validators;

public class UpdateOrderStatusValidator : AbstractValidator<UpdateOrderStatusCommand>
{
    public UpdateOrderStatusValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required.");

        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("New status is not a valid OrderStatus value.");

        RuleFor(x => x.IdempotencyKey)
            .MaximumLength(64).WithMessage("Idempotency key must not exceed 64 characters.")
            .When(x => x.IdempotencyKey is not null);
    }
}
