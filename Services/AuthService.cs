using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using _0900_OdywardRoleManager.Models;
using _0900_OdywardRoleManager.Utils;
using Microsoft.Identity.Client;
using Serilog;

namespace _0900_OdywardRoleManager.Services;

public sealed class AuthService : IAuthService
{
    private readonly IPublicClientApplication _pca;
    private readonly string[] _scopes;
    private readonly ILogger _logger;

    public AuthService(AzureAdOptions azureOptions, GraphOptions graphOptions, ILogger logger)
    {
        azureOptions.Validate();
        graphOptions.Validate();

        _logger = logger;
        _scopes = graphOptions.Scopes.ToArray();
        _pca = PublicClientApplicationBuilder
            .Create(azureOptions.ClientId)
            .WithAuthority(azureOptions.Authority)
            .WithRedirectUri(azureOptions.RedirectUri)
            .Build();
    }

    public async Task<AuthContext> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        var silent = await TryAcquireTokenSilentInternalAsync(cancellationToken).ConfigureAwait(false);
        if (silent is not null)
        {
            return BuildContext(silent);
        }

        _logger.Information("Authentification interactive MSAL en cours. Un administrateur doit autoriser l'application lors de la première connexion.");
        var interactive = await _pca
            .AcquireTokenInteractive(_scopes)
            .WithPrompt(Prompt.SelectAccount)
            .WithExtraScopesToConsent(_scopes) // Force la demande de consentement pour tous les scopes
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        _logger.Information("Authentification réussie pour {Upn} dans le tenant {TenantId}.", 
            interactive.Account.Username, 
            interactive.Account.HomeAccountId?.TenantId);

        return BuildContext(interactive);
    }

    public async Task<AuthContext?> TryAcquireTokenSilentAsync(CancellationToken cancellationToken = default)
    {
        var result = await TryAcquireTokenSilentInternalAsync(cancellationToken).ConfigureAwait(false);
        return result is null ? null : BuildContext(result);
    }

    private async Task<AuthenticationResult?> TryAcquireTokenSilentInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            var accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
            var account = accounts.FirstOrDefault();
            if (account is null)
            {
                return null;
            }

            _logger.Information("Tentative d'authentification MSAL silencieuse pour {Upn}.", account.Username);
            return await _pca.AcquireTokenSilent(_scopes, account)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (MsalUiRequiredException ex)
        {
            _logger.Warning(ex, "Authentification silencieuse impossible : consentement requis. Un administrateur doit autoriser l'application.");
            return null;
        }
    }

    private static AuthContext BuildContext(AuthenticationResult result)
    {
        // Extraction des claims du token ID pour une récupération plus robuste
        var idTokenClaims = result.ClaimsPrincipal.Claims.ToDictionary(c => c.Type, c => c.Value);
        
        // Récupération du TenantId depuis le claim 'tid' (plus fiable que HomeAccountId)
        var tenantId = idTokenClaims.ContainsKey("tid") ? idTokenClaims["tid"] :
                       result.Account?.HomeAccountId?.TenantId ?? string.Empty;
        
        // Récupération de l'ObjectId depuis le claim 'oid' (plus fiable que HomeAccountId)
        var objectId = idTokenClaims.ContainsKey("oid") ? idTokenClaims["oid"] :
                       result.Account?.HomeAccountId?.ObjectId ?? string.Empty;
        
        // Récupération de l'UPN depuis les claims (preferred_username ou upn)
        var upn = idTokenClaims.ContainsKey("preferred_username") ? idTokenClaims["preferred_username"] :
                  idTokenClaims.ContainsKey("upn") ? idTokenClaims["upn"] :
                  result.Account?.Username ?? string.Empty;
        
        return new AuthContext(result.AccessToken, tenantId, objectId, upn, result.ExpiresOn);
    }
}
