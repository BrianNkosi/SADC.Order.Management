using FluentAssertions;
using SADC.Order.Management.Domain.Entities;
using SADC.Order.Management.Domain.Enums;
using Xunit;
using OrderEntity = SADC.Order.Management.Domain.Entities.Order;

namespace SADC.Order.Management.Tests.Unit.Domain;

public class OrderEntityTests
{
    [Fact]
    public void RecalculateTotal_SumsLineItems()
    {
        var order = new OrderEntity
        {
            LineItems =
            [
                new OrderLineItem { Quantity = 2, UnitPrice = 10.50m },
                new OrderLineItem { Quantity = 1, UnitPrice = 25.00m },
                new OrderLineItem { Quantity = 3, UnitPrice = 5.00m }
            ]
        };

        order.RecalculateTotal();

        // 2*10.50 + 1*25.00 + 3*5.00 = 21.00 + 25.00 + 15.00 = 61.00
        order.TotalAmount.Should().Be(61.00m);
    }

    [Fact]
    public void TryTransitionTo_ValidTransition_ReturnsTrue()
    {
        var order = new OrderEntity { Status = OrderStatus.Pending };

        var result = order.TryTransitionTo(OrderStatus.Paid);

        result.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);
        order.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void TryTransitionTo_InvalidTransition_ReturnsFalse()
    {
        var order = new OrderEntity { Status = OrderStatus.Fulfilled };

        var result = order.TryTransitionTo(OrderStatus.Pending);

        result.Should().BeFalse();
        order.Status.Should().Be(OrderStatus.Fulfilled);
    }

    [Fact]
    public void OrderLineItem_LineTotal_ComputesCorrectly()
    {
        var lineItem = new OrderLineItem
        {
            Quantity = 3,
            UnitPrice = 15.99m
        };

        lineItem.LineTotal.Should().Be(47.97m);
    }
}
