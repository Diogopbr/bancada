using System.Security.Claims;
using Bancada.Api.Features.Orders;
using Bancada.Api.Features.Quotes;
using Bancada.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Bancada.Api.Features.Access;

public sealed record AccessRequest(int ExpectedVersion);
public sealed record CustomerSessionRequest(string Token);

public static class CustomerEndpoints
{
    public static void MapCustomer(this WebApplication app)
    {
        app.MapPost("/api/orders/{id:guid}/customer-access", async (Guid id, AccessRequest request,
            BancadaDb db, OrderStore store) =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var order = await store.LoadAsync(id, true);
            order.CheckVersion(request.ExpectedVersion);
            await db.CustomerAccesses.Where(a => a.OrderId == id && !a.Revoked)
                .ExecuteUpdateAsync(update => update.SetProperty(a => a.Revoked, true));
            var token = CustomerAccess.Token();
            var access = new CustomerAccess
            {
                OrderId = id, TokenHash = CustomerAccess.Hash(token), ExpiresAt = BusinessTime.UtcNow.AddDays(7)
            };
            db.CustomerAccesses.Add(access);
            order.Version++;
            order.UpdatedAt = BusinessTime.UtcNow;
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Results.Ok(new { url = "/acompanhar#token=" + token, access.ExpiresAt });
        }).RequireAuthorization("receptionist");
        app.MapDelete("/api/orders/{id:guid}/customer-access", async (Guid id, BancadaDb db, OrderStore store) =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            var order = await store.LoadAsync(id, true);
            var count = await db.CustomerAccesses.Where(a => a.OrderId == id && !a.Revoked)
                .ExecuteUpdateAsync(update => update.SetProperty(a => a.Revoked, true));
            if (count > 0)
            {
                order.Version++;
                order.UpdatedAt = BusinessTime.UtcNow;
                await db.SaveChangesAsync();
            }
            await transaction.CommitAsync();
            return Results.NoContent();
        }).RequireAuthorization("receptionist");

        var customer = app.MapGroup("/api/customer").RequireRateLimiting("customer");
        customer.MapPost("/session", async (CustomerSessionRequest request, HttpContext http,
            BancadaDb db, CustomerAccessService accessService) =>
        {
            if (request.Token is null || request.Token.Length != 43) throw new ApiError(404);
            var hash = CustomerAccess.Hash(request.Token);
            var locator = await db.CustomerAccesses.AsNoTracking().SingleOrDefaultAsync(a => a.TokenHash == hash)
                ?? throw new ApiError(404);
            var expiresAt = await accessService.WithOrderAsync(locator.Id, (_, grant) => Task.FromResult(grant.ExpiresAt));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("accessId", locator.Id.ToString())], CustomerAccessService.Scheme));
            await http.SignInAsync(CustomerAccessService.Scheme, principal,
                new AuthenticationProperties { IsPersistent = false, ExpiresUtc = expiresAt, AllowRefresh = false });
            return Results.NoContent();
        });
        customer.MapGet("/order", async (HttpContext http, CustomerAccessService service) =>
            Results.Ok(await service.WithOrderAsync(await CustomerAccessService.SessionAsync(http),
                (order, _) => Task.FromResult(CustomerDecisionService.Project(order)))));
        customer.MapPost("/decision", async (CustomerDecisionRequest request, HttpContext http, CustomerDecisionService decisions) =>
            Results.Ok(await decisions.DecideAsync(await CustomerAccessService.SessionAsync(http),
                request.QuoteRevision, request.Decision, http.RequestAborted)));
    }
}
