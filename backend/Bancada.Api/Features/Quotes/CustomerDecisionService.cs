using Bancada.Api.Features.Access;
using Bancada.Api.Features.Orders;

namespace Bancada.Api.Features.Quotes;

public sealed record CustomerOrderResponse(string Code, string EquipmentLabel, string Status, int Version,
    QuoteResponse? CurrentQuote, object[] History);
public sealed record CustomerDecisionRequest(int QuoteRevision, string Decision);

public sealed class CustomerDecisionService(CustomerAccessService access)
{
    public Task<CustomerOrderResponse> DecideAsync(Guid accessId, int quoteRevision, string decision,
        CancellationToken cancellationToken) =>
        access.WithOrderAsync(accessId, (order, _) =>
        {
            if (decision is not ("accepted" or "rejected") || quoteRevision < 1) throw new ApiError(400);
            var quote = order.Quotes.OrderByDescending(q => q.Revision).FirstOrDefault();
            if (quote is null || quote.Revision != quoteRevision) throw new ApiError(409);
            if (quote.Decision is not null)
            {
                if (quote.Decision != decision) throw new ApiError(409);
                return Task.FromResult(Project(order));
            }
            if (order.Status != "awaitingApproval") throw new ApiError(409);
            quote.Decision = decision;
            order.Status = decision == "accepted" ? "approved" : "diagnosing";
            order.Changed(decision == "accepted" ? "quoteAccepted" : "quoteRejected", "Cliente",
                $"Orcamento revisao {quoteRevision} {(decision == "accepted" ? "aceito" : "rejeitado")}.", DateTimeOffset.UtcNow);
            return Task.FromResult(Project(order));
        }, cancellationToken);

    public static CustomerOrderResponse Project(Order order) => new(order.Code, order.EquipmentLabel, order.Status,
        order.Version, QuoteProjection.Current(order.Quotes), OrderProjection.Events(order, true));
}
