using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Bancada.Api.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Bancada.Tests;

public class StaffAccessTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Missing_session_is_401()
    {
        using var client = fixture.Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/session")).StatusCode);
    }

    [Fact]
    public async Task Login_session_logout_and_csrf()
    {
        using var client = fixture.Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/login",
            new { email = "atendente@bancada.example", password = ApiFixture.Password })).StatusCode);
        await ApiFixture.Csrf(client);
        var login = await client.PostAsJsonAsync("/api/login",
            new { email = "atendente@bancada.example", password = ApiFixture.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains(login.Headers.GetValues("Set-Cookie"), x => x.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        var session = await client.GetFromJsonAsync<JsonElement>("/api/session");
        Assert.Equal("receptionist", session.GetProperty("role").GetString());
        Assert.False(session.TryGetProperty("password", out _));
        await ApiFixture.Csrf(client);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/session")).StatusCode);
    }

    [Fact]
    public async Task Invalid_login_is_generic()
    {
        using var client = fixture.Client();
        await ApiFixture.Csrf(client);
        var first = await client.PostAsJsonAsync("/api/login", new { email = "absent@bancada.example", password = "wrong" });
        var second = await client.PostAsJsonAsync("/api/login", new { email = "atendente@bancada.example", password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Equal((await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString(),
            (await second.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Staff_logout_revokes_replayed_cookie()
    {
        using var client = fixture.Client();
        await ApiFixture.Csrf(client);
        var login = await client.PostAsJsonAsync("/api/login",
            new { email = "atendente@bancada.example", password = ApiFixture.Password });
        var cookie = login.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("bancada.staff=")).Split(';')[0];
        await ApiFixture.Csrf(client);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/logout", null)).StatusCode);
        using var replay = fixture.Client();
        replay.DefaultRequestHeaders.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await replay.GetAsync("/api/session")).StatusCode);
    }

    [Fact]
    public async Task Staff_production_cookies_are_secure_and_seed_is_explicit()
    {
        using var factory = new ApiFactory(fixture.Connection, "Production");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        await ApiFixture.Csrf(client);
        var response = await client.PostAsJsonAsync("/api/login",
            new { email = "atendente@bancada.example", password = ApiFixture.Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("bancada.staff="));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Bootstrap.RunAsync(factory.Services, true));
    }

    [Fact]
    public async Task Staff_seed_is_idempotent_and_fictional()
    {
        await Bootstrap.RunAsync(fixture.Factory.Services, true);
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BancadaDb>();
        Assert.Equal(3, await db.Users.CountAsync());
        Assert.Equal(3, await db.Orders.CountAsync(o => o.Code.StartsWith("BC-DEMO-")));
        Assert.All(await db.Users.ToListAsync(), user => Assert.EndsWith("@bancada.example", user.Email));
    }

    [Fact]
    public async Task Staff_explicit_bootstrap_command_runs_and_exits()
    {
        var start = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false
        };
        start.ArgumentList.Add(typeof(Program).Assembly.Location);
        start.ArgumentList.Add("--bootstrap");
        start.ArgumentList.Add("--seed");
        start.Environment["ConnectionStrings__Bancada"] = fixture.Connection;
        start.Environment["BANCADA_DEMO_PASSWORD"] = ApiFixture.Password;
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Bootstrap process did not start.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            throw;
        }
        Assert.True(process.ExitCode == 0, await error);
        Assert.DoesNotContain(ApiFixture.Password, await output);
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        Assert.Equal(3, await scope.ServiceProvider.GetRequiredService<BancadaDb>().Users.CountAsync());
    }
}
