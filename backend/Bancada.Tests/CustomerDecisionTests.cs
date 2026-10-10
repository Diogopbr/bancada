using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Bancada.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;
using Bancada.Api.Features.Access;

namespace Bancada.Tests;

public class CustomerDecisionTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private static async Task<string> Grant(HttpClient staff, JsonElement order, int version)
    {
        var response = await staff.PostAsJsonAsync(OrderTests.Path(order, "/customer-access"), new { expectedVersion = version });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var grant = await response.Content.ReadFromJsonAsync<JsonElement>();
        var url = grant.GetProperty("url").GetString()!;
        Assert.StartsWith("/acompanhar#token=", url);
        var token = url.Split("#token=")[1];
        Assert.Equal(43, token.Length);
        return token;
    }

    private static async Task AssertSameUnavailable(HttpResponseMessage expected, HttpResponseMessage actual)
    {
        Assert.Equal(HttpStatusCode.NotFound, actual.StatusCode);
        using var firstDocument = JsonDocument.Parse(await expected.Content.ReadAsStringAsync());
        using var secondDocument = JsonDocument.Parse(await actual.Content.ReadAsStringAsync());
        var first = firstDocument.RootElement;
        var second = secondDocument.RootElement;
        foreach (var field in new[] { "type", "title", "status", "code" })
            Assert.Equal(first.GetProperty(field).ToString(), second.GetProperty(field).ToString());
        Assert.False(second.TryGetProperty("detail", out _));
    }

    private async Task<(JsonElement order, HttpClient staff, HttpClient tech, string token)> Prepared()
    {
        var staff = await fixture.Staff();
        var tech = await fixture.Staff("tecnico1@bancada.example");
        var order = await QuoteTests.Diagnose(staff, tech);
        var response = await tech.PostAsJsonAsync(OrderTests.Path(order, "/quotes"),
            new { expectedVersion = 2, items = new[] { QuoteTests.Item() } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (order, staff, tech, await Grant(staff, order, 3));
    }

    private async Task<HttpClient> Customer(string token)
    {
        var client = fixture.Client();
        await ApiFixture.Csrf(client);
        var response = await client.PostAsJsonAsync("/api/customer/session", new { token });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        await ApiFixture.Csrf(client);
        return client;
    }

    [Theory]
    [InlineData("accepted", "approved")]
    [InlineData("rejected", "diagnosing")]
    public async Task Customer_decision_is_idempotent_and_public(string decision, string status)
    {
        var setup = await Prepared();
        using var staff = setup.staff;
        using var tech = setup.tech;
        using var customer = await Customer(setup.token);
        var request = new { quoteRevision = 1, decision };
        var first = await customer.PostAsJsonAsync("/api/customer/decision", request);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var result = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(status, result.GetProperty("status").GetString());
        Assert.Equal(5, result.GetProperty("version").GetInt32());
        Assert.Equal(new[] { "code", "currentQuote", "equipmentLabel", "history", "status", "version" },
            result.EnumerateObject().Select(x => x.Name).Order().ToArray());
        Assert.DoesNotContain("Nota interna", result.ToString());
        Assert.DoesNotContain(setup.token, result.ToString());
        var repeat = await customer.PostAsJsonAsync("/api/customer/decision", request);
        Assert.Equal(HttpStatusCode.OK, repeat.StatusCode);
        Assert.Equal(result.ToString(), (await repeat.Content.ReadFromJsonAsync<JsonElement>()).ToString());
        Assert.Equal(HttpStatusCode.Conflict, (await customer.PostAsJsonAsync("/api/customer/decision",
            new { quoteRevision = 1, decision = decision == "accepted" ? "rejected" : "accepted" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await customer.GetAsync(OrderTests.Path(setup.order))).StatusCode);
    }

    [Fact]
    public async Task Customer_opposite_concurrent_decisions_commit_one_event()
    {
        var setup = await Prepared();
        using var staff = setup.staff;
        using var tech = setup.tech;
        using var first = await Customer(setup.token);
        using var second = await Customer(setup.token);
        var responses = await Task.WhenAll(
            first.PostAsJsonAsync("/api/customer/decision", new { quoteRevision = 1, decision = "accepted" }),
            second.PostAsJsonAsync("/api/customer/decision", new { quoteRevision = 1, decision = "rejected" }));
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Conflict);
        var saved = await staff.GetFromJsonAsync<JsonElement>(OrderTests.Path(setup.order));
        Assert.Equal(5, saved.GetProperty("version").GetInt32());
        Assert.Equal(4, saved.GetProperty("history").GetArrayLength());
    }

    [Fact]
    public async Task Customer_new_revision_invalidates_approval_and_old_decision()
    {
        var setup = await Prepared();
        using var staff = setup.staff;
        using var tech = setup.tech;
        using var customer = await Customer(setup.token);
        Assert.Equal(HttpStatusCode.OK, (await customer.PostAsJsonAsync("/api/customer/decision",
            new { quoteRevision = 1, decision = "accepted" })).StatusCode);
        var publish = await tech.PostAsJsonAsync(OrderTests.Path(setup.order, "/quotes"),
            new { expectedVersion = 5, items = new[] { QuoteTests.Item("2.00") } });
        Assert.Equal(HttpStatusCode.OK, publish.StatusCode);
        var current = await publish.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("awaitingApproval", current.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, current.GetProperty("currentQuote").GetProperty("decision").ValueKind);
        Assert.Equal(HttpStatusCode.Conflict, (await customer.PostAsJsonAsync("/api/customer/decision",
            new { quoteRevision = 1, decision = "accepted" })).StatusCode);
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BancadaDb>();
        var quotes = await db.Quotes.Include(q => q.Items).Where(q => q.OrderId == setup.order.GetProperty("id").GetGuid())
            .OrderBy(q => q.Revision).ToListAsync();
        Assert.Equal("accepted", quotes[0].Decision);
        Assert.Equal(19.99m, quotes[0].Items[0].UnitPrice);
        Assert.Null(quotes[1].Decision);
    }

    [Fact]
    public async Task Customer_repair_delivery_and_terminal_guards()
    {
        var setup = await Prepared();
        using var staff = setup.staff;
        using var tech = setup.tech;
        using var customer = await Customer(setup.token);
        using var other = await fixture.Staff("tecnico2@bancada.example");
        await customer.PostAsJsonAsync("/api/customer/decision", new { quoteRevision = 1, decision = "accepted" });
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsJsonAsync(OrderTests.Path(setup.order, "/transitions"),
            new { expectedVersion = 5, target = "repairing" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await tech.PostAsJsonAsync(OrderTests.Path(setup.order, "/transitions"),
            new { expectedVersion = 5, target = "repairing" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await tech.PostAsJsonAsync(OrderTests.Path(setup.order, "/quotes"),
            new { expectedVersion = 6, items = new[] { QuoteTests.Item() } })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await staff.PostAsJsonAsync(OrderTests.Path(setup.order, "/transitions"),
            new { expectedVersion = 6, target = "cancelled", reason = "Desistiu" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await tech.PostAsJsonAsync(OrderTests.Path(setup.order, "/transitions"),
            new { expectedVersion = 6, target = "ready" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await tech.PostAsJsonAsync(OrderTests.Path(setup.order, "/transitions"),
            new { expectedVersion = 7, target = "delivered" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await staff.PostAsJsonAsync(OrderTests.Path(setup.order, "/transitions"),
            new { expectedVersion = 7, target = "delivered" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await staff.PostAsJsonAsync(OrderTests.Path(setup.order, "/transitions"),
            new { expectedVersion = 8, target = "cancelled", reason = "Desistiu" })).StatusCode);
    }

    [Fact]
    public async Task Customer_invalid_revoked_expired_and_replaced_access_is_generic()
    {
        var setup = await Prepared();
        using var staff = setup.staff;
        using var tech = setup.tech;
        using var customer = await Customer(setup.token);
        Assert.Equal(HttpStatusCode.NoContent, (await staff.DeleteAsync(OrderTests.Path(setup.order, "/customer-access"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await customer.GetAsync("/api/customer/order")).StatusCode);
        using var anonymous = fixture.Client();
        await ApiFixture.Csrf(anonymous);
        var revoked = await anonymous.PostAsJsonAsync("/api/customer/session", new { token = setup.token });
        var invalid = await anonymous.PostAsJsonAsync("/api/customer/session", new { token = new string('x', 43) });
        Assert.Equal(HttpStatusCode.NotFound, revoked.StatusCode);
        await AssertSameUnavailable(revoked, invalid);
        var fresh = await Grant(staff, setup.order, 5);
        using var expiring = await Customer(fresh);
        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BancadaDb>();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"CustomerAccesses\" SET \"ExpiresAt\" = {DateTimeOffset.UtcNow.AddMinutes(-1)} WHERE \"OrderId\" = {setup.order.GetProperty("id").GetGuid()}");
        }
        Assert.Equal(HttpStatusCode.NotFound, (await expiring.GetAsync("/api/customer/order")).StatusCode);
        var expired = await anonymous.PostAsJsonAsync("/api/customer/session", new { token = fresh });
        await AssertSameUnavailable(invalid, expired);
        var replacement = await Grant(staff, setup.order, 6);
        using var replaced = await Customer(replacement);
        await Grant(staff, setup.order, 7);
        Assert.Equal(HttpStatusCode.NotFound, (await replaced.GetAsync("/api/customer/order")).StatusCode);
    }

    [Fact]
    public async Task Customer_rate_limit_rejects_twenty_first_request()
    {
        using var client = fixture.Client();
        await ApiFixture.Csrf(client);
        for (var i = 0; i < 20; i++)
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/customer/order")).StatusCode);
        var response = await client.GetAsync("/api/customer/order");
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("rateLimited", (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Customer_writes_require_csrf_and_access_requires_receptionist_current_version()
    {
        var setup = await Prepared();
        using var staff = setup.staff;
        using var tech = setup.tech;
        Assert.Equal(HttpStatusCode.Forbidden, (await tech.PostAsJsonAsync(OrderTests.Path(setup.order, "/customer-access"),
            new { expectedVersion = 4 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await staff.PostAsJsonAsync(OrderTests.Path(setup.order, "/customer-access"),
            new { expectedVersion = 3 })).StatusCode);
        using var customer = fixture.Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await customer.PostAsJsonAsync("/api/customer/session",
            new { token = setup.token })).StatusCode);
        using var loggedIn = await Customer(setup.token);
        loggedIn.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await loggedIn.PostAsJsonAsync("/api/customer/decision",
            new { quoteRevision = 1, decision = "accepted" })).StatusCode);
    }

    [Fact]
    public async Task Customer_token_is_hashed_cookie_is_bounded_and_logs_are_safe()
    {
        var setup = await Prepared();
        using var staff = setup.staff;
        using var tech = setup.tech;
        using var customer = fixture.Client();
        await ApiFixture.Csrf(customer);
        var response = await customer.PostAsJsonAsync("/api/customer/session", new { token = setup.token });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("bancada.customer=")).Split(';')[0];
        var options = fixture.Factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CustomerAccessService.Scheme);
        var ticket = options.TicketDataFormat.Unprotect(cookie["bancada.customer=".Length..]);
        Assert.NotNull(ticket);
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BancadaDb>();
        var grant = await db.CustomerAccesses.SingleAsync(a => a.OrderId == setup.order.GetProperty("id").GetGuid());
        Assert.Equal(CustomerAccess.Hash(setup.token), grant.TokenHash);
        Assert.NotEqual(setup.token, grant.TokenHash);
        Assert.True(ticket.Properties.ExpiresUtc <= grant.ExpiresAt);
        Assert.InRange((grant.ExpiresAt - DateTimeOffset.UtcNow).TotalDays, 6.99, 7);
        Assert.All(fixture.Logs.Messages, message =>
        {
            Assert.DoesNotContain(setup.token, message);
            Assert.DoesNotContain(ApiFixture.Password, message);
            Assert.DoesNotContain(cookie, message);
        });
    }

    [Fact]
    public async Task Customer_order_is_isolated_and_quote_history_survive_new_host()
    {
        var setup = await Prepared();
        using var staff = setup.staff;
        using var tech = setup.tech;
        using var customer = await Customer(setup.token);
        var other = await OrderTests.Create(staff);
        var publicOrder = await customer.GetFromJsonAsync<JsonElement>($"/api/customer/order?orderId={other.GetProperty("id").GetGuid()}");
        Assert.Equal(setup.order.GetProperty("code").GetString(), publicOrder.GetProperty("code").GetString());
        using var restarted = new ApiFactory(fixture.Connection);
        using var newClient = restarted.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await ApiFixture.Csrf(newClient);
        Assert.Equal(HttpStatusCode.NoContent, (await newClient.PostAsJsonAsync("/api/customer/session", new { token = setup.token })).StatusCode);
        var persisted = await newClient.GetFromJsonAsync<JsonElement>("/api/customer/order");
        Assert.Equal(publicOrder.ToString(), persisted.ToString());
    }
}
