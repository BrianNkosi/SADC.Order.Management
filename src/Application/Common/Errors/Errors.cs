namespace SADC.Order.Management.Application.Common.Errors;

/// <summary>
/// Static factory methods for domain-specific errors, grouped by domain area.
/// </summary>
public static class Errors
{
    public static class General
    {
        public static Error NotFound(string entity, object id) =>
            new("record.not.found", $"{entity} with identifier '{id}' was not found.");

        public static Error Validation(string message) =>
            new("validation.error", message);

        public static Error InternalError(string message) =>
            new("internal.error", message);
    }

    public static class Customers
    {
        public static Error NotFound(Guid id) =>
            General.NotFound("Customer", id);
    }

    public static class Orders
    {
        public static Error NotFound(Guid id) =>
            General.NotFound("Order", id);

        public static Error InvalidCurrency(string currencyCode, string countryCode, IEnumerable<string> validCurrencies) =>
            new("order.invalid.currency",
                $"Currency '{currencyCode}' is not valid for country '{countryCode}'. " +
                $"Valid currencies: {string.Join(", ", validCurrencies)}.");

        public static Error InvalidStatusTransition(string currentStatus, string targetStatus, IEnumerable<string> allowedTransitions) =>
            new("order.invalid.status.transition",
                $"Cannot transition order from '{currentStatus}' to '{targetStatus}'. " +
                $"Allowed transitions: {string.Join(", ", allowedTransitions)}.");

        public static Error CustomerNotFound(Guid customerId) =>
            new("order.customer.not.found", $"Customer '{customerId}' not found.");
    }
}
