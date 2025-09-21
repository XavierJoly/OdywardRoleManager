using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace _0900_OdywardRoleManager.Views.Controls;

public partial class RoleItem : UserControl
{
    public RoleItem()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
