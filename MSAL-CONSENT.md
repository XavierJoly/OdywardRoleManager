# Guide du consentement administrateur - Odyward Role Manager

## 🔐 Authentification Multi-tenant MSAL

L'application **Odyward Role Manager** utilise une authentification MSAL multi-tenant qui respecte ces principes :

### ✅ Processus d'authentification

1. **Configuration multi-tenant**
   - Authority : `https://login.microsoftonline.com/common`
   - Pas de TenantId codé en dur
   - Support de tous les tenants Azure AD

2. **Extraction dynamique des informations**
   - **TenantId** : Récupéré depuis le claim `tid` du token
   - **UserObjectId** : Récupéré depuis le claim `oid` du token  
   - **UserPrincipalName** : Récupéré depuis `preferred_username` ou `upn`

3. **Scopes requis**
   ```
   - Directory.ReadWrite.All
   - RoleManagement.ReadWrite.Directory
   - User.Read.All
   ```

### 🔒 Consentement administrateur

#### Première connexion dans un tenant
Lors de la première utilisation de l'application dans un tenant client :

1. **L'administrateur se connecte** avec son compte
2. **Microsoft affiche l'écran de consentement** avec les permissions demandées
3. **L'administrateur doit cliquer "Accepter"** pour autoriser l'application
4. **L'application reçoit les tokens** avec les informations du tenant

#### Messages affichés à l'utilisateur

```
🔐 Authentification requise - Un administrateur doit autoriser l'application lors de la première connexion dans ce tenant.
✅ Connecté - Utilisateur: admin@client.com, Tenant: abc123-def456-...
```

#### Consentements ultérieurs
- Les connexions suivantes du même utilisateur → **Authentification silencieuse**
- Autres administrateurs du même tenant → **Pas de nouveau consentement requis**
- Nouveau tenant → **Nouveau consentement administrateur requis**

### 🏗️ Architecture technique

#### AuthContext dynamique
```csharp
public class AuthContext
{
    public string AccessToken { get; }      // Token d'accès Graph API
    public string TenantId { get; }         // ID du tenant client (dynamique)
    public string UserObjectId { get; }     // ID de l'utilisateur connecté
    public string UserPrincipalName { get; } // Email de l'utilisateur
    public DateTimeOffset ExpiresOn { get; } // Expiration du token
}
```

#### Extraction des claims
```csharp
private static AuthContext BuildContext(AuthenticationResult result)
{
    var idTokenClaims = result.ClaimsPrincipal.Claims.ToDictionary(c => c.Type, c => c.Value);
    
    var tenantId = idTokenClaims.ContainsKey("tid") ? idTokenClaims["tid"] : "";
    var objectId = idTokenClaims.ContainsKey("oid") ? idTokenClaims["oid"] : "";
    var upn = idTokenClaims.ContainsKey("preferred_username") 
        ? idTokenClaims["preferred_username"] 
        : idTokenClaims.ContainsKey("upn") ? idTokenClaims["upn"] : "";
    
    return new AuthContext(result.AccessToken, tenantId, objectId, upn, result.ExpiresOn);
}
```

### 📋 Configuration App Registration Azure AD

Pour que l'application fonctionne en mode multi-tenant :

1. **Portail Azure** > **Azure Active Directory** > **Inscriptions d'applications**

2. **Nouvelle inscription** :
   - **Nom** : `Odyward Role Manager`
   - **Types de comptes pris en charge** : ✅ `Comptes dans un annuaire d'organisation (Tout annuaire Azure AD - Multilocataire)`
   - **URI de redirection** : `Client public/natif (mobile et desktop)` → `http://localhost`

3. **Permissions API** :
   ```
   Microsoft Graph (Application permissions) :
   - Directory.ReadWrite.All
   - RoleManagement.ReadWrite.Directory  
   - User.Read.All
   
   ⚠️ Statut : "Consentement de l'administrateur requis"
   ```

4. **Authentification** :
   - **Plateformes** : Client public/natif
   - **URI de redirection** : `http://localhost`
   - **Flux** : Autoriser les flux de clients publics ✅

### 🔍 Vérification du consentement

#### Côté administrateur client
L'administrateur peut vérifier les applications autorisées :
- **Portail Azure** > **Azure Active Directory** > **Applications d'entreprise**
- Rechercher "Odyward Role Manager"
- Voir les permissions accordées

#### Côté développeur  
Dans le portail Azure de l'App Registration :
- **Applications d'entreprise** > **Odyward Role Manager** 
- **Permissions** : Voir quels tenants ont donné leur consentement

### 🚨 Dépannage consentement

#### "Besoin d'un admin avec droits RoleManagement"
➡️ L'utilisateur connecté n'est pas administrateur global/des rôles

#### "Consentement requis"  
➡️ Première connexion dans ce tenant → Normal, consentement admin requis

#### "Application non trouvée"
➡️ Vérifier que l'App Registration est bien configurée en multi-tenant

#### Token expiré
➡️ L'application gère automatiquement le renouvellement silencieux

### ✅ Points clés de sécurité

- ✅ **Pas de Client Secret** : Application publique uniquement
- ✅ **Multi-tenant sécurisé** : Chaque tenant contrôle son consentement  
- ✅ **Permissions granulaires** : Uniquement les scopes nécessaires
- ✅ **Extraction dynamique** : Pas de configuration par tenant
- ✅ **Audit complet** : Traçabilité par tenant et utilisateur

---

Cette architecture garantit que **chaque organisation client garde le contrôle** sur l'autorisation de l'application, tout en permettant une **expérience utilisateur fluide** après le consentement initial.