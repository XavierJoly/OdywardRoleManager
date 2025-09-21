using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using _0900_OdywardRoleManager.Models;
using _0900_OdywardRoleManager.Utils;
using Serilog;

namespace _0900_OdywardRoleManager.Services;

public sealed class GraphService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly RoleCatalog _roleCatalog;
    private readonly ExportService _exportService;
    private readonly ILogger _logger;
    private bool _disposed;

    public GraphService(RoleCatalog roleCatalog, ExportService exportService, ILogger logger, HttpMessageHandler? httpMessageHandler = null)
    {
        _roleCatalog = roleCatalog;
        _exportService = exportService;
        _logger = logger;
        _httpClient = httpMessageHandler is null ? new HttpClient() : new HttpClient(httpMessageHandler, disposeHandler: false);
        _httpClient.BaseAddress = new Uri(Constants.GraphBaseUrl);
    }

    public async Task<DirectoryUser?> GetUserByEmailAsync(string email, AuthContext authContext, CancellationToken cancellationToken)
    {
        var encodedEmail = Uri.EscapeDataString(email);
        var relativeUrl = $"/users/{encodedEmail}?$select=id,displayName,userPrincipalName,mail";

        using var response = await SendAsync(() => CreateRequest(HttpMethod.Get, relativeUrl, authContext.AccessToken), cancellationToken).ConfigureAwait(false);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        var root = document.RootElement;
        if (!root.TryGetProperty("id", out var idProperty))
        {
            return null;
        }

        return new DirectoryUser
        {
            Id = idProperty.GetString() ?? string.Empty,
            DisplayName = root.GetPropertyOrDefault("displayName"),
            UserPrincipalName = root.GetPropertyOrDefault("userPrincipalName"),
            Mail = root.GetPropertyOrDefault("mail"),
        };
    }

    public async Task<IReadOnlyCollection<string>> GetAssignedRoleDisplayNamesAsync(string userId, AuthContext authContext, CancellationToken cancellationToken)
    {
        var roles = new List<string>();
        var relativeUrl = $"/users/{userId}/transitiveMemberOf/microsoft.graph.directoryRole?$select=id,displayName";
        string? nextLink = relativeUrl;

        while (!string.IsNullOrEmpty(nextLink))
        {
            using var response = await SendAsync(() => CreateRequest(HttpMethod.Get, nextLink!, authContext.AccessToken), cancellationToken).ConfigureAwait(false);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (document.RootElement.TryGetProperty("value", out var valueElement))
            {
                foreach (var item in valueElement.EnumerateArray())
                {
                    var displayName = item.GetPropertyOrDefault("displayName");
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        roles.Add(displayName);
                    }
                }
            }

            nextLink = document.RootElement.TryGetProperty("@odata.nextLink", out var nextLinkElement)
                ? nextLinkElement.GetString()
                : null;
        }

        return roles;
    }

    public async Task<string> EnsureRoleEnabledAsync(string displayName, AuthContext authContext, CancellationToken cancellationToken)
    {
        var filter = Uri.EscapeDataString($"displayName eq '{displayName.Replace("'", "''", StringComparison.Ordinal)}'");
        using var response = await SendAsync(() => CreateRequest(HttpMethod.Get, $"/directoryRoles?$filter={filter}&$select=id,displayName", authContext.AccessToken), cancellationToken).ConfigureAwait(false);
        await using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
        {
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (document.RootElement.TryGetProperty("value", out var valueElement))
            {
                foreach (var item in valueElement.EnumerateArray())
                {
                    var name = item.GetPropertyOrDefault("displayName");
                    if (string.Equals(name, displayName, StringComparison.OrdinalIgnoreCase))
                    {
                        return item.GetPropertyOrDefault("id");
                    }
                }
            }
        }

        if (!_roleCatalog.TryGetDefinition(displayName, out var definition))
        {
            throw new InvalidOperationException($"Le rôle '{displayName}' n'est pas disponible dans le catalogue.");
        }

        _logger.Information("Activation du rôle directoryRole {DisplayName}.", displayName);

        var payload = new
        {
            roleTemplateId = definition.RoleTemplateId,
        };

        using var creationResponse = await SendAsync(() =>
        {
            var request = CreateRequest(HttpMethod.Post, "/directoryRoles", authContext.AccessToken);
            request.Content = JsonContent.Create(payload);
            return request;
        }, cancellationToken).ConfigureAwait(false);

        await using var responseStream = await creationResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var created = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var roleId = created.RootElement.GetPropertyOrDefault("id");

        if (string.IsNullOrEmpty(roleId))
        {
            throw new InvalidOperationException($"Activation du rôle {displayName} échouée : identifiant introuvable.");
        }

        return roleId;
    }

    public async Task<IReadOnlyCollection<string>> AssignRolesAsync(string userId, IEnumerable<string> displayNames, AuthContext authContext, CancellationToken cancellationToken)
    {
        var assigned = new List<string>();
        foreach (var displayName in displayNames)
        {
            var roleId = await EnsureRoleEnabledAsync(displayName, authContext, cancellationToken).ConfigureAwait(false);
            var payload = new Dictionary<string, string>
            {
                ["@odata.id"] = $"{Constants.GraphBaseUrl}/directoryObjects/{userId}"
            };

            using var response = await SendAsync(() =>
            {
                var request = CreateRequest(HttpMethod.Post, $"/directoryRoles/{roleId}/members/$ref", authContext.AccessToken);
                request.Content = JsonContent.Create(payload);
                return request;
            }, cancellationToken).ConfigureAwait(false);

            assigned.Add(displayName);
            _logger.Information("Rôle {Role} attribué à l'utilisateur {UserId}.", displayName, userId);
        }

        return assigned;
    }

    public async Task<IReadOnlyCollection<string>> RevokeRolesAsync(string userId, IEnumerable<string> displayNames, AuthContext authContext, CancellationToken cancellationToken)
    {
        var revoked = new List<string>();
        foreach (var displayName in displayNames)
        {
            var roleId = await EnsureRoleEnabledAsync(displayName, authContext, cancellationToken).ConfigureAwait(false);
            using var response = await SendAsync(() =>
            {
                var request = CreateRequest(HttpMethod.Delete, $"/directoryRoles/{roleId}/members/{userId}/$ref", authContext.AccessToken);
                return request;
            }, cancellationToken).ConfigureAwait(false);

            revoked.Add(displayName);
            _logger.Information("Rôle {Role} révoqué pour l'utilisateur {UserId}.", displayName, userId);
        }

        return revoked;
    }

    public async Task<IReadOnlyCollection<string>> RevokeAllRolesAttributedByAppAsync(string email, string userId, AuthContext authContext, CancellationToken cancellationToken)
    {
        var entries = await _exportService.ReadAuditEntriesForUserAsync(email, cancellationToken).ConfigureAwait(false);
        if (entries.Count == 0)
        {
            return Array.Empty<string>();
        }

        var toRevoke = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            var before = new HashSet<string>(entry.RolesBefore, StringComparer.OrdinalIgnoreCase);
            foreach (var roleAfter in entry.RolesAfter)
            {
                if (!before.Contains(roleAfter))
                {
                    toRevoke.Add(roleAfter);
                }
            }
        }

        if (toRevoke.Count == 0)
        {
            return Array.Empty<string>();
        }

        await RevokeRolesAsync(userId, toRevoke, authContext, cancellationToken).ConfigureAwait(false);
        return toRevoke.ToArray();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _httpClient.Dispose();
        _disposed = true;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativeOrAbsoluteUrl, string accessToken)
    {
        var request = new HttpRequestMessage(method, relativeOrAbsoluteUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> requestFactory, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = requestFactory();
            var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            if ((int)response.StatusCode == 429 && attempt < maxAttempts)
            {
                var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.Warning("Graph API a renvoyé 429. Nouvelle tentative dans {Delay}.", delay);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new GraphApiException(response.StatusCode, "Besoin d’un compte admin avec droits RoleManagement.ReadWrite.Directory.");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new GraphApiException(response.StatusCode, body);
        }

        throw new GraphApiException(HttpStatusCode.TooManyRequests, "Graph API: nombre maximal de tentatives atteint.");
    }
}

file static class JsonElementExtensions
{
    public static string GetPropertyOrDefault(this JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }
}
