using Microsoft.AspNetCore.Identity;

namespace Bancada.Api.Features.Access;

public sealed class StaffUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public Guid? WorkspaceId { get; set; }
    public string? DemoRole { get; set; }
}
