namespace Bancada.Api.Features.History;

public sealed class OrderEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public string Action { get; set; } = "";
    public string ActorName { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string Detail { get; set; } = "";
}
