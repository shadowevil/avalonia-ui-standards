using System.Reflection;
using Avalonia;
using AvaloniaUiStandards;
using AvaloniaUiStandards.Modules;

namespace Catalog;

public partial class MainWindow : StandardsWindow
{
    public MainWindow()
    {
        InitializeComponent();
        var version = typeof(StandardsTheme).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
        StatusVersion.Text = $"AvaloniaUiStandards {version.Split('+')[0]}";

        PageList.ItemsSource = CatalogPages.All.Select(p => new SidebarItem { Icon = p.Icon, Content = p.Title }).ToList();
        PageList.SelectionChanged += (_, _) =>
        {
            if (PageList.SelectedIndex < 0)
                return;
            var page = CatalogPages.All[PageList.SelectedIndex];
            Content = page.Create();
            StatusPage.Text = page.Title;
        };
        PageList.SelectedIndex = 0;

        // The ribbons tone themselves down outside the glass panel, where text sits on the backdrop.
        Ribbons.Bind(RibbonBackdrop.PanelBoundsProperty, this.GetObservable(ContentPanelBoundsProperty));
    }
}
