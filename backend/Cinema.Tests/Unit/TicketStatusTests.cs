using Cinema.Domain.Enums;
using Xunit;

namespace Cinema.Tests.Unit;

public class TicketStatusTests
{
    [Fact]
    public void TicketStatus_HasUsedValue() =>
        Assert.Equal(3, (int)TicketStatus.Used);

    [Fact]
    public void TicketStatus_HasRefundedValue() =>
        Assert.Equal(4, (int)TicketStatus.Refunded);

    [Fact]
    public void TicketStatus_HasNotUsedValue() =>
        Assert.Equal(5, (int)TicketStatus.NotUsed);
}
