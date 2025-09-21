using CommunityToolkit.Mvvm.ComponentModel;
using _0900_OdywardRoleManager.Models;

namespace _0900_OdywardRoleManager.ViewModels;

public partial class RoleItemViewModel : ObservableObject
{
    private readonly RoleModel _model;

    public RoleItemViewModel(RoleModel model)
    {
        _model = model;
    }

    public string DisplayName => _model.DisplayName;

    public string Description => _model.Description;

    public string TemplateId => _model.TemplateId;

    [ObservableProperty]
    private bool isAssigned;

    partial void OnIsAssignedChanged(bool value) => OnPropertyChanged(nameof(Status));

    [ObservableProperty]
    private bool isSelected;

    partial void OnIsSelectedChanged(bool value) => OnPropertyChanged(nameof(Status));

    public string Status => IsAssigned ? "Déjà attribué" : (IsSelected ? "Sélectionné" : "Disponible");
}
