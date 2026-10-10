using Bancada.Api.Features.History;
using Bancada.Api.Features.Quotes;
using Bancada.Api.Infrastructure;

namespace Bancada.Api.Features.Orders;

public sealed class Order
{
    public static readonly string[] Statuses =
        ["received", "diagnosing", "awaitingApproval", "approved", "repairing", "ready", "delivered", "cancelled"];
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? WorkspaceId { get; set; }
    public string Code { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string EquipmentLabel { get; set; } = "";
    public string EquipmentSerial { get; set; } = "";
    public string ReportedIssue { get; set; } = "";
    public string TechnicianId { get; set; } = "";
    public string TechnicianName { get; set; } = "";
    public string Status { get; set; } = "received";
    public string Diagnosis { get; set; } = "";
    public int Version { get; set; } = 1;
    public DateTimeOffset UpdatedAt { get; set; } = BusinessTime.UtcNow;
    public List<OrderEvent> History { get; set; } = [];
    public List<Quote> Quotes { get; set; } = [];

    public void CheckVersion(int expectedVersion)
    {
        if (Version != expectedVersion) throw new ApiError(409);
    }

    public void RequireTechnician(string actorId, string role)
    {
        if (role != "technician" || TechnicianId != actorId) throw new ApiError(403);
    }

    public void ApplyTransition(string actorId, string role, string target, int expectedVersion,
        string? reason, string actorName, DateTimeOffset now)
    {
        CheckVersion(expectedVersion);
        if (!Statuses.Contains(target)) throw new ApiError(400);
        if (target == "cancelled")
        {
            if (role != "receptionist") throw new ApiError(403);
            if (Status is not ("received" or "diagnosing" or "awaitingApproval" or "approved")) throw new ApiError(409);
            Validation.Text(reason, 1, 2000);
            Status = target;
            Changed("cancelled", actorName, $"Ordem cancelada: {reason!.Trim()}", now);
            return;
        }
        if (target == "delivered")
        {
            if (role != "receptionist") throw new ApiError(403);
            if (Status != "ready") throw new ApiError(409);
        }
        else
        {
            RequireTechnician(actorId, role);
            if (target == "repairing" && Quotes.OrderByDescending(q => q.Revision).FirstOrDefault()?.Decision != "accepted")
                throw new ApiError(409);
            if (!((Status == "received" && target == "diagnosing")
                || (Status == "approved" && target == "repairing")
                || (Status == "repairing" && target == "ready"))) throw new ApiError(409);
        }
        Status = target;
        var (action, detail) = target switch
        {
            "diagnosing" => ("diagnosisUpdated", "Diagnostico iniciado."),
            "repairing" => ("repairStarted", "Reparo iniciado."),
            "ready" => ("repairCompleted", "Reparo concluido."),
            _ => ("delivered", "Equipamento entregue.")
        };
        Changed(action, actorName, detail, now);
    }

    public void UpdateDiagnosis(string actorId, string role, int expectedVersion, string diagnosis, string actorName)
    {
        CheckVersion(expectedVersion);
        RequireTechnician(actorId, role);
        if (Status is not ("received" or "diagnosing")) throw new ApiError(409);
        Validation.Text(diagnosis, 1, 4000);
        Diagnosis = diagnosis.Trim();
        Status = "diagnosing";
        Changed("diagnosisUpdated", actorName, "Diagnostico atualizado.", BusinessTime.UtcNow);
    }

    public void Changed(string action, string actorName, string detail, DateTimeOffset now)
    {
        Version++;
        now = BusinessTime.Normalize(now);
        UpdatedAt = now;
        History.Add(new OrderEvent { OrderId = Id, Action = action, ActorName = actorName, Detail = detail, CreatedAt = now });
    }
}

public static class Validation
{
    public static void Text(string? value, int minimum, int maximum)
    {
        if (value is null || value.Trim().Length < minimum || value.Length > maximum) throw new ApiError(400);
    }
}
