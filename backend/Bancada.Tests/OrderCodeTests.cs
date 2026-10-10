using System.Text.Json;
using System.Net.Http.Json;

namespace Bancada.Tests;

public sealed class OrderCodeTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Concurrent_orders_receive_short_distinct_persisted_reference_numbers()
    {
        using var staff = await fixture.Staff();
        var orders = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => OrderTests.Create(staff)));
        var codes = orders.Select(o => o.GetProperty("code").GetString()!).ToArray();
        Assert.Equal(4, codes.Distinct().Count());
        foreach (var order in orders)
        {
            Assert.Matches("^BC-[0-9]{5,}$", order.GetProperty("code").GetString()!);
            var persisted = await staff.GetFromJsonAsync<JsonElement>(OrderTests.Path(order));
            Assert.Equal(order.GetProperty("code").GetString(), persisted.GetProperty("code").GetString());
        }
    }
}
