using Bancada.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bancada.Api.Features.Orders;

public sealed class OrderStore(BancadaDb db)
{
    public async Task<Order> LoadAsync(Guid id, bool locked = false)
    {
        var query = locked
            ? db.Orders.FromSqlInterpolated($"SELECT * FROM \"Orders\" WHERE \"Id\" = {id} FOR UPDATE")
            : db.Orders.Where(o => o.Id == id);
        var order = await query.SingleOrDefaultAsync() ?? throw new ApiError(404);
        await db.Entry(order).Collection(o => o.History).LoadAsync();
        await db.Entry(order).Collection(o => o.Quotes).Query().Include(q => q.Items).LoadAsync();
        return order;
    }

    public async Task<OrderDetail> MutateAsync(Guid id, int expectedVersion, Action<Order> change)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var order = await LoadAsync(id, true);
        order.CheckVersion(expectedVersion);
        var existingEvents = order.History.Select(e => e.Id).ToHashSet();
        change(order);
        foreach (var entry in order.History.Where(e => !existingEvents.Contains(e.Id)))
            db.OrderEvents.Add(entry);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return OrderProjection.Detail(order);
    }
}
