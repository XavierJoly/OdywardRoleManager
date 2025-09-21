using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace _0900_OdywardRoleManager.ViewModels.Dialogs;

public partial class SummaryDialogViewModel : ObservableObject
{
    public SummaryDialogViewModel(string email, IEnumerable<string> rolesToAssign, IEnumerable<string> currentRoles)
    {
        Email = email;
        RolesToAssign = new ObservableCollection<string>(rolesToAssign);
        CurrentRoles = new ObservableCollection<string>(currentRoles);
    }

    public string Email { get; }

    public ObservableCollection<string> RolesToAssign { get; }

    public ObservableCollection<string> CurrentRoles { get; }

    [ObservableProperty]
    private bool isConfirmed;
}
