using System.Globalization;
using System.Text.RegularExpressions;
using Bancada.Api.Features.Orders;

namespace Bancada.Api.Features.Quotes;

public sealed class Quote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public int Revision { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public decimal Total { get; set; }
    public string? Decision { get; set; }
    public List<QuoteItem> Items { get; set; } = [];

    public static decimal CalculateTotal(IReadOnlyList<QuoteItem> items)
    {
        if (items.Count is < 1 or > 100) throw new ApiError(400);
        foreach (var item in items)
        {
            Validation.Text(item.Description, 1, 200);
            if (item.Kind is not ("part" or "labor") || item.Quantity is < 1 or > 999
                || item.UnitPrice < 0 || item.UnitPrice > 999999.99m
                || decimal.Round(item.UnitPrice, 2) != item.UnitPrice) throw new ApiError(400);
        }
        return items.Sum(item => item.Quantity * item.UnitPrice);
    }

    public static decimal Price(string? value)
    {
        if (value is null || value.Length > 12 || !Regex.IsMatch(value, @"\A[0-9]+(?:\.[0-9]{1,2})?\z",
                RegexOptions.CultureInvariant)
            || !decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var price)
            || price > 999999.99m) throw new ApiError(400);
        return price;
    }
}

public sealed class QuoteItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuoteId { get; set; }
    public int Position { get; set; }
    public string Description { get; set; } = "";
    public string Kind { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
