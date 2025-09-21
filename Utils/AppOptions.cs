using System;
using System.Collections.Generic;

namespace _0900_OdywardRoleManager.Utils;

public sealed class AzureAdOptions
{
    public string ClientId { get; set; } = string.Empty;

    public string Authority { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ClientId))
        {
            throw new InvalidOperationException("AzureAd:ClientId est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(Authority))
        {
            throw new InvalidOperationException("AzureAd:Authority est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(RedirectUri))
        {
            throw new InvalidOperationException("AzureAd:RedirectUri est obligatoire.");
        }
    }
}

public sealed class GraphOptions
{
    public IReadOnlyCollection<string> Scopes { get; set; } = Array.Empty<string>();

    public void Validate()
    {
        if (Scopes.Count == 0)
        {
            throw new InvalidOperationException("Graph:Scopes doit contenir au moins une valeur.");
        }
    }
}
