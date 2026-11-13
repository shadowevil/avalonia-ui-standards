using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaloniaUiStandards;
using AvaloniaUiStandards.Modules;

namespace Catalog;

/// <summary>A small window with the app name and the library version.</summary>
internal sealed class AboutWindow : StandardsWindow
{
    public AboutWindow()
    {
        Title = $"About {AppInfo.Name}";
        Width = 360;
        Height = 200;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Content = new StackPanel
        {
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                new TextBlock { Text = AppInfo.Name, FontSize = 18, FontWeight = Avalonia.Media.FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Center },
                new TextBlock { Text = AppInfo.LibraryText, HorizontalAlignment = HorizontalAlignment.Center },
            },
        };
    }
}
