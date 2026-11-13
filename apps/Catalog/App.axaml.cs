using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Catalog;

public partial class App : Application
{
    private static AboutWindow? _about;

    internal static AboutWindow? OpenAbout => _about;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ApplyIdentity(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Sets the application name and its own application menu (the macOS app menu).</summary>
    internal static void ApplyIdentity(Application app)
    {
        app.Name = AppInfo.Name;
        var about = new NativeMenuItem($"About {AppInfo.Name}");
        about.Click += (_, _) => ShowAbout();
        NativeMenu.SetMenu(app, new NativeMenu { about });
    }

    private static void ShowAbout()
    {
        // The macOS app menu stays clickable while the dialog is open: reuse the open window.
        if (_about is not null)
        {
            _about.Activate();
            return;
        }
        var owner = (Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        _about = new AboutWindow();
        _about.Closed += (_, _) => _about = null;
        if (owner is not null)
            _about.ShowDialog(owner);
        else
            _about.Show(); // no main window to own it (headless tests)
    }
}
