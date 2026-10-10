using Bancada.Api.Features.Access;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Bancada.Api.Features.Orders;
using Bancada.Api.Features.History;
using Bancada.Api.Features.Quotes;

namespace Bancada.Api.Infrastructure;

public static class Bootstrap
{
    public static async Task RunAsync(IServiceProvider services, bool seed)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BancadaDb>();
        await db.Database.MigrateAsync();
        if (!seed) return;
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        if (!environment.IsDevelopment() && !configuration.GetValue<bool>("BANCADA_ALLOW_PRODUCTION_SEED"))
            throw new InvalidOperationException("Demo seeding outside Development requires BANCADA_ALLOW_PRODUCTION_SEED=true.");
        var password = configuration["BANCADA_DEMO_PASSWORD"];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("BANCADA_DEMO_PASSWORD is required for demo seeding.");
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "receptionist", "technician" })
            if (!await roles.RoleExistsAsync(role)) Check(await roles.CreateAsync(new IdentityRole(role)));
        var users = scope.ServiceProvider.GetRequiredService<UserManager<StaffUser>>();
        var people = new[]
        {
            ("00000000-0000-0000-0000-000000000001", "atendente@bancada.example", "Atendente Demo", "receptionist"),
            ("00000000-0000-0000-0000-000000000002", "tecnico1@bancada.example", "Tecnico Um", "technician"),
            ("00000000-0000-0000-0000-000000000003", "tecnico2@bancada.example", "Tecnico Dois", "technician")
        };
        foreach (var (id, email, name, role) in people)
        {
            var user = await users.FindByEmailAsync(email);
            if (user is null)
            {
                user = new StaffUser { Id = id, Email = email, UserName = email, DisplayName = name, EmailConfirmed = true };
                Check(await users.CreateAsync(user, password));
            }
            if (!await users.IsInRoleAsync(user, role)) Check(await users.AddToRoleAsync(user, role));
        }
        await SeedOrders(db);
    }

    private static async Task SeedOrders(BancadaDb db)
    {
        var now = new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        await using var transaction = await db.Database.BeginTransactionAsync();
        for (var index = 1; index <= 3; index++)
        {
            var id = Guid.Parse($"10000000-0000-0000-0000-{index:000000000000}");
            if (await db.Orders.AnyAsync(o => o.Id == id)) continue;
            var order = new Order
            {
                Id = id, Code = $"BC-DEMO-{index:000}", CustomerName = $"Cliente Ficticio {index}",
                CustomerEmail = $"cliente{index}@bancada.example", EquipmentLabel = $"Notebook Demo {index}",
                EquipmentSerial = $"FICTICIO-{index:000}", ReportedIssue = "Equipamento de demonstracao nao liga.",
                TechnicianId = "00000000-0000-0000-0000-000000000002", TechnicianName = "Tecnico Um", UpdatedAt = now
            };
            order.History.Add(new OrderEvent
            {
                Id = Guid.Parse($"20000000-0000-0000-0000-{index:000000000000}"),
                OrderId = id, Action = "received", ActorName = "Atendente Demo", CreatedAt = now, Detail = "Equipamento recebido."
            });
            if (index >= 2)
            {
                order.Diagnosis = "Fonte de demonstracao defeituosa.";
                order.Status = "diagnosing";
                order.Changed("diagnosisUpdated", "Tecnico Um", "Diagnostico atualizado.", now.AddMinutes(10));
                order.History[^1].Id = Guid.Parse($"30000000-0000-0000-0000-{index:000000000000}");
            }
            if (index == 3)
            {
                var quote = new Quote
                {
                    Id = Guid.Parse("40000000-0000-0000-0000-000000000003"), OrderId = id, Revision = 1,
                    PublishedAt = now.AddMinutes(20),
                    Items = [new QuoteItem
                    {
                        Id = Guid.Parse("50000000-0000-0000-0000-000000000003"),
                        Description = "Fonte ficticia", Kind = "part", Quantity = 1, UnitPrice = 89.90m
                    }]
                };
                quote.Total = Quote.CalculateTotal(quote.Items);
                order.Quotes.Add(quote);
                order.Status = "awaitingApproval";
                order.Changed("quotePublished", "Tecnico Um", "Orcamento revisao 1 publicado.", quote.PublishedAt);
                order.History[^1].Id = Guid.Parse("60000000-0000-0000-0000-000000000003");
            }
            db.Orders.Add(order);
        }
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException("Identity bootstrap failed: " + string.Join(", ", result.Errors.Select(e => e.Code)));
    }
}
