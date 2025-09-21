# Odyward Role Manager

![Avalonia](https://img.shields.io/badge/Avalonia-11.3.6-purple)
![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![Platform](https://img.shields.io/badge/Platform-Windows%20|%20macOS%20|%20Linux-green)

Application desktop cross-platform pour la gestion des rôles Entra ID (Azure AD). Permet de consulter, attribuer, révoquer et effectuer des rollbacks sur les rôles administratifs d'utilisateurs de manière sécurisée et auditable.

## 🎯 Fonctionnalités

- **Multi-tenant** : Support de plusieurs tenants Azure AD sans configuration codée en dur
- **Authentification MSAL** : Authentification interactive sécurisée avec Microsoft Identity
- **Gestion des rôles** : Attribution et révocation de rôles administratifs Entra ID
- **Audit complet** : Traçabilité de toutes les opérations avec export JSON
- **Rollback intelligent** : Annulation automatique des rôles attribués par l'application
- **Interface moderne** : UI Fluent Design avec support thème clair/sombre
- **Cross-platform** : Windows, macOS et Linux

## 🚀 Prérequis

- **.NET 8 SDK** ou supérieur
- **Application Azure AD** enregistrée en mode multi-tenant avec les permissions requises
- **Compte administrateur** avec droits `RoleManagement.ReadWrite.Directory`

### Permissions Azure AD requises

Votre application Azure AD doit avoir ces permissions accordées avec **consentement administrateur** :
- `Directory.ReadWrite.All`
- `RoleManagement.ReadWrite.Directory`
- `User.Read.All`

> **Important** : Au premier login dans chaque tenant client, un administrateur doit accepter le consentement pour ces permissions.

## ⚙️ Configuration

### 1. Enregistrement de l'application Azure AD

1. Allez dans le **Portail Azure** > **Azure Active Directory** > **Inscriptions d'applications**
2. Cliquez sur **Nouvelle inscription**
3. Configurez :
   - **Nom** : `Odyward Role Manager`
   - **Types de comptes pris en charge** : `Comptes dans un annuaire d'organisation (Tout annuaire Azure AD - Multilocataire)`
   - **URI de redirection** : `Client public/natif (mobile et desktop)` → `http://localhost`

4. Dans **Permissions API**, ajoutez les permissions Microsoft Graph :
   - `Directory.ReadWrite.All` (Admin)
   - `RoleManagement.ReadWrite.Directory` (Admin)
   - `User.Read.All` (Admin)

5. **Important** : Un administrateur global doit accorder le consentement admin

### 2. Configuration de l'application

Modifiez le fichier `appsettings.json` :

```json
{
  "AzureAd": {
    "ClientId": "VOTRE_CLIENT_ID_ICI",
    "Authority": "https://login.microsoftonline.com/common",
    "RedirectUri": "http://localhost"
  },
  "Graph": {
    "Scopes": [
      "Directory.ReadWrite.All",
      "RoleManagement.ReadWrite.Directory",
      "User.Read.All"
    ]
  }
}
```

## 🔧 Installation et compilation

### Développement

```bash
# Cloner et se déplacer dans le projet
cd 0900_OdywardRoleManager

# Restaurer les dépendances
dotnet restore

# Compiler
dotnet build

# Exécuter les tests
dotnet test

# Lancer l'application
dotnet run
```

### Production

Utilisez les scripts de build fournis :

#### Windows
```cmd
# Avec le script batch
build-windows.bat

# Ou directement
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

#### macOS
```bash
# Avec le script bash (crée un bundle .app)
./build-macos.sh

# Ou directement
dotnet publish -c Release -r osx-x64 --self-contained true /p:PublishSingleFile=true
```

#### Linux
```bash
# Avec le script bash
./build-linux.sh

# Ou directement
dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true
```

## 📖 Utilisation

### 1. Première connexion
- Lancez l'application
- L'authentification MSAL se déclenche automatiquement au premier usage
- **Premier login dans un tenant** : Microsoft affichera l'écran de consentement administrateur
- Authentifiez-vous avec un compte administrateur du tenant client
- Acceptez les permissions demandées
- L'application récupère automatiquement :
  - **TenantId** : Identifiant du tenant client (dynamique)
  - **UserObjectId** : Identifiant de l'utilisateur connecté
  - **UserPrincipalName** : Email de l'utilisateur

### 2. Consultation des rôles
- Saisissez l'email de l'utilisateur à gérer
- Cliquez sur **"Voir rôles attribués"**
- Les rôles actuels s'affichent avec leur statut

### 3. Attribution de rôles
- Cochez les rôles à attribuer
- Cliquez sur **"Attribuer rôles sélectionnés"**
- Validez dans la boîte de dialogue de confirmation
- L'opération est automatiquement auditée

### 4. Révocation de rôles
- Cochez les rôles à révoquer
- Cliquez sur **"Révoquer rôles sélectionnés"**
- Confirmez l'opération

### 5. Rollback
- Cliquez sur **"Rollback"** pour annuler tous les rôles attribués par l'application
- Basé sur l'historique d'audit JSON

### 6. Export et audit
- Consultez le journal en temps réel
- Exportez les rapports au format JSON
- Les logs détaillés sont dans le dossier `Logs/`

## 🏗️ Architecture

### Structure du projet

```
OdywardRoleManager/
├── Models/              # Modèles de données
├── Services/            # Services métier et API
├── ViewModels/          # ViewModels MVVM
├── Views/              # Interfaces utilisateur XAML
├── Utils/              # Utilitaires et helpers
├── Styles/             # Styles et thèmes
└── Assets/             # Ressources (icônes, images)
```

### Services principaux

- **AuthService** : Authentification MSAL multi-tenant
- **GraphService** : Interactions avec Microsoft Graph API
- **RoleCatalog** : Catalogue des rôles supportés
- **ExportService** : Audit et export des opérations

### Rôles supportés

L'application gère actuellement ces rôles administratifs :
- **Application Administrator** : Gestion des applications d'entreprise
- **Groups Administrator** : Gestion des groupes et licences
- **User Administrator** : Gestion des utilisateurs et mots de passe

## 🔒 Sécurité et authentification

### Authentification multi-tenant MSAL
- **Authority dynamique** : `https://login.microsoftonline.com/common`
- **Extraction automatique** des informations du tenant depuis les claims du token :
  - `tid` → TenantId du client
  - `oid` → ObjectId de l'utilisateur
  - `preferred_username` ou `upn` → Email de l'utilisateur
- **Pas de TenantId codé en dur** : Support natif multi-tenant
- **Consentement par tenant** : Chaque organisation contrôle l'autorisation

### Processus de consentement
1. **Premier login dans un tenant** → Écran de consentement Microsoft
2. **Logins suivants du même utilisateur** → Authentification silencieuse
3. **Autres admins du même tenant** → Pas de nouveau consentement
4. **Nouveau tenant** → Nouveau consentement administrateur requis

### Sécurité renforcée
- **Authentification moderne** : MSAL avec PKCE
- **Permissions granulaires** : Scopes Microsoft Graph spécifiques
- **Audit complet** : Traçabilité de toutes les opérations par tenant
- **Pas de secrets** : Application publique uniquement (ClientId)
- **Validation** : Contrôles d'intégrité des données

## 📊 Logs et audit

### Logs système
- Fichiers rotatifs dans `Logs/odyward-{date}.log`
- Niveaux : Information, Warning, Error
- Retention : 14 jours

### Audit des opérations
- Fichiers JSON par utilisateur dans `Logs/`
- Historique des rôles avant/après chaque opération
- Métadonnées : timestamp, tenant, acteur

### Export des rapports
- Format JSON structuré
- Données : utilisateur, tenant, rôles, horodatage
- Facilite l'intégration avec des outils d'analyse

## 🧪 Tests

```bash
# Exécuter tous les tests
dotnet test

# Tests avec couverture
dotnet test --collect:"XPlat Code Coverage"

# Tests spécifiques
dotnet test --filter "ClassName=ValidationTests"
```

Types de tests inclus :
- **Tests unitaires** : Validation des services
- **Tests d'intégration** : ExportService et RoleCatalog
- **Tests de validation** : Email et données

## 🚨 Dépannage

### Problèmes d'authentification
- Vérifiez que le ClientId est correct dans `appsettings.json`
- Assurez-vous que le consentement admin a été accordé
- Vérifiez que l'URI de redirection correspond

### Erreurs Graph API
- **401 Unauthorized** : Permissions insuffisantes ou token expiré
- **403 Forbidden** : Consentement admin requis
- **429 Too Many Requests** : Retry automatique implémenté

### Problèmes de rôles
- Certains rôles peuvent nécessiter des permissions élevées
- Vérifiez que les templates de rôles existent dans le tenant
- Consultez les logs pour plus de détails

## 🤝 Contribution

1. Fork du projet
2. Créez votre branche feature (`git checkout -b feature/AmazingFeature`)
3. Commitez vos changes (`git commit -m 'Add AmazingFeature'`)
4. Push sur la branche (`git push origin feature/AmazingFeature`)
5. Ouvrez une Pull Request

## 📄 Licence

Ce projet est sous licence MIT. Voir le fichier `LICENSE` pour plus de détails.

## 🆘 Support

Pour le support et les questions :
- 📧 Email : support@odyward.com
- 📚 Documentation : [Wiki du projet](https://github.com/odyward/rolemanager/wiki)
- � Guide consentement : [MSAL-CONSENT.md](./MSAL-CONSENT.md)
- 🚀 Déploiement rapide : [DEPLOY.md](./DEPLOY.md)
- �🐛 Issues : [GitHub Issues](https://github.com/odyward/rolemanager/issues)

---

**Odyward Role Manager** - Gestion sécurisée et auditable des rôles Entra ID