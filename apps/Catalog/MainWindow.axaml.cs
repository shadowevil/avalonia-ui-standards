using System.Reflection;
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

        PageList.ItemsSource = CatalogPages.All.Select(p => p.Title).ToList();
        PageList.SelectionChanged += (_, _) =>
        {
            if (PageList.SelectedIndex < 0)
                return;
            var page = CatalogPages.All[PageList.SelectedIndex];
            Scroller.Content = page.Create();
            StatusPage.Text = page.Title;
        };
        PageList.SelectedIndex = 0;
    }
}
