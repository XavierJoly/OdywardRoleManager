using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using _0900_OdywardRoleManager.Models;
using _0900_OdywardRoleManager.Services;
using _0900_OdywardRoleManager.Utils;
using _0900_OdywardRoleManager.ViewModels.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace _0900_OdywardRoleManager.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly GraphService _graphService;
    private readonly RoleCatalog _roleCatalog;
    private readonly ExportService _exportService;
    private readonly ILogger _logger;

    private AuthContext? _authContext;
    private DirectoryUser? _currentUser;

    public MainWindowViewModel(IAuthService authService, GraphService graphService, RoleCatalog roleCatalog, ExportService exportService, ILogger logger)
    {
        _authService = authService;
        _graphService = graphService;
        _roleCatalog = roleCatalog;
        _exportService = exportService;
        _logger = logger;

        Roles = new ObservableCollection<RoleItemViewModel>(_roleCatalog
            .CreateRoleModels()
            .Select(role => new RoleItemViewModel(role)));

        foreach (var role in Roles)
        {
            role.PropertyChanged += OnRolePropertyChanged;
        }

        ActivityLog = new ObservableCollection<string>();

        LoadRolesCommand = new AsyncRelayCommand(LoadRolesAsync, CanExecuteRoleCommands);
        AssignSelectedRolesCommand = new AsyncRelayCommand(AssignSelectedRolesAsync, CanExecuteSelectionCommands);
        RevokeSelectedRolesCommand = new AsyncRelayCommand(RevokeSelectedRolesAsync, CanExecuteSelectionCommands);
        RollbackCommand = new AsyncRelayCommand(RollbackAsync, () => !IsBusy);
        ExportAuditCommand = new AsyncRelayCommand(ExportAuditAsync, () => !IsBusy && _currentUser is not null);
    }

    public ObservableCollection<RoleItemViewModel> Roles { get; }

    public ObservableCollection<string> ActivityLog { get; }

    public IAsyncRelayCommand LoadRolesCommand { get; }

    public IAsyncRelayCommand AssignSelectedRolesCommand { get; }

    public IAsyncRelayCommand RevokeSelectedRolesCommand { get; }

    public IAsyncRelayCommand RollbackCommand { get; }

    public IAsyncRelayCommand ExportAuditCommand { get; }

    public Func<SummaryDialogViewModel, Task<bool>>? SummaryDialogHandler { get; set; }

    public Func<ConfirmDialogViewModel, Task<bool>>? ConfirmationDialogHandler { get; set; }

    [ObservableProperty]
    private string emailInput = string.Empty;

    partial void OnEmailInputChanged(string value) => LoadRolesCommand.NotifyCanExecuteChanged();

    [ObservableProperty]
    private bool isBusy;

    partial void OnIsBusyChanged(bool value) => RaiseCommandStates();

    [ObservableProperty]
    private string statusMessage = "Prêt";

    [ObservableProperty]
    private string tenantId = string.Empty;

    [ObservableProperty]
    private string currentUserPrincipalName = string.Empty;

    private bool CanExecuteRoleCommands() => !IsBusy && Validation.IsValidEmail(EmailInput);

    private bool CanExecuteSelectionCommands()
    {
        if (IsBusy || _currentUser is null)
        {
            return false;
        }

        return Roles.Any(role => role.IsSelected);
    }

    private void RaiseCommandStates()
    {
        LoadRolesCommand.NotifyCanExecuteChanged();
        AssignSelectedRolesCommand.NotifyCanExecuteChanged();
        RevokeSelectedRolesCommand.NotifyCanExecuteChanged();
        RollbackCommand.NotifyCanExecuteChanged();
        ExportAuditCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadRolesAsync()
    {
        if (!Validation.IsValidEmail(EmailInput))
        {
            StatusMessage = "Adresse e-mail invalide.";
            return;
        }

        await RunAsync(async () =>
        {
            _authContext = await EnsureAuthContextAsync();
            var user = await _graphService.GetUserByEmailAsync(EmailInput, _authContext, default);
            if (user is null)
            {
                StatusMessage = "Utilisateur introuvable.";
                AddLog($"Utilisateur {EmailInput} introuvable.");
                return;
            }

            _currentUser = user;
            CurrentUserPrincipalName = user.UserPrincipalName;
            TenantId = _authContext.TenantId;

            var assignedRoles = await _graphService.GetAssignedRoleDisplayNamesAsync(user.Id, _authContext, default);
            UpdateRoleStates(assignedRoles);

            StatusMessage = $"Rôles chargés pour {user.UserPrincipalName}.";
            AddLog($"{assignedRoles.Count} rôle(s) détecté(s) pour {user.UserPrincipalName}.");
        }, "Chargement des rôles");
    }

    private async Task AssignSelectedRolesAsync()
    {
        if (!EnsureUserAndAuth())
        {
            return;
        }

        var selection = Roles
            .Where(role => role.IsSelected && !role.IsAssigned)
            .Select(role => role.DisplayName)
            .ToList();

        if (selection.Count == 0)
        {
            StatusMessage = "Aucun rôle à attribuer.";
            return;
        }

        var beforeSnapshot = GetAssignedRoles();

        if (SummaryDialogHandler is not null)
        {
            var summaryVm = new SummaryDialogViewModel(_currentUser!.UserPrincipalName ?? EmailInput, selection, beforeSnapshot);
            var proceed = await SummaryDialogHandler(summaryVm);
            if (!proceed)
            {
                StatusMessage = "Attribution annulée.";
                return;
            }
        }

        await RunAsync(async () =>
        {
            _authContext = await EnsureAuthContextAsync();
            var selectionSet = new HashSet<string>(selection, StringComparer.OrdinalIgnoreCase);
            var assigned = await _graphService.AssignRolesAsync(_currentUser!.Id, selection, _authContext!, default);
            foreach (var role in Roles)
            {
                if (selectionSet.Contains(role.DisplayName))
                {
                    role.IsAssigned = true;
                    role.IsSelected = false;
                }
            }

            var after = GetAssignedRoles();
            var entry = CreateAuditEntry(beforeSnapshot, after);
            await _exportService.SaveAuditEntryAsync(entry, default);
            AddLog($"Attribution réussie: {string.Join(", ", assigned)}");
            StatusMessage = "Attribution terminée.";
        }, "Attribution des rôles");
    }

    private async Task RevokeSelectedRolesAsync()
    {
        if (!EnsureUserAndAuth())
        {
            return;
        }

        var selection = Roles
            .Where(role => role.IsSelected && role.IsAssigned)
            .Select(role => role.DisplayName)
            .ToList();

        if (selection.Count == 0)
        {
            StatusMessage = "Aucun rôle à révoquer.";
            return;
        }

        if (ConfirmationDialogHandler is not null)
        {
            var confirmVm = new ConfirmDialogViewModel("Révocation", "Confirmer la révocation des rôles sélectionnés ?", selection);
            var proceed = await ConfirmationDialogHandler(confirmVm);
            if (!proceed)
            {
                StatusMessage = "Révocation annulée.";
                return;
            }
        }

        var beforeSnapshot = GetAssignedRoles();

        await RunAsync(async () =>
        {
            _authContext = await EnsureAuthContextAsync();
            var selectionSet = new HashSet<string>(selection, StringComparer.OrdinalIgnoreCase);
            var revoked = await _graphService.RevokeRolesAsync(_currentUser!.Id, selection, _authContext!, default);
            foreach (var role in Roles)
            {
                if (selectionSet.Contains(role.DisplayName))
                {
                    role.IsAssigned = false;
                    role.IsSelected = false;
                }
            }

            var after = GetAssignedRoles();
            var entry = CreateAuditEntry(beforeSnapshot, after);
            await _exportService.SaveAuditEntryAsync(entry, default);
            AddLog($"Révocation réussie: {string.Join(", ", revoked)}");
            StatusMessage = "Révocation terminée.";
        }, "Révocation des rôles");
    }

    private async Task RollbackAsync()
    {
        if (!EnsureUserAndAuth())
        {
            return;
        }

        if (ConfirmationDialogHandler is not null)
        {
            var confirmVm = new ConfirmDialogViewModel("Rollback", "Confirmer le rollback des rôles attribués par l'application ?", Array.Empty<string>());
            var proceed = await ConfirmationDialogHandler(confirmVm);
            if (!proceed)
            {
                StatusMessage = "Rollback annulé.";
                return;
            }
        }

        var beforeSnapshot = GetAssignedRoles();

        await RunAsync(async () =>
        {
            _authContext = await EnsureAuthContextAsync();
            var identifier = _currentUser!.UserPrincipalName ?? EmailInput;
            var revoked = await _graphService.RevokeAllRolesAttributedByAppAsync(identifier, _currentUser.Id, _authContext!, default);
            if (revoked.Count == 0)
            {
                StatusMessage = "Aucun rôle attribué par l'application.";
                return;
            }

            var revokedSet = new HashSet<string>(revoked, StringComparer.OrdinalIgnoreCase);
            foreach (var role in Roles)
            {
                if (revokedSet.Contains(role.DisplayName))
                {
                    role.IsAssigned = false;
                    role.IsSelected = false;
                }
            }

            var after = GetAssignedRoles();
            var entry = CreateAuditEntry(beforeSnapshot, after);
            await _exportService.SaveAuditEntryAsync(entry, default);
            AddLog($"Rollback: {string.Join(", ", revoked)}");
            StatusMessage = "Rollback terminé.";
        }, "Rollback des rôles");
    }

    private async Task ExportAuditAsync()
    {
        if (_currentUser is null || _authContext is null)
        {
            StatusMessage = "Charger d'abord un utilisateur.";
            return;
        }

        await RunAsync(async () =>
        {
            _authContext = await EnsureAuthContextAsync();
            var roles = GetAssignedRoles();
            var entry = CreateAuditEntry(roles, roles);
            var path = await _exportService.ExportAssignmentReportAsync(entry, default);
            AddLog($"Rapport exporté: {path}");
            StatusMessage = "Rapport exporté.";
        }, "Export du rapport");
    }

    private async Task RunAsync(Func<Task> action, string activity)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        StatusMessage = activity;
        try
        {
            await action();
        }
        catch (GraphApiException ex)
        {
            StatusMessage = ex.Message;
            _logger.Warning(ex, "Erreur Graph API");
            AddLog($"Erreur Graph API: {ex.Message}");
        }
        catch (Exception ex)
        {
            StatusMessage = "Erreur inattendue.";
            _logger.Error(ex, "Erreur inattendue");
            AddLog($"Erreur inattendue: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            RaiseCommandStates();
        }
    }

    private async Task<AuthContext> EnsureAuthContextAsync()
    {
        if (_authContext is { ExpiresOn: var exp } && exp > DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return _authContext;
        }

        var silent = await _authService.TryAcquireTokenSilentAsync();
        if (silent is not null)
        {
            _authContext = silent;
            AddLog($"✅ Reconnexion automatique - Tenant: {silent.TenantId}");
            return _authContext;
        }

        AddLog("🔐 Authentification requise - Un administrateur doit autoriser l'application lors de la première connexion dans ce tenant.");
        _authContext = await _authService.AuthenticateAsync();
        AddLog($"✅ Connecté - Utilisateur: {_authContext.UserPrincipalName}, Tenant: {_authContext.TenantId}");
        
        return _authContext;
    }

    private bool EnsureUserAndAuth()
    {
        if (_currentUser is null)
        {
            StatusMessage = "Charger d'abord l'utilisateur.";
            return false;
        }

        return true;
    }

    private void UpdateRoleStates(IReadOnlyCollection<string> assignedRoles)
    {
        var assignedSet = new HashSet<string>(assignedRoles, StringComparer.OrdinalIgnoreCase);
        foreach (var role in Roles)
        {
            role.IsAssigned = assignedSet.Contains(role.DisplayName);
            role.IsSelected = false;
        }

        RaiseCommandStates();
    }

    private List<string> GetAssignedRoles() => Roles
        .Where(role => role.IsAssigned)
        .Select(role => role.DisplayName)
        .ToList();

    private AuditEntry CreateAuditEntry(IEnumerable<string> before, IEnumerable<string> after)
    {
        return new AuditEntry
        {
            Email = _currentUser?.UserPrincipalName ?? EmailInput,
            TenantId = _authContext?.TenantId ?? string.Empty,
            ActorUpn = _authContext?.UserPrincipalName ?? string.Empty,
            RolesBefore = before.ToList(),
            RolesAfter = after.ToList(),
            Timestamp = DateTimeOffset.UtcNow,
        };
    }

    private void AddLog(string message)
    {
        var entry = $"{DateTime.Now:HH:mm:ss} | {message}";
        ActivityLog.Insert(0, entry);
        while (ActivityLog.Count > 200)
        {
            ActivityLog.RemoveAt(ActivityLog.Count - 1);
        }
    }

    private void OnRolePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RoleItemViewModel.IsSelected) or nameof(RoleItemViewModel.IsAssigned))
        {
            RaiseCommandStates();
        }
    }
}
