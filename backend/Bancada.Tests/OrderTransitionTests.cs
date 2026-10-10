using Bancada.Api;
using Bancada.Api.Features.Orders;
using Bancada.Api.Features.Quotes;

namespace Bancada.Tests;

public class OrderTransitionTests
{
    [Theory]
    [InlineData("received", "diagnosing", "technician", null, 0)]
    [InlineData("ready", "delivered", "receptionist", null, 0)]
    [InlineData("ready", "delivered", "technician", null, 403)]
    [InlineData("approved", "repairing", "receptionist", null, 403)]
    [InlineData("repairing", "ready", "technician", null, 0)]
    [InlineData("repairing", "cancelled", "receptionist", "Motivo", 409)]
    [InlineData("received", "cancelled", "receptionist", "", 400)]
    [InlineData("received", "cancelled", "technician", "Motivo", 403)]
    [InlineData("delivered", "diagnosing", "technician", null, 409)]
    [InlineData("cancelled", "diagnosing", "technician", null, 409)]
    public void Order_transition_matrix(string status, string target, string role, string? reason, int error)
    {
        var order = new Order { Status = status, TechnicianId = OrderTests.Tech1 };
        if (error > 0)
        {
            Assert.Equal(error, Assert.Throws<ApiError>(() => order.ApplyTransition(OrderTests.Tech1, role,
                target, 1, reason, "Ator", DateTimeOffset.UtcNow)).Status);
            Assert.Equal(1, order.Version);
            Assert.Empty(order.History);
        }
        else
        {
            order.ApplyTransition(OrderTests.Tech1, role, target, 1, reason, "Ator", DateTimeOffset.UtcNow);
            Assert.Equal(target, order.Status);
            Assert.Equal(2, order.Version);
            Assert.Single(order.History);
        }
    }

    [Fact]
    public void Order_repair_requires_current_accepted_quote_and_assigned_technician()
    {
        var order = new Order { Status = "approved", TechnicianId = OrderTests.Tech1 };
        Assert.Equal(409, Assert.Throws<ApiError>(() => order.ApplyTransition(OrderTests.Tech1, "technician",
            "repairing", 1, null, "Tecnico", DateTimeOffset.UtcNow)).Status);
        order.Quotes.Add(new Quote { Revision = 1, Decision = "accepted" });
        Assert.Equal(403, Assert.Throws<ApiError>(() => order.ApplyTransition("another", "technician",
            "repairing", 1, null, "Tecnico", DateTimeOffset.UtcNow)).Status);
        order.ApplyTransition(OrderTests.Tech1, "technician", "repairing", 1, null, "Tecnico", DateTimeOffset.UtcNow);
        Assert.Equal("repairing", order.Status);
    }
}
