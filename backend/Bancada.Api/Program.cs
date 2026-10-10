using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
var app = builder.Build();
app.UseExceptionHandler();
app.MapOpenApi();
app.MapGet("/health/ready", async (IConfiguration configuration, CancellationToken cancellationToken) =>
{
    var connectionString = configuration.GetConnectionString("Bancada");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    try
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT 1", connection);
        await command.ExecuteScalarAsync(cancellationToken);
        return Results.Ok(new { status = "ready" });
    }
    catch (NpgsqlException)
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}).WithName("Readiness");

app.Run();

public partial class Program { }
