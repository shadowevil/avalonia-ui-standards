using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using AvaloniaUiStandards.Blocks;

namespace Catalog;

/// <summary>
/// The catalog window's menus (File, Edit, View, Help), defined once as a <see cref="NativeMenu"/>: macOS shows them in
/// the system menu bar, the Windows layout in the window's menu row. The application menu's About item joins the end of
/// Help in the menu row.
/// </summary>
internal sealed class CatalogMenu
{
    private readonly MainWindow _window;

    public CatalogMenu(MainWindow window)
    {
        _window = window;
        var command = Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control;

        CloseWindow = new NativeMenuItem("_Close window");
        CloseWindow.Click += (_, _) => _window.Close();
        var file = new NativeMenuItem("_File") { Menu = new NativeMenu { CloseWindow } };

        // Edit acts on the focused text box, as a text field's own context menu does.
        var edit = new NativeMenuItem("_Edit")
        {
            Menu = new NativeMenu
            {
                TextAction("_Undo", new KeyGesture(Key.Z, command), t => t.Undo()),
                TextAction("_Redo", new KeyGesture(Key.Y, command), t => t.Redo()),
                new NativeMenuItemSeparator(),
                TextAction("Cu_t", new KeyGesture(Key.X, command), t => t.Cut()),
                TextAction("_Copy", new KeyGesture(Key.C, command), t => t.Copy()),
                TextAction("_Paste", new KeyGesture(Key.V, command), t => t.Paste()),
                new NativeMenuItemSeparator(),
                TextAction("Select _all", new KeyGesture(Key.A, command), t => t.SelectAll()),
            },
        };

        // View: the theme and density as radio items in step with the toolbar band, and page navigation.
        var band = window.ThemeControls;
        ThemeItems = RadioGroup(band.Theme, ["_System", "_Light", "_Dark"]);
        DensityItems = RadioGroup(band.Density, ["_Normal", "_Compact"]);
        NextPage = new NativeMenuItem("_Next page") { Gesture = new KeyGesture(Key.PageDown, command) };
        NextPage.Click += (_, _) => MovePage(1);
        PreviousPage = new NativeMenuItem("_Previous page") { Gesture = new KeyGesture(Key.PageUp, command) };
        PreviousPage.Click += (_, _) => MovePage(-1);
        var view = new NativeMenuItem("_View")
        {
            Menu = new NativeMenu
            {
                new NativeMenuItem("_Theme") { Menu = MenuOf(ThemeItems) },
                new NativeMenuItem("_Density") { Menu = MenuOf(DensityItems) },
                new NativeMenuItemSeparator(),
                NextPage,
                PreviousPage,
            },
        };

        // Help: where the settings are kept; About comes from the application menu.
        SettingsLocation = new NativeMenuItem("Show the settings _file location");
        SettingsLocation.Click += (_, _) => _ = OpenFolder();
        var help = new NativeMenuItem("_Help") { Menu = new NativeMenu { SettingsLocation } };

        Menu = new NativeMenu { file, edit, view, help };
    }

    /// <summary>The window's menu.</summary>
    public NativeMenu Menu { get; }

    public NativeMenuItem CloseWindow { get; }

    /// <summary>Help: opens the folder holding settings.json.</summary>
    public NativeMenuItem SettingsLocation { get; }

    /// <summary>View, Theme: System, Light, Dark.</summary>
    public IReadOnlyList<NativeMenuItem> ThemeItems { get; }

    /// <summary>View, Density: Normal, Compact.</summary>
    public IReadOnlyList<NativeMenuItem> DensityItems { get; }

    public NativeMenuItem NextPage { get; }

    public NativeMenuItem PreviousPage { get; }

    private static NativeMenu MenuOf(IEnumerable<NativeMenuItem> items)
    {
        var menu = new NativeMenu();
        foreach (var item in items)
            menu.Add(item);
        return menu;
    }

    private NativeMenuItem TextAction(string header, KeyGesture gesture, Action<TextBox> action)
    {
        var item = new NativeMenuItem(header) { Gesture = gesture };
        item.Click += (_, _) =>
        {
            if (_window.FocusManager?.GetFocusedElement() is TextBox box)
                action(box);
        };
        return item;
    }

    // One radio item per segment of a toolbar band control: choosing one selects its segment, and the marks follow the control.
    private static IReadOnlyList<NativeMenuItem> RadioGroup(SegmentedControl control, string[] headers)
    {
        var items = headers.Select((h, i) => new NativeMenuItem(h) { ToggleType = MenuItemToggleType.Radio }).ToList();
        for (var i = 0; i < items.Count; i++)
        {
            var index = i;
            items[i].Click += (_, _) => control.SelectedIndex = index;
        }
        void Mark()
        {
            for (var i = 0; i < items.Count; i++)
                items[i].IsChecked = i == control.SelectedIndex;
        }
        control.SelectionChanged += (_, _) => Mark();
        Mark();
        return items;
    }

    private void MovePage(int step)
    {
        var count = CatalogPages.All.Count;
        var current = Math.Max(0, _window.SelectedPage);
        _window.SelectPage((current + step + count) % count);
    }

    private async Task OpenFolder()
    {
        var folder = Path.GetDirectoryName(CatalogSettings.DefaultPath)!;
        Directory.CreateDirectory(folder);
        if (TopLevel.GetTopLevel(_window)?.Launcher is { } launcher)
            await launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(folder));
    }
}
