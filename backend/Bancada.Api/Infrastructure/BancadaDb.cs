using Bancada.Api.Features.Access;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Bancada.Api.Features.Orders;
using Bancada.Api.Features.History;
using Bancada.Api.Features.Quotes;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace Bancada.Api.Infrastructure;

public sealed class BancadaDb(DbContextOptions<BancadaDb> options, IHttpContextAccessor http) :
    IdentityDbContext<StaffUser>(options), IDataProtectionKeyContext
{
    public Guid? CurrentWorkspaceId => Guid.TryParse(http.HttpContext?.User.FindFirst("workspaceId")?.Value, out var id) ? id : null;
    public DbSet<DemoWorkspace> DemoWorkspaces => Set<DemoWorkspace>();
    public DbSet<DemoVisitor> DemoVisitors => Set<DemoVisitor>();
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderEvent> OrderEvents => Set<OrderEvent>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<CustomerAccess> CustomerAccesses => Set<CustomerAccess>();
    public DbSet<StaffSession> StaffSessions => Set<StaffSession>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasSequence<long>("OrderNumbers").StartsAt(10001);
        builder.Entity<DemoWorkspace>().HasIndex(w => w.ExpiresAt);
        builder.Entity<DemoVisitor>(visitor =>
        {
            visitor.HasKey(v => v.TokenHash);
            visitor.Property(v => v.TokenHash).HasMaxLength(64);
            visitor.HasOne<DemoWorkspace>().WithMany().HasForeignKey(v => v.WorkspaceId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<StaffUser>().HasOne<DemoWorkspace>().WithMany().HasForeignKey(u => u.WorkspaceId);
        builder.Entity<Order>(order =>
        {
            order.HasOne<DemoWorkspace>().WithMany().HasForeignKey(o => o.WorkspaceId);
            order.HasQueryFilter(o => o.WorkspaceId == CurrentWorkspaceId
                && (o.WorkspaceId == null || DemoWorkspaces.Any(w => w.Id == o.WorkspaceId && !w.Revoked && w.ExpiresAt > DateTimeOffset.UtcNow)));
            order.Property(o => o.Version).IsConcurrencyToken();
            order.Property(o => o.Code)
                .HasDefaultValueSql("'BC-' || nextval('\"OrderNumbers\"')::text")
                .HasSentinel("");
            order.HasIndex(o => o.Code).IsUnique();
            order.HasOne<StaffUser>().WithMany().HasForeignKey(o => o.TechnicianId).OnDelete(DeleteBehavior.Restrict);
            order.HasMany(o => o.History).WithOne().HasForeignKey(e => e.OrderId);
            order.HasMany(o => o.Quotes).WithOne().HasForeignKey(q => q.OrderId);
        });
        builder.Entity<Quote>(quote =>
        {
            quote.HasIndex(q => new { q.OrderId, q.Revision }).IsUnique();
            quote.Property(q => q.Total).HasPrecision(14, 2);
            quote.HasMany(q => q.Items).WithOne().HasForeignKey(i => i.QuoteId);
        });
        builder.Entity<QuoteItem>().Property(i => i.UnitPrice).HasPrecision(8, 2);
        builder.Entity<CustomerAccess>(access =>
        {
            access.HasIndex(a => a.TokenHash).IsUnique();
            access.Property(a => a.TokenHash).HasMaxLength(64);
            access.HasOne<Order>().WithMany().HasForeignKey(a => a.OrderId);
        });
        builder.Entity<StaffSession>().HasOne<StaffUser>().WithMany().HasForeignKey(s => s.UserId);
    }
}
