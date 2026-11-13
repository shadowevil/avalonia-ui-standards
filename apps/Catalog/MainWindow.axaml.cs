using Avalonia.Controls;

namespace Catalog;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Sidebar.ItemsSource = CatalogPages.All.Select(p => p.Title).ToList();
        Sidebar.SelectionChanged += (_, _) =>
        {
            if (Sidebar.SelectedIndex >= 0)
                Scroller.Content = CatalogPages.All[Sidebar.SelectedIndex].Create();
        };
        Sidebar.SelectedIndex = 0;
    }
}
