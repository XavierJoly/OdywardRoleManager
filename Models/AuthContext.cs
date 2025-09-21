using System;

namespace _0900_OdywardRoleManager.Models;

public sealed class AuthContext
{
    public AuthContext(string accessToken, string tenantId, string userObjectId, string userPrincipalName, DateTimeOffset expiresOn)
    {
        AccessToken = accessToken;
        TenantId = tenantId;
        UserObjectId = userObjectId;
        UserPrincipalName = userPrincipalName;
        ExpiresOn = expiresOn;
    }

    public string AccessToken { get; }

    public string TenantId { get; }

    public string UserObjectId { get; }

    public string UserPrincipalName { get; }

    public DateTimeOffset ExpiresOn { get; }
}
