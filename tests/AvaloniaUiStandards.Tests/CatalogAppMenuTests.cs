using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Catalog;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>The catalog's own app name and application menu replace Avalonia's default "About Avalonia".</summary>
public class CatalogAppMenuTests
{
    [AvaloniaFact]
    public void AppMenu_NamesTheApp_AndAboutOpensOnce()
    {
        var app = Application.Current!;
        App.ApplyIdentity(app);
        try
        {
            Assert.Equal("Avalonia UI Standards Catalog", app.Name);
            var item = Assert.Single(NativeMenu.GetMenu(app)!.Items.OfType<NativeMenuItem>());
            Assert.Equal("About Avalonia UI Standards Catalog", item.Header);

            var click = (INativeMenuItemExporterEventsImplBridge)item;
            click.RaiseClicked();
            var about = App.OpenAbout;
            Assert.NotNull(about);
            var texts = about.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
            Assert.Contains(AppInfo.Name, texts);
            Assert.Contains(AppInfo.LibraryText, texts);

            click.RaiseClicked();
            Assert.Same(about, App.OpenAbout);

            about.Close();
            Assert.Null(App.OpenAbout);
        }
        finally
        {
            App.OpenAbout?.Close();
            app.Name = null;
            NativeMenu.SetMenu(app, null!);
        }
    }

    [AvaloniaFact]
    public void MainWindow_StatusBarShowsLibraryVersion()
    {
        var window = new MainWindow();
        try
        {
            window.Show();
            var status = window.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "StatusVersion");
            Assert.Equal(AppInfo.LibraryText, status.Text);
        }
        finally
        {
            window.Close();
        }
    }
}
