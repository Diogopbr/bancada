using System.Security.Claims;
using Bancada.Api.Features.Orders;
using Bancada.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Bancada.Api.Features.Access;

public sealed class CustomerAccessService(BancadaDb db, OrderStore store, IConfiguration configuration, IHttpContextAccessor http)
{
    public const string Scheme = "customer";

    public static async Task<Guid> SessionAsync(HttpContext http)
    {
        var authentication = await http.AuthenticateAsync(Scheme);
        if (!authentication.Succeeded || !Guid.TryParse(authentication.Principal?.FindFirstValue("accessId"), out var id))
            throw new ApiError(404);
        return id;
    }

    public async Task<T> WithOrderAsync<T>(Guid accessId, Func<Order, CustomerAccess, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var locator = await db.CustomerAccesses.AsNoTracking().SingleOrDefaultAsync(a => a.Id == accessId, cancellationToken)
            ?? throw new ApiError(404);
        var workspace = await db.Orders.IgnoreQueryFilters().AsNoTracking()
            .Where(o => o.Id == locator.OrderId).Select(o => new { o.WorkspaceId }).SingleOrDefaultAsync(cancellationToken)
            ?? throw new ApiError(404);
        if (configuration.GetValue<bool>("BANCADA_DEMO_MODE") != (workspace.WorkspaceId is not null))
            throw new ApiError(404);
        if (db.CurrentWorkspaceId is not null && db.CurrentWorkspaceId != workspace.WorkspaceId)
            throw new ApiError(404);
        var visitorToken = http.HttpContext?.Request.Cookies[DemoResetService.CookieName];
        if (workspace.WorkspaceId is not null && visitorToken?.Length == 43)
        {
            var visitorHash = CustomerAccess.Hash(visitorToken);
            var visitorWorkspace = await db.DemoVisitors.AsNoTracking().Where(v => v.TokenHash == visitorHash)
                .Select(v => v.WorkspaceId).SingleOrDefaultAsync(cancellationToken);
            if (visitorWorkspace is not null && visitorWorkspace != workspace.WorkspaceId) throw new ApiError(404);
        }
        await DemoResetService.LockWorkspaceAsync(db, workspace.WorkspaceId, 404);
        var order = await store.LoadAsync(locator.OrderId, true, customer: true);
        // Grant changes and decisions share the order lock, including the post-lock validity check.
        var access = await db.CustomerAccesses.AsNoTracking().SingleOrDefaultAsync(a => a.Id == accessId, cancellationToken);
        if (access is null || access.Revoked || access.ExpiresAt <= DateTimeOffset.UtcNow) throw new ApiError(404);
        var existingEvents = order.History.Select(e => e.Id).ToHashSet();
        var result = await operation(order, access);
        foreach (var entry in order.History.Where(e => !existingEvents.Contains(e.Id))) db.OrderEvents.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
