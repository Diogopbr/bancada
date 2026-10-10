using Bancada.Api.Features.Access;
using Bancada.Api.Features.History;
using Bancada.Api.Features.Orders;
using Bancada.Api.Features.Quotes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bancada.Api.Infrastructure;

public sealed class DemoResetService(BancadaDb db, UserManager<StaffUser> users, RoleManager<IdentityRole> roles,
    SignInManager<StaffUser> signIn, IConfiguration configuration, IHostEnvironment environment)
{
    public const string CookieName = "bancada.demo";
    public bool Enabled => configuration.GetValue<bool>("BANCADA_DEMO_MODE");
    public static readonly string[] Roles = ["receptionist", "technician1", "technician2"];

    public async Task<object> EnterAsync(HttpContext http, string role, bool reset, bool start)
    {
        if (!Enabled) throw new ApiError(404);
        if (!Roles.Contains(role)) throw new ApiError(400);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var token = http.Request.Cookies[CookieName];
        DemoVisitor? visitor = null;
        if (token?.Length == 43)
        {
            var hash = CustomerAccess.Hash(token);
            visitor = await db.DemoVisitors.FromSqlInterpolated(
                $"SELECT * FROM \"DemoVisitors\" WHERE \"TokenHash\" = {hash} FOR UPDATE").SingleOrDefaultAsync();
        }
        if (visitor is null)
        {
            if (!start) throw new ApiError(401);
            token = CustomerAccess.Token();
            visitor = new DemoVisitor { TokenHash = CustomerAccess.Hash(token) };
            db.DemoVisitors.Add(visitor);
        }
        DemoWorkspace? workspace = null;
        if (visitor.WorkspaceId is Guid workspaceId)
        {
            workspace = await db.DemoWorkspaces.FromSqlInterpolated(
                $"SELECT * FROM \"DemoWorkspaces\" WHERE \"Id\" = {workspaceId} FOR UPDATE").SingleOrDefaultAsync();
        }
        var valid = workspace is { Revoked: false } && workspace.ExpiresAt > DateTimeOffset.UtcNow;
        if (!valid && !start) throw new ApiError(401);
        if (reset && workspace is not null)
        {
            workspace.Revoked = true;
            valid = false;
        }
        if (!valid)
        {
            workspace = new DemoWorkspace();
            db.DemoWorkspaces.Add(workspace);
            visitor.WorkspaceId = workspace.Id;
            await db.SaveChangesAsync();
            await ProvisionAsync(workspace.Id);
        }
        var user = await db.Users.SingleAsync(u => u.WorkspaceId == visitor.WorkspaceId && u.DemoRole == role);
        await db.StaffSessions.Where(s => db.Users.Any(u => u.Id == s.UserId && u.WorkspaceId == visitor.WorkspaceId))
            .ExecuteUpdateAsync(update => update.SetProperty(s => s.Revoked, true));
        await signIn.SignOutAsync();
        await signIn.SignInWithClaimsAsync(user, false,
            [new System.Security.Claims.Claim("workspaceId", visitor.WorkspaceId!.Value.ToString())]);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        http.Response.Cookies.Append(CookieName, token!, new CookieOptions
        {
            HttpOnly = true, Secure = !environment.IsDevelopment() || http.Request.IsHttps, SameSite = SameSiteMode.Strict, Path = "/",
            Expires = workspace!.ExpiresAt, IsEssential = true
        });
        return await StaffEndpoints.Session(user, users);
    }

    private async Task ProvisionAsync(Guid workspaceId)
    {
        if (await db.Roles.CountAsync(r => r.NormalizedName == "RECEPTIONIST" || r.NormalizedName == "TECHNICIAN") < 2)
        {
            // Serialize first-time Identity role creation across hosts without requiring shared seeded accounts.
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext('Bancada'), hashtext('DemoIdentityRoles'))");
        }
        foreach (var role in new[] { "receptionist", "technician" })
            if (!await roles.RoleExistsAsync(role)) Check(await roles.CreateAsync(new IdentityRole(role)));
        var people = new List<StaffUser>();
        foreach (var (role, name) in new[] { ("receptionist", "Atendente Demo"), ("technician1", "Tecnico Um"), ("technician2", "Tecnico Dois") })
        {
            var id = Guid.NewGuid().ToString();
            var user = new StaffUser
            {
                Id = id, UserName = $"{id}@bancada.example", Email = $"{id}@bancada.example",
                EmailConfirmed = true, DisplayName = name, WorkspaceId = workspaceId, DemoRole = role
            };
            Check(await users.CreateAsync(user));
            Check(await users.AddToRoleAsync(user, role == "receptionist" ? "receptionist" : "technician"));
            people.Add(user);
        }
        for (var index = 1; index <= 3; index++)
        {
            var order = new Order
            {
                WorkspaceId = workspaceId, CustomerName = $"Cliente Ficticio {index}",
                CustomerEmail = $"cliente{index}@bancada.example", EquipmentLabel = $"Notebook Demo {index}",
                EquipmentSerial = $"FICTICIO-{index}", ReportedIssue = "Equipamento ficticio nao liga.",
                TechnicianId = people[1].Id, TechnicianName = people[1].DisplayName
            };
            order.Code = $"BC-{order.Id:N}".ToUpperInvariant();
            order.History.Add(new OrderEvent
            {
                OrderId = order.Id, Action = "received", ActorName = people[0].DisplayName,
                Detail = "Equipamento ficticio recebido.", CreatedAt = order.UpdatedAt
            });
            if (index >= 2) order.UpdateDiagnosis(people[1].Id, "technician", order.Version,
                "Fonte ficticia defeituosa.", people[1].DisplayName);
            if (index == 3)
            {
                var quote = new Quote
                {
                    OrderId = order.Id, Revision = 1, PublishedAt = DateTimeOffset.UtcNow,
                    Items = [new QuoteItem { Description = "Fonte ficticia", Kind = "part", Quantity = 1, UnitPrice = 89.90m }]
                };
                quote.Total = Quote.CalculateTotal(quote.Items);
                order.Quotes.Add(quote);
                order.Status = "awaitingApproval";
                order.Changed("quotePublished", people[1].DisplayName, "Orcamento revisao 1 publicado.", quote.PublishedAt);
            }
            db.Orders.Add(order);
        }
        await db.SaveChangesAsync();
    }

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException("Demo identity failed: " + string.Join(", ", result.Errors.Select(e => e.Code)));
    }

    public static async Task LockWorkspaceAsync(BancadaDb db, Guid? id, int errorStatus = 401)
    {
        if (id is null) return;
        var workspace = await db.DemoWorkspaces.FromSqlInterpolated(
            $"SELECT * FROM \"DemoWorkspaces\" WHERE \"Id\" = {id.Value} FOR UPDATE").AsNoTracking().SingleOrDefaultAsync();
        if (workspace is null || workspace.Revoked || workspace.ExpiresAt <= DateTimeOffset.UtcNow)
            throw new ApiError(errorStatus);
    }
}
