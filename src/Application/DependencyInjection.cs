using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SADC.Order.Management.Application.Customers;
using SADC.Order.Management.Application.Mappings;
using SADC.Order.Management.Application.Orders;

namespace SADC.Order.Management.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(typeof(MappingProfile).Assembly);

        // FluentValidation — register all validators from this assembly
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Application services
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}
