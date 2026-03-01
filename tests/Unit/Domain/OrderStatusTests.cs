using FluentAssertions;
using SADC.Order.Management.Domain.Enums;
using Xunit;

namespace SADC.Order.Management.Tests.Unit.Domain;

public class OrderStatusTests
{
    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Paid, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Paid, OrderStatus.Fulfilled, true)]
    [InlineData(OrderStatus.Paid, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Fulfilled, false)]
    [InlineData(OrderStatus.Fulfilled, OrderStatus.Pending, false)]
    [InlineData(OrderStatus.Fulfilled, OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Paid, false)]
    public void CanTransitionTo_ReturnsExpectedResult(OrderStatus from, OrderStatus to, bool expected)
    {
        from.CanTransitionTo(to).Should().Be(expected);
    }

    [Fact]
    public void AllowedTransitions_FromPending_ReturnsPaidAndCancelled()
    {
        var allowed = OrderStatus.Pending.AllowedTransitions();
        allowed.Should().BeEquivalentTo([OrderStatus.Paid, OrderStatus.Cancelled]);
    }

    [Fact]
    public void AllowedTransitions_FromFulfilled_ReturnsEmpty()
    {
        OrderStatus.Fulfilled.AllowedTransitions().Should().BeEmpty();
    }
}
