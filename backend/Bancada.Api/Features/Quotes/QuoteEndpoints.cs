using System.Security.Claims;
using Bancada.Api.Features.Access;
using Bancada.Api.Features.Orders;
using Bancada.Api.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace Bancada.Api.Features.Quotes;

public static class QuoteEndpoints
{
    public static void MapQuotes(this WebApplication app)
    {
        app.MapPost("/api/orders/{id:guid}/quotes", async (Guid id, PublishQuoteRequest request,
            OrderStore store, BancadaDb db, UserManager<StaffUser> users, ClaimsPrincipal principal) =>
        {
            var actor = await users.GetUserAsync(principal) ?? throw new ApiError(401);
            return Results.Ok(await store.MutateAsync(id, request.ExpectedVersion, order =>
            {
                order.RequireTechnician(actor.Id, OrderEndpoints.Role(principal));
                if (order.Status is not ("diagnosing" or "awaitingApproval" or "approved")
                    || string.IsNullOrWhiteSpace(order.Diagnosis)) throw new ApiError(409);
                if (request.Items is null || request.Items.Count is < 1 or > 100 || request.Items.Any(i => i is null))
                    throw new ApiError(400);
                var quote = new Quote
                {
                    OrderId = id, Revision = order.Quotes.Count + 1, PublishedAt = BusinessTime.UtcNow,
                    Items = request.Items.Select((i, position) => new QuoteItem
                    {
                        Position = position, Description = i.Description, Kind = i.Kind,
                        Quantity = i.Quantity, UnitPrice = Quote.Price(i.UnitPrice)
                    }).ToList()
                };
                quote.Total = Quote.CalculateTotal(quote.Items);
                order.Quotes.Add(quote);
                db.Quotes.Add(quote);
                order.Status = "awaitingApproval";
                order.Changed("quotePublished", actor.DisplayName, $"Orcamento revisao {quote.Revision} publicado.", quote.PublishedAt);
            }));
        }).RequireAuthorization("staff");
    }
}
