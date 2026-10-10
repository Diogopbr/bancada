namespace Bancada.Api.Features.Access;

public sealed class DemoWorkspace
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddHours(24);
    public bool Revoked { get; set; }
}

public sealed class DemoVisitor
{
    public string TokenHash { get; set; } = "";
    public Guid? WorkspaceId { get; set; }
}
