using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Bancada.Tests;

public class QuoteTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    internal static object Item(string price = "19.99", int quantity = 1) =>
        new { description = "Peca ficticia", kind = "part", quantity, unitPrice = price };

    internal static async Task<JsonElement> Diagnose(HttpClient staff, HttpClient tech)
    {
        var order = await OrderTests.Create(staff);
        var response = await tech.PostAsJsonAsync(OrderTests.Path(order, "/diagnosis"),
            new { expectedVersion = 1, diagnosis = "Nota interna: fonte defeituosa" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Quote_total_is_server_calculated_and_revisions_persist()
    {
        using var staff = await fixture.Staff();
        using var tech = await fixture.Staff("tecnico1@bancada.example");
        var order = await Diagnose(staff, tech);
        var response = await tech.PostAsJsonAsync(OrderTests.Path(order, "/quotes"),
            new { expectedVersion = 2, items = new[] { Item("0.10", 3), Item() }, total = "0.01" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var first = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("20.29", first.GetProperty("currentQuote").GetProperty("total").GetString());
        Assert.Equal("awaitingApproval", first.GetProperty("status").GetString());
        var second = await tech.PostAsJsonAsync(OrderTests.Path(order, "/quotes"),
            new { expectedVersion = 3, items = new[] { Item("2.00") } });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var saved = await staff.GetFromJsonAsync<JsonElement>(OrderTests.Path(order));
        Assert.Equal(2, saved.GetProperty("currentQuote").GetProperty("revision").GetInt32());
        Assert.Equal(4, saved.GetProperty("history").GetArrayLength());
    }

    [Theory]
    [InlineData("-1.00", 1)]
    [InlineData("1.001", 1)]
    [InlineData("1000000.00", 1)]
    [InlineData("1,00", 1)]
    [InlineData("1e2", 1)]
    [InlineData("1.00", 0)]
    [InlineData("1.00", 1000)]
    public async Task Quote_invalid_money_or_quantity_is_rejected(string price, int quantity)
    {
        using var staff = await fixture.Staff();
        using var tech = await fixture.Staff("tecnico1@bancada.example");
        var order = await Diagnose(staff, tech);
        Assert.Equal(HttpStatusCode.BadRequest, (await tech.PostAsJsonAsync(OrderTests.Path(order, "/quotes"),
            new { expectedVersion = 2, items = new[] { Item(price, quantity) } })).StatusCode);
        var saved = await staff.GetFromJsonAsync<JsonElement>(OrderTests.Path(order));
        Assert.Equal(2, saved.GetProperty("version").GetInt32());
    }

    [Fact]
    public async Task Quote_requires_diagnosis_items_and_assigned_technician()
    {
        using var staff = await fixture.Staff();
        using var tech = await fixture.Staff("tecnico1@bancada.example");
        using var other = await fixture.Staff("tecnico2@bancada.example");
        var order = await OrderTests.Create(staff);
        Assert.Equal(HttpStatusCode.Conflict, (await tech.PostAsJsonAsync(OrderTests.Path(order, "/quotes"),
            new { expectedVersion = 1, items = new[] { Item() } })).StatusCode);
        order = await Diagnose(staff, tech);
        Assert.Equal(HttpStatusCode.BadRequest, (await tech.PostAsJsonAsync(OrderTests.Path(order, "/quotes"),
            new { expectedVersion = 2, items = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsJsonAsync(OrderTests.Path(order, "/quotes"),
            new { expectedVersion = 2, items = new[] { Item() } })).StatusCode);
    }

    [Fact]
    public async Task Quote_json_types_limits_and_money_boundary()
    {
        using var staff = await fixture.Staff();
        using var tech = await fixture.Staff("tecnico1@bancada.example");
        var order = await Diagnose(staff, tech);
        foreach (var json in new[]
        {
            """{"expectedVersion":2,"items":[{"description":"Teste","kind":"part","quantity":1,"unitPrice":1.00}]}""",
            """{"expectedVersion":2,"items":[{"description":"Teste","kind":"part","quantity":1.5,"unitPrice":"1.00"}]}""",
            """{"expectedVersion":2,"items":[null]}""",
            """{"expectedVersion":2,"items":null}""",
            """{"expectedVersion":2,"items":[{"description":"Teste","kind":"unknown","quantity":1,"unitPrice":"1.00"}]}"""
        })
        {
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await tech.PostAsync(OrderTests.Path(order, "/quotes"), content);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("validation", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await tech.PostAsJsonAsync(OrderTests.Path(order, "/quotes"),
            new { expectedVersion = 2, items = Enumerable.Range(0, 101).Select(_ => Item()).ToArray() })).StatusCode);
        var valid = await tech.PostAsJsonAsync(OrderTests.Path(order, "/quotes"),
            new { expectedVersion = 2, items = Enumerable.Range(0, 100).Select(_ => Item("999999.99", 999)).ToArray() });
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        Assert.Equal("99899999001.00", (await valid.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("currentQuote").GetProperty("total").GetString());
    }
}
