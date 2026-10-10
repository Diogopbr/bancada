using Microsoft.EntityFrameworkCore;

namespace Bancada.Api.Infrastructure;

public sealed class DemoCleanupService(IServiceScopeFactory scopes, IConfiguration configuration,
    ILogger<DemoCleanupService> logger) : BackgroundService
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private DateTimeOffset nextCleanup;

    public async Task CleanupIfDueAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("BANCADA_DEMO_MODE") || DateTimeOffset.UtcNow < nextCleanup
            || !await gate.WaitAsync(0, cancellationToken)) return;
        try
        {
            if (DateTimeOffset.UtcNow < nextCleanup) return;
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<BancadaDb>();
            // Do not bootstrap schema on ordinary startup, including the cleanup worker.
            if ((await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any()) return;
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var visitors = await db.DemoVisitors.FromSqlRaw("""
                SELECT v.* FROM "DemoVisitors" v JOIN "DemoWorkspaces" w ON w."Id" = v."WorkspaceId"
                WHERE w."Revoked" OR w."ExpiresAt" <= now()
                ORDER BY w."ExpiresAt" LIMIT 10 FOR UPDATE OF v SKIP LOCKED
                """).ToListAsync(cancellationToken);
            var visitorHashes = visitors.Select(v => v.TokenHash).ToArray();
            var expired = await db.DemoWorkspaces.FromSqlRaw("""
                SELECT * FROM "DemoWorkspaces"
                WHERE "Revoked" OR "ExpiresAt" <= now()
                ORDER BY "ExpiresAt" LIMIT 10 FOR UPDATE SKIP LOCKED
                """).Where(w => !db.DemoVisitors.Any(v => v.WorkspaceId == w.Id && !visitorHashes.Contains(v.TokenHash)))
                .ToListAsync(cancellationToken);
            var ids = expired.Select(w => w.Id).ToArray();
            await db.Orders.IgnoreQueryFilters().Where(o => ids.Contains(o.WorkspaceId!.Value))
                .ExecuteDeleteAsync(cancellationToken);
            await db.Users.Where(u => ids.Contains(u.WorkspaceId!.Value)).ExecuteDeleteAsync(cancellationToken);
            await db.DemoVisitors.Where(v => v.WorkspaceId != null && ids.Contains(v.WorkspaceId.Value))
                .ExecuteDeleteAsync(cancellationToken);
            await db.DemoWorkspaces.Where(w => ids.Contains(w.Id)).ExecuteDeleteAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            // Keep transactions bounded, but drain a backlog without a five-minute pause per batch.
            nextCleanup = ids.Length == 10
                ? DateTimeOffset.UtcNow.AddSeconds(1)
                : DateTimeOffset.UtcNow.AddMinutes(5);
        }
        finally { gate.Release(); }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try { await CleanupIfDueAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Npgsql.NpgsqlException error) { logger.LogError(error, "Demo cleanup failed; expired workspaces remain inaccessible."); }
        }
    }
}
