namespace SADC.Order.Management.Application.Customers.DTOs;

public record CustomerDto(
    Guid Id,
    string Name,
    string Email,
    string CountryCode,
    DateTime CreatedAtUtc);
