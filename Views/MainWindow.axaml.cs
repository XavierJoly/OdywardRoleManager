using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using _0900_OdywardRoleManager.ViewModels;
using _0900_OdywardRoleManager.ViewModels.Dialogs;
using _0900_OdywardRoleManager.Views.Dialogs;

namespace _0900_OdywardRoleManager.Views;

public partial class MainWindow : Window
{
    private ToggleSwitch? _themeToggle;
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        AttachThemeToggle();
        DataContextChanged += OnDataContextChanged;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void AttachThemeToggle()
    {
        _themeToggle = this.FindControl<ToggleSwitch>("ThemeToggle");
        if (_themeToggle is null)
        {
            return;
        }

        _themeToggle.IsCheckedChanged += ThemeToggleOnChanged;

        var theme = Application.Current?.ActualThemeVariant ?? ThemeVariant.Light;
        _themeToggle.IsChecked = theme == ThemeVariant.Dark;
    }

    private void ThemeToggleOnChanged(object? sender, RoutedEventArgs e)
    {
        if (_themeToggle is null)
        {
            return;
        }

        Application.Current!.RequestedThemeVariant = _themeToggle.IsChecked == true
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.SummaryDialogHandler = null;
            _viewModel.ConfirmationDialogHandler = null;
        }

        _viewModel = DataContext as MainWindowViewModel;
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.SummaryDialogHandler = ShowSummaryDialogAsync;
        _viewModel.ConfirmationDialogHandler = ShowConfirmDialogAsync;
    }

    private async Task<bool> ShowSummaryDialogAsync(SummaryDialogViewModel viewModel)
    {
        var dialog = new SummaryDialog
        {
            DataContext = viewModel,
        };

        var result = await dialog.ShowDialog<bool>(this);
        return result;
    }

    private async Task<bool> ShowConfirmDialogAsync(ConfirmDialogViewModel viewModel)
    {
        var dialog = new ConfirmDialog
        {
            DataContext = viewModel,
        };

        var result = await dialog.ShowDialog<bool>(this);
        return result;
    }
}
