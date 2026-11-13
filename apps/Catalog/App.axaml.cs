using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Catalog;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        Name = AppInfo.Name;
        NativeMenu.SetMenu(this, CreateMenu());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }

    internal static NativeMenu CreateMenu()
    {
        var about = new NativeMenuItem($"About {AppInfo.Name}");
        about.Click += (_, _) =>
        {
            var owner = (Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            var window = new AboutWindow();
            if (owner is not null)
                window.ShowDialog(owner);
            else
                window.Show();
        };
        return new NativeMenu { about };
    }
}
