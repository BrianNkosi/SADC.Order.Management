namespace SADC.Order.Management.Application.Customers.DTOs;

public record CreateCustomerRequest(
    string Name,
    string Email,
    string CountryCode);
