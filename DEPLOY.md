# Guide de déploiement rapide - Odyward Role Manager

## 🚀 Déploiement rapide

### 1. Prérequis Azure AD

```bash
# 1. Créer l'app dans Azure AD
# 2. Noter le Client ID
# 3. Configurer les permissions :
#    - Directory.ReadWrite.All (Admin)
#    - RoleManagement.ReadWrite.Directory (Admin)  
#    - User.Read.All (Admin)
# 4. Faire valider par un Global Admin
```

### 2. Configuration

```json
// appsettings.json
{
  "AzureAd": {
    "ClientId": "REMPLACER_PAR_VOTRE_CLIENT_ID",
    "Authority": "https://login.microsoftonline.com/common",
    "RedirectUri": "http://localhost"
  }
}
```

### 3. Build et déploiement

#### Windows (ExE portable)
```cmd
build-windows.bat
# → bin/Release/net8.0/win-x64/publish/0900_OdywardRoleManager.exe
```

#### macOS (Bundle .app)
```bash
./build-macos.sh
# → bin/Release/net8.0/osx-x64/publish/OdywardRoleManager.app
```

#### Linux (Exécutable)
```bash
./build-linux.sh  
# → bin/Release/net8.0/linux-x64/publish/0900_OdywardRoleManager
```

## 📋 Checklist de déploiement

- [ ] Application Azure AD créée et configurée
- [ ] Permissions accordées par l'administrateur global
- [ ] Client ID mis à jour dans `appsettings.json`
- [ ] Application testée en mode développement (`dotnet run`)
- [ ] Build de production réussi
- [ ] Tests unitaires qui passent (`dotnet test`)

## 🔧 Dépannage rapide

### Erreur d'authentification
➡️ Vérifiez le Client ID et les permissions Azure AD

### Erreur "Unauthorized"
➡️ Consentement administrateur requis

### Erreur de build
➡️ Vérifiez que .NET 8 SDK est installé

## 📞 Support
- Email : support@odyward.com
- Documentation complète : README.md