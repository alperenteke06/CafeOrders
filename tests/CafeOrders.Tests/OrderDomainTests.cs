using CafeOrders.Domain.Entities;
using CafeOrders.Domain.Enums;

namespace CafeOrders.Tests;

public sealed class OrderDomainTests
{
    [Fact]
    public void RecalculateTotal_SumsLineTotals()
    {
        var order = new Order
        {
            OrderLines =
            [
                new OrderLine { Quantity = 1, UnitPrice = 30m, LineTotal = 30m },
                new OrderLine { Quantity = 2, UnitPrice = 15m, LineTotal = 30m }
            ]
        };

        order.RecalculateTotal();

        Assert.Equal(60m, order.TotalPrice);
    }

    [Fact]
    public void InfoMessage_IsCurrentlyActive_RespectsDateWindow()
    {
        var now = DateTime.UtcNow;
        var message = new InfoMessage
        {
            IsActive = true,
            Type = InfoMessageType.Warning,
            StartDate = now.AddMinutes(-10),
            EndDate = now.AddMinutes(10)
        };

        Assert.True(message.IsCurrentlyActive(now));
        Assert.False(message.IsCurrentlyActive(now.AddHours(2)));
    }
}
