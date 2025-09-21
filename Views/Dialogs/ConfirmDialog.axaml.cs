using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace _0900_OdywardRoleManager.Views.Dialogs;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        if (this.FindControl<Button>("ConfirmButton") is { } confirmButton)
        {
            confirmButton.Click += (_, _) => Close(true);
        }

        if (this.FindControl<Button>("CancelButton") is { } cancelButton)
        {
            cancelButton.Click += (_, _) => Close(false);
        }
    }
}
