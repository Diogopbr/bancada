using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Bancada.Api.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bancada.Tests;

public sealed class ApiFixture : IAsyncLifetime
{
    private readonly string admin = Environment.GetEnvironmentVariable("BANCADA_TEST_CONNECTION")
        ?? throw new InvalidOperationException("BANCADA_TEST_CONNECTION is required.");
    private readonly string database = $"bancada_test_{Guid.NewGuid():N}";
    public const string Password = "Fictional-test-only!927";
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public string Connection { get; private set; } = "";
    public TestLogs Logs { get; } = new();
    public bool Seed { get; init; } = true;

    public async Task InitializeAsync()
    {
        await using var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(admin) { Pooling = false }.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", connection);
        await command.ExecuteNonQueryAsync();
        Connection = new NpgsqlConnectionStringBuilder(admin) { Database = database, Pooling = false }.ConnectionString;
        Factory = new ApiFactory(Connection, logs: Logs);
        using (var client = Client())
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        await using (var isolated = new NpgsqlConnection(Connection))
        {
            await isolated.OpenAsync();
            await using var schema = new NpgsqlCommand("SELECT to_regclass('\"AspNetUsers\"') IS NULL", isolated);
            Assert.Equal(true, await schema.ExecuteScalarAsync());
        }
        await Bootstrap.RunAsync(Factory.Services, Seed);
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null) await Factory.DisposeAsync();
        await using var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(admin) { Pooling = false }.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand($"DROP DATABASE \"{database}\"", connection) { CommandTimeout = 60 };
        await command.ExecuteNonQueryAsync();
        await using var verify = new NpgsqlCommand("SELECT COUNT(*) FROM pg_database WHERE datname = @database", connection);
        verify.Parameters.AddWithValue("database", database);
        Assert.Equal(0L, await verify.ExecuteScalarAsync());
    }

    public HttpClient Client() => Factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost")
    });

    public static async Task Csrf(HttpClient client)
    {
        var result = await client.GetFromJsonAsync<JsonElement>("/api/csrf");
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", result.GetProperty("token").GetString());
    }

    public async Task<HttpClient> Staff(string email = "atendente@bancada.example")
    {
        var client = Client();
        await Csrf(client);
        var response = await client.PostAsJsonAsync("/api/login", new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await Csrf(client);
        return client;
    }
}

internal sealed class ApiFactory(string connection, string environment = "Development", TestLogs? logs = null,
    bool demo = false, string? webRoot = null, string? trustedProxy = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        if (webRoot is not null) builder.UseWebRoot(webRoot);
        builder.ConfigureServices(services => services.AddTransient<IStartupFilter, TestIpFilter>());
        if (logs is not null) builder.ConfigureLogging(logging => logging.AddProvider(logs));
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Bancada"] = connection,
                ["BANCADA_DEMO_PASSWORD"] = ApiFixture.Password,
                ["BANCADA_DEMO_MODE"] = demo.ToString()
            };
            if (trustedProxy is not null) settings["Hosting:TrustedProxies:0"] = trustedProxy;
            configuration.AddInMemoryCollection(settings);
        });
    }

}

public sealed class TestLogs : ILoggerProvider
    {
        public System.Collections.Concurrent.ConcurrentQueue<string> Messages { get; } = new();
        public ILogger CreateLogger(string categoryName) => new CapturedLogger(Messages);
        public void Dispose() { }

        private sealed class CapturedLogger(System.Collections.Concurrent.ConcurrentQueue<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) => messages.Enqueue(formatter(state, exception));
        }
    }
internal sealed class TestIpFilter : IStartupFilter
{
    private static int nextIp;
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        var clients = new System.Collections.Concurrent.ConcurrentDictionary<string, IPAddress>();
        app.Use(async (http, continuation) =>
        {
            // TestServer clients have no socket IP. Their CSRF cookie identifies a simulated IP.
            var key = http.Request.Cookies["bancada.csrf"] ?? Guid.NewGuid().ToString();
            http.Connection.RemoteIpAddress = clients.GetOrAdd(key,
                _ => new IPAddress(new byte[] { 10, 0, (byte)(Volatile.Read(ref nextIp) / 250), (byte)(Interlocked.Increment(ref nextIp) % 250 + 1) }));
            if (http.Request.Headers.ContainsKey("X-Forwarded-Proto"))
                http.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
            await continuation();
        });
        next(app);
    };
}
