using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Bancada.Tests;

public class HealthTests
{
    [Fact]
    public async Task Readiness_requires_database()
    {
        var connection = Environment.GetEnvironmentVariable("BANCADA_TEST_CONNECTION")
            ?? throw new InvalidOperationException("BANCADA_TEST_CONNECTION is required; database tests may not be skipped.");
        using var factory = new ReadinessFactory(connection);
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task Readiness_reports_unavailable_database()
    {
        using var factory = new ReadinessFactory("Host=127.0.0.1;Port=1;Database=absent;Username=absent;Password=absent;Timeout=1");
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready")).StatusCode);
    }
}

internal sealed class ReadinessFactory(string connection) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Bancada"] = connection }));
    }
}
