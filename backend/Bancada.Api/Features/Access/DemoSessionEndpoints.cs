using Bancada.Api.Infrastructure;

namespace Bancada.Api.Features.Access;

public sealed record DemoRoleRequest(string Role);
public sealed record DemoEmptyRequest;

public static class DemoSessionEndpoints
{
    public static void MapDemo(this WebApplication app)
    {
        app.MapGet("/api/demo", (DemoResetService service) => Results.Ok(new { enabled = service.Enabled }));
        var group = app.MapGroup("/api/demo").RequireRateLimiting("demo");
        group.MapPost("/start", async (DemoEmptyRequest request, HttpContext http, DemoResetService service) =>
            Results.Ok(await service.EnterAsync(http, "receptionist", false, true)));
        group.MapPost("/role", async (DemoRoleRequest request, HttpContext http, DemoResetService service) =>
            Results.Ok(await service.EnterAsync(http, request.Role, false, false)));
        group.MapPost("/reset", async (DemoEmptyRequest request, HttpContext http, DemoResetService service) =>
            Results.Ok(await service.EnterAsync(http, "receptionist", true, false)));
    }
}
