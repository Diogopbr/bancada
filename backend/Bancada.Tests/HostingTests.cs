using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using System.Diagnostics;
using System.Net.Sockets;

namespace Bancada.Tests;

public class HostingTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Database_keys_preserve_staff_and_customer_cookies_across_restart_and_revocation()
    {
        string staffCookie;
        string customerCookie;
        using (var first = new ApiFactory(fixture.Connection, "Production"))
        {
            using var client = first.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            await ApiFixture.Csrf(client);
            var login = await client.PostAsJsonAsync("/api/login",
                new { email = "atendente@bancada.example", password = ApiFixture.Password });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            staffCookie = login.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("bancada.staff=")).Split(';')[0];
            await ApiFixture.Csrf(client);
            var orders = await client.GetFromJsonAsync<JsonElement>("/api/orders");
            var order = orders.GetProperty("items")[0];
            var grant = await client.PostAsJsonAsync($"/api/orders/{order.GetProperty("id").GetGuid()}/customer-access",
                new { expectedVersion = order.GetProperty("version").GetInt32() });
            var token = (await grant.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("url").GetString()!.Split("#token=")[1];
            using var customer = first.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            await ApiFixture.Csrf(customer);
            var exchange = await customer.PostAsJsonAsync("/api/customer/session", new { token });
            Assert.Equal(HttpStatusCode.NoContent, exchange.StatusCode);
            customerCookie = exchange.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("bancada.customer=")).Split(';')[0];
        }
        using var second = new ApiFactory(fixture.Connection, "Production");
        using var restarted = second.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        restarted.DefaultRequestHeaders.Add("Cookie", staffCookie);
        Assert.Equal(HttpStatusCode.OK, (await restarted.GetAsync("/api/session")).StatusCode);
        using var resumedCustomer = second.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        resumedCustomer.DefaultRequestHeaders.Add("Cookie", customerCookie);
        Assert.Equal(HttpStatusCode.OK, (await resumedCustomer.GetAsync("/api/customer/order")).StatusCode);
        await ApiFixture.Csrf(restarted);
        Assert.Equal(HttpStatusCode.NoContent, (await restarted.PostAsync("/api/logout", null)).StatusCode);
        using var replay = second.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        replay.DefaultRequestHeaders.Add("Cookie", staffCookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await replay.GetAsync("/api/session")).StatusCode);
        using var staff = await fixture.Staff();
        var current = (await staff.GetFromJsonAsync<JsonElement>("/api/orders")).GetProperty("items")[0];
        Assert.Equal(HttpStatusCode.NoContent, (await staff.DeleteAsync($"/api/orders/{current.GetProperty("id").GetGuid()}/customer-access")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await resumedCustomer.GetAsync("/api/customer/order")).StatusCode);
        await using var connection = new NpgsqlConnection(fixture.Connection);
        await connection.OpenAsync();
        await using var keys = new NpgsqlCommand("SELECT count(*) FROM \"DataProtectionKeys\"", connection);
        Assert.True((long)(await keys.ExecuteScalarAsync())! > 0);
    }

    [Fact]
    public async Task Compiled_spa_and_assets_are_served_but_api_health_and_missing_assets_never_fallback()
    {
        var root = Path.Combine(Path.GetTempPath(), $"bancada-hosting-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root, "index.html"), "<html><body>Bancada test SPA</body></html>");
            await File.WriteAllTextAsync(Path.Combine(root, "main.js"), "window.bancada = true;");
            using var factory = new ApiFactory(fixture.Connection, "Production", webRoot: root);
            using var client = factory.CreateClient();
            Assert.Contains("Bancada test SPA", await client.GetStringAsync("/"));
            Assert.Contains("Bancada test SPA", await client.GetStringAsync("/ordens/qualquer"));
            Assert.Contains("window.bancada", await client.GetStringAsync("/main.js"));
            foreach (var path in new[] { "/api/unknown", "/health/unknown", "/missing.js" })
            {
                var response = await client.GetAsync(path);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                Assert.DoesNotContain("text/html", response.Content.Headers.ContentType?.ToString() ?? "");
            }
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        }
        finally { Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("10.0.0.1", true)]
    public async Task Forwarded_https_is_only_honored_from_explicit_trusted_proxy(string? proxy, bool secure)
    {
        using var factory = new ApiFactory(fixture.Connection, trustedProxy: proxy);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("http://localhost") });
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.1");
        var response = await client.GetAsync("/api/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("bancada.csrf="));
        Assert.Equal(secure, cookie.Contains("; secure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Unavailable_health_and_unauthenticated_api_never_return_spa_html()
    {
        using var factory = new ApiFactory("Host=127.0.0.1;Port=1;Database=absent;Username=absent;Timeout=1", "Production");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var health = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, health.StatusCode);
        Assert.DoesNotContain("text/html", health.Content.Headers.ContentType?.ToString() ?? "");
        var api = await client.GetAsync("/api/session");
        Assert.Equal(HttpStatusCode.Unauthorized, api.StatusCode);
        Assert.DoesNotContain("text/html", api.Content.Headers.ContentType?.ToString() ?? "");
    }

    [Fact]
    public async Task Real_process_restart_preserves_demo_staff_customer_and_visitor_then_reset_revokes_them()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var address = new Uri($"http://127.0.0.1:{port}");
        var logs = new List<Task<string>>();
        Process Launch()
        {
            var start = new ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
                UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true
            };
            start.ArgumentList.Add(typeof(Program).Assembly.Location);
            start.Environment["ASPNETCORE_URLS"] = address.ToString();
            start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            start.Environment["DOTNET_ENVIRONMENT"] = "Development";
            start.Environment["ConnectionStrings__Bancada"] = fixture.Connection;
            start.Environment["BANCADA_DEMO_MODE"] = "true";
            var process = Process.Start(start) ?? throw new InvalidOperationException("Demo host did not start.");
            logs.Add(process.StandardOutput.ReadToEndAsync());
            logs.Add(process.StandardError.ReadToEndAsync());
            return process;
        }
        async Task Ready(Process process, HttpClient client)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            while (!timeout.IsCancellationRequested)
            {
                Assert.False(process.HasExited, "Demo host exited before readiness.");
                try
                {
                    if ((await client.GetAsync("/health/ready", timeout.Token)).StatusCode == HttpStatusCode.OK) return;
                }
                catch (HttpRequestException error) when (error.InnerException is SocketException { SocketErrorCode: SocketError.ConnectionRefused })
                {
                    fixture.Logs.Messages.Enqueue("Awaiting demo host socket readiness.");
                }
                await Task.Delay(100, timeout.Token);
            }
            throw new TimeoutException("Demo host readiness timed out.");
        }
        using var handler = new HttpClientHandler { CookieContainer = new CookieContainer() };
        using var staff = new HttpClient(handler) { BaseAddress = address };
        using var customerHandler = new HttpClientHandler { CookieContainer = new CookieContainer() };
        using var customer = new HttpClient(customerHandler) { BaseAddress = address };
        var process = Launch();
        try
        {
            await Ready(process, staff);
            await ApiFixture.Csrf(staff);
            Assert.Equal(HttpStatusCode.OK, (await staff.PostAsJsonAsync("/api/demo/start", new { })).StatusCode);
            await ApiFixture.Csrf(staff);
            var originalSession = await staff.GetFromJsonAsync<JsonElement>("/api/session");
            var order = (await staff.GetFromJsonAsync<JsonElement>("/api/orders")).GetProperty("items")[0];
            var grant = await staff.PostAsJsonAsync($"/api/orders/{order.GetProperty("id").GetGuid()}/customer-access",
                new { expectedVersion = order.GetProperty("version").GetInt32() });
            var token = (await grant.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("url").GetString()!.Split("#token=")[1];
            await ApiFixture.Csrf(customer);
            Assert.Equal(HttpStatusCode.NoContent, (await customer.PostAsJsonAsync("/api/customer/session", new { token })).StatusCode);
            var staffCookie = handler.CookieContainer.GetCookies(address)["bancada.staff"]!.Value;
            process.Kill(true);
            await process.WaitForExitAsync();
            process.Dispose();
            process = Launch();
            await Ready(process, staff);
            Assert.Equal(originalSession.GetProperty("id").GetString(),
                (await staff.GetFromJsonAsync<JsonElement>("/api/session")).GetProperty("id").GetString());
            Assert.Equal(HttpStatusCode.OK, (await customer.GetAsync("/api/customer/order")).StatusCode);
            await ApiFixture.Csrf(staff);
            Assert.Equal(HttpStatusCode.OK, (await staff.PostAsJsonAsync("/api/demo/role", new { role = "technician2" })).StatusCode);
            await ApiFixture.Csrf(staff);
            Assert.Equal(HttpStatusCode.OK, (await staff.PostAsJsonAsync("/api/demo/reset", new { })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await customer.GetAsync("/api/customer/order")).StatusCode);
            using var replay = new HttpClient { BaseAddress = address };
            replay.DefaultRequestHeaders.Add("Cookie", "bancada.staff=" + staffCookie);
            Assert.Equal(HttpStatusCode.Unauthorized, (await replay.GetAsync("/api/session")).StatusCode);
        }
        finally
        {
            if (!process.HasExited) process.Kill(true);
            await process.WaitForExitAsync();
            process.Dispose();
            await Task.WhenAll(logs);
        }
    }
}
