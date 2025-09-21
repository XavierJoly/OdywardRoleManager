using System;
using System.Collections.Generic;

namespace _0900_OdywardRoleManager.Models;

public sealed class AuditEntry
{
    public string Email { get; init; } = string.Empty;

    public IReadOnlyCollection<string> RolesBefore { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> RolesAfter { get; init; } = Array.Empty<string>();

    public string TenantId { get; init; } = string.Empty;

    public string ActorUpn { get; init; } = string.Empty;

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
