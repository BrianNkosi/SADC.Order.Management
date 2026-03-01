using AutoMapper;
using SADC.Order.Management.Application.Customers.DTOs;
using SADC.Order.Management.Application.Orders.DTOs;
using SADC.Order.Management.Domain.Entities;
using OrderEntity = SADC.Order.Management.Domain.Entities.Order;

namespace SADC.Order.Management.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Customer
        CreateMap<Customer, CustomerDto>();

        // Order → OrderDto
        CreateMap<OrderEntity, OrderDto>()
            .ForCtorParam("CustomerName", opt => opt.MapFrom(src => src.Customer.Name))
            .ForCtorParam("LineItems", opt => opt.MapFrom(src => src.LineItems));

        // Order → OrderSummaryDto (list view, no line items)
        CreateMap<OrderEntity, OrderSummaryDto>()
            .ForCtorParam("CustomerName", opt => opt.MapFrom(src => src.Customer.Name))
            .ForCtorParam("LineItemCount", opt => opt.MapFrom(src => src.LineItems.Count));

        // OrderLineItem → OrderLineItemDto
        CreateMap<OrderLineItem, OrderLineItemDto>()
            .ForCtorParam("LineTotal", opt => opt.MapFrom(src => src.Quantity * src.UnitPrice));
    }
}
