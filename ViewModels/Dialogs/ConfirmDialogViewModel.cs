using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace _0900_OdywardRoleManager.ViewModels.Dialogs;

public partial class ConfirmDialogViewModel : ObservableObject
{
    public ConfirmDialogViewModel(string title, string message, IEnumerable<string> items)
    {
        Title = title;
        Message = message;
        Items = new ObservableCollection<string>(items);
    }

    public string Title { get; }

    public string Message { get; }

    public ObservableCollection<string> Items { get; }

    [ObservableProperty]
    private bool isConfirmed;
}
