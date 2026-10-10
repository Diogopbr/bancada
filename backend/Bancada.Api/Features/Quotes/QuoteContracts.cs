using System.Globalization;

namespace Bancada.Api.Features.Quotes;

public sealed record PublishQuoteRequest(int ExpectedVersion, List<QuoteItemRequest>? Items);
public sealed record QuoteItemRequest(string Description, string Kind, int Quantity, string UnitPrice);
public sealed record QuoteResponse(int Revision, QuoteItemResponse[] Items, string Total, DateTimeOffset PublishedAt, string? Decision);
public sealed record QuoteItemResponse(string Description, string Kind, int Quantity, string UnitPrice);

public static class QuoteProjection
{
    public static QuoteResponse? Current(IEnumerable<Quote> quotes)
    {
        var quote = quotes.OrderByDescending(q => q.Revision).FirstOrDefault();
        return quote is null ? null : new(quote.Revision,
            quote.Items.OrderBy(i => i.Position).Select(i => new QuoteItemResponse(i.Description, i.Kind, i.Quantity,
                i.UnitPrice.ToString("F2", CultureInfo.InvariantCulture))).ToArray(),
            quote.Total.ToString("F2", CultureInfo.InvariantCulture), quote.PublishedAt, quote.Decision);
    }
}
