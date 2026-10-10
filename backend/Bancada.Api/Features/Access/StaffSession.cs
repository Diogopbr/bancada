using System.Security.Claims;
using Bancada.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bancada.Api.Features.Access;

public sealed class StaffSession
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public bool Revoked { get; set; }
}

public static class StaffSessionEvents
{
    public static async Task SignedIn(CookieSigningInContext context)
    {
        var principal = context.Principal ?? throw new InvalidOperationException("Staff principal missing.");
        var identity = principal.Identity as ClaimsIdentity ?? throw new InvalidOperationException("Staff identity missing.");
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Staff user missing.");
        var session = new StaffSession { Id = Guid.NewGuid(), UserId = userId, ExpiresAt = BusinessTime.UtcNow.AddHours(8) };
        identity.AddClaim(new Claim("staffSessionId", session.Id.ToString()));
        var db = context.HttpContext.RequestServices.GetRequiredService<BancadaDb>();
        db.StaffSessions.Add(session);
        await db.SaveChangesAsync();
    }

    public static async Task Validate(CookieValidatePrincipalContext context)
    {
        await SecurityStampValidator.ValidatePrincipalAsync(context);
        var db = context.HttpContext.RequestServices.GetRequiredService<BancadaDb>();
        var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var workspaceClaim = context.Principal?.FindFirstValue("workspaceId");
        var demo = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue<bool>("BANCADA_DEMO_MODE");
        var workspaceId = Guid.TryParse(workspaceClaim, out var parsed) ? parsed : (Guid?)null;
        if (!Guid.TryParse(context.Principal?.FindFirstValue("staffSessionId"), out var id)
            || !await db.StaffSessions.AsNoTracking().AnyAsync(s => s.Id == id && s.UserId == userId
                && !s.Revoked && s.ExpiresAt > DateTimeOffset.UtcNow)
            || (demo && workspaceId is null) || (!demo && workspaceId is not null)
            || !await db.Users.AnyAsync(u => u.Id == userId && u.WorkspaceId == workspaceId)
            || (workspaceId is not null && !await db.DemoWorkspaces.AnyAsync(w =>
                w.Id == workspaceId && !w.Revoked && w.ExpiresAt > DateTimeOffset.UtcNow)))
            context.RejectPrincipal();
    }

    public static async Task SignedOut(CookieSigningOutContext context)
    {
        if (!Guid.TryParse(context.HttpContext.User.FindFirstValue("staffSessionId"), out var id)) return;
        var db = context.HttpContext.RequestServices.GetRequiredService<BancadaDb>();
        await db.StaffSessions.Where(s => s.Id == id).ExecuteUpdateAsync(update => update.SetProperty(s => s.Revoked, true));
    }
}
