using Bancada.Api.Features.Quotes;

namespace Bancada.Api.Features.Orders;

public sealed record CreateOrderRequest(string CustomerName, string CustomerEmail, string EquipmentLabel,
    string? EquipmentSerial, string ReportedIssue, string TechnicianId);
public sealed record DiagnosisRequest(int ExpectedVersion, string Diagnosis);
public sealed record TransitionRequest(int ExpectedVersion, string Target, string? Reason);
public sealed record OrderSummary(Guid Id, string Code, string CustomerName, string EquipmentLabel,
    string Status, string TechnicianId, string TechnicianName, DateTimeOffset UpdatedAt, int Version);
public sealed record OrderDetail(Guid Id, string Code, string CustomerName, string EquipmentLabel,
    string Status, string TechnicianId, string TechnicianName, DateTimeOffset UpdatedAt, int Version,
    string CustomerEmail, string EquipmentSerial, string ReportedIssue, string Diagnosis, QuoteResponse? CurrentQuote,
    object[] History);

public static class OrderProjection
{
    public static OrderSummary Summary(Order order) => new(order.Id, order.Code, order.CustomerName, order.EquipmentLabel,
        order.Status, order.TechnicianId, order.TechnicianName, order.UpdatedAt, order.Version);
    public static OrderDetail Detail(Order order) => new(order.Id, order.Code, order.CustomerName, order.EquipmentLabel,
        order.Status, order.TechnicianId, order.TechnicianName, order.UpdatedAt, order.Version,
        order.CustomerEmail, order.EquipmentSerial, order.ReportedIssue, order.Diagnosis, QuoteProjection.Current(order.Quotes), Events(order, false));
    public static object[] Events(Order order, bool customer) => order.History.OrderBy(e => e.CreatedAt).ThenBy(e => e.Id)
        .Select(e => (object)new
        {
            e.Id, e.Action, e.ActorName, e.CreatedAt,
            detail = customer && e.Action == "cancelled" ? "Ordem cancelada." : e.Detail
        }).ToArray();
}
