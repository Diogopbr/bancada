using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Bancada.Api.Features.Access;
using Bancada.Api.Features.History;
using Bancada.Api.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bancada.Api.Features.Orders;

public static class OrderEndpoints
{
    public static void MapOrders(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders").RequireAuthorization("staff");
        group.MapPost("", async (CreateOrderRequest request, BancadaDb db, UserManager<StaffUser> users, ClaimsPrincipal principal) =>
        {
            Validation.Text(request.CustomerName, 1, 120);
            Validation.Text(request.EquipmentLabel, 1, 120);
            Validation.Text(request.ReportedIssue, 1, 2000);
            Validation.Text(request.EquipmentSerial ?? "", 0, 100);
            if (string.IsNullOrWhiteSpace(request.CustomerEmail) || request.CustomerEmail.Length > 254
                || !new EmailAddressAttribute().IsValid(request.CustomerEmail)) throw new ApiError(400);
            var technician = await users.FindByIdAsync(request.TechnicianId ?? "");
            if (technician is null || !await users.IsInRoleAsync(technician, "technician")) throw new ApiError(400);
            var actor = await users.GetUserAsync(principal) ?? throw new ApiError(401);
            var order = new Order
            {
                CustomerName = request.CustomerName.Trim(), CustomerEmail = request.CustomerEmail.Trim(),
                EquipmentLabel = request.EquipmentLabel.Trim(), EquipmentSerial = request.EquipmentSerial?.Trim() ?? "",
                ReportedIssue = request.ReportedIssue.Trim(), TechnicianId = technician.Id, TechnicianName = technician.DisplayName
            };
            order.Code = $"BC-{order.Id:N}".ToUpperInvariant();
            order.History.Add(new OrderEvent
            {
                OrderId = order.Id, Action = "received", ActorName = actor.DisplayName,
                Detail = "Equipamento recebido.", CreatedAt = order.UpdatedAt
            });
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            return Results.Created($"/api/orders/{order.Id}", OrderProjection.Detail(order));
        }).RequireAuthorization("receptionist");
        group.MapGet("", async (HttpRequest request, BancadaDb db, UserManager<StaffUser> users) =>
        {
            var allowed = new[] { "search", "status", "technicianId", "page", "pageSize" };
            if (request.Query.Keys.Any(k => !allowed.Contains(k)) || request.Query.Any(q => q.Value.Count != 1))
                throw new ApiError(400);
            var search = request.Query["search"].ToString();
            var status = request.Query["status"].ToString();
            var technician = request.Query["technicianId"].ToString();
            var page = Page(request, "page", 1, int.MaxValue);
            var size = Page(request, "pageSize", 20, 50);
            if (search.Length > 100 || (status.Length > 0 && !Order.Statuses.Contains(status))) throw new ApiError(400);
            if (technician.Length > 0)
            {
                var user = await users.FindByIdAsync(technician);
                if (user is null || !await users.IsInRoleAsync(user, "technician")) throw new ApiError(400);
            }
            var query = db.Orders.AsNoTracking();
            if (search.Length > 0) query = query.Where(o => o.Code.Contains(search) || o.EquipmentLabel.Contains(search)
                || o.CustomerName.Contains(search));
            if (status.Length > 0) query = query.Where(o => o.Status == status);
            if (technician.Length > 0) query = query.Where(o => o.TechnicianId == technician);
            var total = await query.CountAsync();
            var offset = (long)(page - 1) * size;
            var items = offset > int.MaxValue ? [] : await query.OrderByDescending(o => o.UpdatedAt).ThenBy(o => o.Id)
                .Skip((int)offset).Take(size).ToListAsync();
            return Results.Ok(new { items = items.Select(OrderProjection.Summary), total, page, pageSize = size });
        });
        group.MapGet("/{id:guid}", async (Guid id, OrderStore store) => Results.Ok(OrderProjection.Detail(await store.LoadAsync(id))));
        group.MapPost("/{id:guid}/diagnosis", async (Guid id, DiagnosisRequest request, OrderStore store,
            UserManager<StaffUser> users, ClaimsPrincipal principal) =>
        {
            var user = await users.GetUserAsync(principal) ?? throw new ApiError(401);
            return Results.Ok(await store.MutateAsync(id, request.ExpectedVersion, order =>
                order.UpdateDiagnosis(user.Id, Role(principal), request.ExpectedVersion, request.Diagnosis, user.DisplayName)));
        });
        group.MapPost("/{id:guid}/transitions", async (Guid id, TransitionRequest request, OrderStore store,
            UserManager<StaffUser> users, ClaimsPrincipal principal) =>
        {
            var user = await users.GetUserAsync(principal) ?? throw new ApiError(401);
            return Results.Ok(await store.MutateAsync(id, request.ExpectedVersion, order =>
                order.ApplyTransition(user.Id, Role(principal), request.Target, request.ExpectedVersion, request.Reason,
                    user.DisplayName, DateTimeOffset.UtcNow)));
        });
    }

    public static string Role(ClaimsPrincipal principal) => principal.IsInRole("receptionist") ? "receptionist" : "technician";
    private static int Page(HttpRequest request, string key, int fallback, int maximum)
    {
        if (!request.Query.ContainsKey(key)) return fallback;
        if (!int.TryParse(request.Query[key], out var value) || value < 1 || value > maximum) throw new ApiError(400);
        return value;
    }
}
