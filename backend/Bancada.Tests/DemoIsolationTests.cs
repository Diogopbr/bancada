using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace Bancada.Tests;

public class DemoIsolationTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private ApiFactory Factory() => new(fixture.Connection, demo: true);
    [Fact]
    public async Task Demo_examples_include_superseded_revision_and_completed_delivery()
    {
        using var factory = Factory();
        using var client = Client(factory);
        await Start(client);
        var list = await Orders(client);
        Assert.Equal(5, list.GetProperty("total").GetInt32());
        var items = list.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(5, items.Select(i => i.GetProperty("equipmentLabel").GetString()).Distinct().Count());
        var revised = items.Single(i => i.GetProperty("equipmentLabel").GetString() == "Console — revisão de orçamento");
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/orders/{revised.GetProperty("id").GetGuid()}");
        Assert.Equal("awaitingApproval", detail.GetProperty("status").GetString());
        Assert.Equal(2, detail.GetProperty("currentQuote").GetProperty("revision").GetInt32());
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("currentQuote").GetProperty("decision").ValueKind);
        Assert.Contains(detail.GetProperty("history").EnumerateArray(), e => e.GetProperty("action").GetString() == "quoteAccepted");
        var delivered = items.Single(i => i.GetProperty("status").GetString() == "delivered");
        var finished = await client.GetFromJsonAsync<JsonElement>($"/api/orders/{delivered.GetProperty("id").GetGuid()}");
        Assert.Equal("accepted", finished.GetProperty("currentQuote").GetProperty("decision").GetString());
        Assert.Equal("delivered", finished.GetProperty("history").EnumerateArray().Last().GetProperty("action").GetString());
    }
    private static HttpClient Client(ApiFactory factory) => factory.CreateClient(
        new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    private static async Task<JsonElement> Start(HttpClient client)
    {
        await ApiFixture.Csrf(client);
        var response = await client.PostAsJsonAsync("/api/demo/start", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await ApiFixture.Csrf(client);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> Orders(HttpClient client) =>
        await client.GetFromJsonAsync<JsonElement>("/api/orders");

    private static async Task<JsonElement> Role(HttpClient client, string role)
    {
        await ApiFixture.Csrf(client);
        var response = await client.PostAsJsonAsync("/api/demo/role", new { role });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await ApiFixture.Csrf(client);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Demo_is_opt_in_and_refuses_global_password_login()
    {
        using var normal = fixture.Client();
        Assert.False((await normal.GetFromJsonAsync<JsonElement>("/api/demo")).GetProperty("enabled").GetBoolean());
        await ApiFixture.Csrf(normal);
        Assert.Equal(HttpStatusCode.NotFound, (await normal.PostAsJsonAsync("/api/demo/start", new { })).StatusCode);
        using var factory = Factory();
        using var client = Client(factory);
        Assert.True((await client.GetFromJsonAsync<JsonElement>("/api/demo")).GetProperty("enabled").GetBoolean());
        await ApiFixture.Csrf(client);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/login",
            new { email = "atendente@bancada.example", password = ApiFixture.Password })).StatusCode);
    }

    [Fact]
    public async Task Visitors_are_isolated_in_every_order_surface()
    {
        using var factory = Factory();
        using var a = Client(factory);
        using var b = Client(factory);
        var sessionA = await Start(a);
        var sessionB = await Start(b);
        Assert.NotEqual(sessionA.GetProperty("id").GetString(), sessionB.GetProperty("id").GetString());
        Assert.Equal(new[] { "demoRole", "displayName", "id", "role" }, sessionA.EnumerateObject().Select(p => p.Name).Order().ToArray());
        var ordersA = await Orders(a);
        var ordersB = await Orders(b);
        Assert.Equal(5, ordersA.GetProperty("total").GetInt32());
        Assert.Equal(5, ordersB.GetProperty("total").GetInt32());
        var foreign = ordersB.GetProperty("items")[0];
        var id = foreign.GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NotFound, (await a.GetAsync($"/api/orders/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/orders/{id}/diagnosis",
            new { expectedVersion = 1, diagnosis = "Fictional" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/orders/{id}/transitions",
            new { expectedVersion = 1, target = "cancelled", reason = "Fictional" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/orders/{id}/quotes",
            new { expectedVersion = 1, items = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync($"/api/orders/{id}/customer-access",
            new { expectedVersion = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await a.DeleteAsync($"/api/orders/{id}/customer-access")).StatusCode);
        var techniciansA = await a.GetFromJsonAsync<JsonElement>("/api/technicians");
        var techniciansB = await b.GetFromJsonAsync<JsonElement>("/api/technicians");
        Assert.Equal(2, techniciansA.GetArrayLength());
        var foreignTechnician = techniciansB[0].GetProperty("id").GetString();
        Assert.DoesNotContain(techniciansA.EnumerateArray(), t => t.GetProperty("id").GetString() == foreignTechnician);
        Assert.Equal(HttpStatusCode.BadRequest, (await a.GetAsync($"/api/orders?technicianId={foreignTechnician}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await a.PostAsJsonAsync("/api/orders",
            NewOrder(foreignTechnician!))).StatusCode);
        var grant = await b.PostAsJsonAsync($"/api/orders/{id}/customer-access",
            new { expectedVersion = foreign.GetProperty("version").GetInt32() });
        Assert.Equal(HttpStatusCode.OK, grant.StatusCode);
        var token = (await grant.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("url").GetString()!.Split("#token=")[1];
        Assert.Equal(HttpStatusCode.NotFound, (await a.PostAsJsonAsync("/api/customer/session", new { token })).StatusCode);
        using var separateBrowser = Client(factory);
        await ApiFixture.Csrf(separateBrowser);
        Assert.Equal(HttpStatusCode.NoContent, (await separateBrowser.PostAsJsonAsync("/api/customer/session", new { token })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await separateBrowser.GetAsync("/api/customer/order")).StatusCode);
        await ApiFixture.Csrf(a);
        Assert.Equal(HttpStatusCode.OK, (await a.PostAsJsonAsync("/api/demo/reset", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await separateBrowser.GetAsync("/api/customer/order")).StatusCode);
    }

    private static object NewOrder(string technicianId) => new
    {
        customerName = "Cliente Ficticio", customerEmail = "cliente@bancada.example",
        equipmentLabel = "Notebook ficticio", reportedIssue = "Nao liga", technicianId
    };

    [Fact]
    public async Task Repeated_start_reuses_workspace_and_roles_revoke_previous_staff_cookie_even_after_logout()
    {
        using var factory = Factory();
        using var client = Client(factory);
        var first = await Start(client);
        Assert.Equal(first.GetProperty("id").GetString(), (await Start(client)).GetProperty("id").GetString());
        await ApiFixture.Csrf(client);
        var switchResponse = await client.PostAsJsonAsync("/api/demo/role", new { role = "technician1" });
        Assert.Equal(HttpStatusCode.OK, switchResponse.StatusCode);
        var oldCookie = switchResponse.Headers.GetValues("Set-Cookie").Last(c => c.StartsWith("bancada.staff=")).Split(';')[0];
        var tech1 = await switchResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("technician", tech1.GetProperty("role").GetString());
        var tech2 = await Role(client, "technician2");
        Assert.NotEqual(tech1.GetProperty("id").GetString(), tech2.GetProperty("id").GetString());
        using var replay = Client(factory);
        replay.DefaultRequestHeaders.Add("Cookie", oldCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await replay.GetAsync("/api/session")).StatusCode);
        var received = (await Orders(client)).GetProperty("items").EnumerateArray().First(o => o.GetProperty("status").GetString() == "received");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/api/orders/{received.GetProperty("id").GetGuid()}/diagnosis",
            new { expectedVersion = received.GetProperty("version").GetInt32(), diagnosis = "Fictional" })).StatusCode);
        await ApiFixture.Csrf(client);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/logout", null)).StatusCode);
        Assert.Equal("receptionist", (await Role(client, "receptionist")).GetProperty("role").GetString());
        await ApiFixture.Csrf(client);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/demo/role", new { role = "admin" })).StatusCode);
    }

    [Fact]
    public async Task Twenty_order_quota_is_atomic_including_five_seed_orders()
    {
        using var factory = Factory();
        using var client = Client(factory);
        await Start(client);
        var technician = (await client.GetFromJsonAsync<JsonElement>("/api/technicians"))[0].GetProperty("id").GetString()!;
        var responses = await Task.WhenAll(Enumerable.Range(0, 24).Select(_ => client.PostAsJsonAsync("/api/orders", NewOrder(technician))));
        Assert.Equal(15, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(9, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(20, (await Orders(client)).GetProperty("total").GetInt32());
        Assert.Equal(20, (await Orders(client)).GetProperty("total").GetInt32());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Expiry_or_reset_invalidates_staff_customer_sessions_and_raw_grants(bool reset)
    {
        using var factory = Factory();
        using var client = Client(factory);
        var original = await Start(client);
        var order = (await Orders(client)).GetProperty("items")[0];
        var response = await client.PostAsJsonAsync($"/api/orders/{order.GetProperty("id").GetGuid()}/customer-access",
            new { expectedVersion = order.GetProperty("version").GetInt32() });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("url").GetString()!.Split("#token=")[1];
        using var customer = Client(factory);
        await ApiFixture.Csrf(customer);
        Assert.Equal(HttpStatusCode.NoContent, (await customer.PostAsJsonAsync("/api/customer/session", new { token })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync("/api/customer/order")).StatusCode);
        var login = await client.PostAsJsonAsync("/api/demo/start", new { });
        var oldCookie = login.Headers.GetValues("Set-Cookie").Last(c => c.StartsWith("bancada.staff=")).Split(';')[0];
        if (reset)
        {
            await ApiFixture.Csrf(client);
            var fresh = await client.PostAsJsonAsync("/api/demo/reset", new { });
            Assert.Equal(HttpStatusCode.OK, fresh.StatusCode);
            Assert.NotEqual(original.GetProperty("id").GetString(), (await fresh.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString());
            await ApiFixture.Csrf(client);
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/orders/{order.GetProperty("id").GetGuid()}")).StatusCode);
        }
        else
        {
            await using var db = new NpgsqlConnection(fixture.Connection);
            await db.OpenAsync();
            await using var expire = new NpgsqlCommand("""
                UPDATE "DemoWorkspaces" SET "ExpiresAt" = now() - interval '1 second'
                WHERE "Id" = (SELECT "WorkspaceId" FROM "AspNetUsers" WHERE "Id" = @user)
                """, db);
            expire.Parameters.AddWithValue("user", original.GetProperty("id").GetString()!);
            Assert.Equal(1, await expire.ExecuteNonQueryAsync());
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/session")).StatusCode);
        }
        using var replay = Client(factory);
        replay.DefaultRequestHeaders.Add("Cookie", oldCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await replay.GetAsync("/api/session")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await customer.GetAsync("/api/customer/order")).StatusCode);
        await ApiFixture.Csrf(customer);
        Assert.Equal(HttpStatusCode.NotFound, (await customer.PostAsJsonAsync("/api/customer/decision",
            new { quoteRevision = 1, decision = "accepted" })).StatusCode);
        using var rawReplay = Client(factory);
        await ApiFixture.Csrf(rawReplay);
        Assert.Equal(HttpStatusCode.NotFound, (await rawReplay.PostAsJsonAsync("/api/customer/session", new { token })).StatusCode);
        if (!reset) Assert.NotEqual(original.GetProperty("id").GetString(), (await Start(client)).GetProperty("id").GetString());
    }

    [Fact]
    public async Task Demo_mutations_require_csrf_and_are_rate_limited()
    {
        using var factory = Factory();
        using var client = Client(factory);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/demo/start", new { })).StatusCode);
        await Start(client);
        var responses = new List<HttpResponseMessage>();
        for (var i = 0; i < 25; i++)
            responses.Add(await client.PostAsJsonAsync("/api/demo/role", new { role = "invalid" }));
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Expiry_is_fixed_and_concurrent_start_with_same_visitor_is_bounded()
    {
        using var factory = Factory();
        using var client = Client(factory);
        var first = await Start(client);
        var userId = first.GetProperty("id").GetString()!;
        await using var connection = new NpgsqlConnection(fixture.Connection);
        await connection.OpenAsync();
        async Task<DateTime> Expiry()
        {
            await using var command = new NpgsqlCommand("""
                SELECT "ExpiresAt" FROM "DemoWorkspaces"
                WHERE "Id" = (SELECT "WorkspaceId" FROM "AspNetUsers" WHERE "Id" = @user)
                """, connection);
            command.Parameters.AddWithValue("user", userId);
            return (DateTime)(await command.ExecuteScalarAsync())!;
        }
        var expiresAt = await Expiry();
        Assert.InRange(expiresAt - DateTime.UtcNow, TimeSpan.FromHours(23.9), TimeSpan.FromHours(24));
        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => client.PostAsJsonAsync("/api/demo/start", new { })));
        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(userId, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString());
        }
        await Role(client, "technician1");
        Assert.Equal(expiresAt, await Expiry());
        await using var count = new NpgsqlCommand("""
            SELECT count(*) FROM "Orders" WHERE "WorkspaceId" =
                (SELECT "WorkspaceId" FROM "AspNetUsers" WHERE "Id" = @user)
            """, connection);
        count.Parameters.AddWithValue("user", userId);
        Assert.Equal(5L, await count.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Cleanup_is_bounded_and_removes_only_expired_private_graphs()
    {
        using var factory = Factory();
        using var client = Client(factory);
        await Start(client);
        // An independent host has its own due timer, as happens on a serverless cold start.
        var expiredIds = new List<Guid>();
        await using var connection = new NpgsqlConnection(fixture.Connection);
        await connection.OpenAsync();
        for (var i = 0; i < 12; i++)
        {
            using var visitor = Client(factory);
            var user = await Start(visitor);
            await using var expire = new NpgsqlCommand("""
                UPDATE "DemoWorkspaces" SET "ExpiresAt" = now() - interval '1 second'
                WHERE "Id" = (SELECT "WorkspaceId" FROM "AspNetUsers" WHERE "Id" = @user)
                RETURNING "Id"
                """, connection);
            expire.Parameters.AddWithValue("user", user.GetProperty("id").GetString()!);
            expiredIds.Add((Guid)(await expire.ExecuteScalarAsync())!);
        }
        using var wakingHost = Factory();
        using var wake = Client(wakingHost);
        Assert.Equal(HttpStatusCode.OK, (await wake.GetAsync("/api/demo")).StatusCode);
        await using var count = new NpgsqlCommand("""SELECT count(*) FROM "DemoWorkspaces" WHERE "Id" = ANY(@ids)""", connection);
        count.Parameters.AddWithValue("ids", expiredIds.ToArray());
        Assert.Equal(2L, await count.ExecuteScalarAsync());
        await using var children = new NpgsqlCommand("""
            SELECT count(*) FROM "Orders" o
            WHERE o."WorkspaceId" = ANY(@ids)
            """, connection);
        children.Parameters.AddWithValue("ids", expiredIds.ToArray());
        Assert.Equal(10L, await children.ExecuteScalarAsync());
        Assert.Equal(5, (await Orders(client)).GetProperty("total").GetInt32());
        using var normal = await fixture.Staff();
        Assert.Equal(3, (await Orders(normal)).GetProperty("total").GetInt32());
        var drainDeadline = DateTime.UtcNow.AddSeconds(10);
        while ((long)(await count.ExecuteScalarAsync())! != 0 && DateTime.UtcNow < drainDeadline)
            await Task.Delay(100);
        Assert.Equal(0L, await count.ExecuteScalarAsync());
        using var nextHost = Factory();
        using var next = Client(nextHost);
        Assert.Equal(HttpStatusCode.OK, (await next.GetAsync("/api/demo")).StatusCode);
        Assert.Equal(0L, await count.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Demo_visitor_cookie_is_opaque_secure_and_survives_host_restart()
    {
        string cookie;
        string userId;
        using (var first = new ApiFactory(fixture.Connection, "Production", demo: true))
        {
            using var client = Client(first);
            await ApiFixture.Csrf(client);
            var response = await client.PostAsJsonAsync("/api/demo/start", new { });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            userId = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
            var header = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("bancada.demo="));
            Assert.Contains("; secure", header, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("; httponly", header, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=strict", header, StringComparison.OrdinalIgnoreCase);
            cookie = header.Split(';')[0];
            Assert.Equal(43, cookie.Split('=')[1].Length);
        }
        using var second = new ApiFactory(fixture.Connection, "Production", demo: true);
        using var resumed = Client(second);
        resumed.DefaultRequestHeaders.Add("Cookie", cookie);
        await ApiFixture.Csrf(resumed);
        var response2 = await resumed.PostAsJsonAsync("/api/demo/start", new { });
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.Equal(userId, (await response2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString());
    }

    [Fact]
    public async Task Concurrent_first_visitors_work_after_bootstrap_without_global_seed()
    {
        var empty = new ApiFixture { Seed = false };
        await empty.InitializeAsync();
        try
        {
            using var factory = new ApiFactory(empty.Connection, logs: empty.Logs, demo: true);
            var clients = Enumerable.Range(0, 8).Select(_ => Client(factory)).ToArray();
            try
            {
                await Task.WhenAll(clients.Select(ApiFixture.Csrf));
                var responses = await Task.WhenAll(clients.Select(client => client.PostAsJsonAsync("/api/demo/start", new { })));
                foreach (var response in responses)
                    Assert.True(response.StatusCode == HttpStatusCode.OK, string.Join("\n",
                        empty.Logs.Messages.Where(m => m.Contains("23505") || m.Contains("Identity")).TakeLast(5)));
                foreach (var client in clients) await ApiFixture.Csrf(client);
                var sessions = await Task.WhenAll(responses.Select(async response => await response.Content.ReadFromJsonAsync<JsonElement>()));
                Assert.Equal(8, sessions.Select(s => s.GetProperty("id").GetString()).Distinct().Count());
                foreach (var client in clients) Assert.Equal(5, (await Orders(client)).GetProperty("total").GetInt32());
            }
            finally { foreach (var client in clients) client.Dispose(); }
        }
        finally { await empty.DisposeAsync(); }
    }
}
