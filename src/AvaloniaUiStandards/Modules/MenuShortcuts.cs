using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// The window shortcuts of a <see cref="StandardsWindow"/>'s menu: each menu item with a gesture (the window's menu
/// and the Application's) is clicked by its gesture, whenever the platform does not export the menu to a system menu
/// bar (which then handles them itself). They work in either layout mode, with or without the menu row. The focused
/// control handles a key first: a shortcut acts only on a key no control handled, as in native desktop apps.
/// </summary>
internal sealed class MenuShortcuts : IDisposable
{
    private readonly TopLevel _window;
    private readonly List<(KeyGesture Gesture, IReadOnlyList<NativeMenuItem> Path)> _shortcuts = [];
    private readonly List<IDisposable> _watches = [];
    private NativeMenu? _menu;
    private bool _refreshPosted;
    private bool _watchingApp;

    public MenuShortcuts(TopLevel window)
    {
        _window = window;
        _window.PropertyChanged += OnWindowPropertyChanged;
        // Bubbling and unhandled only: after the focused control and its parents have had the key.
        _window.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble);
    }

    /// <summary>The window's menu; with none there are no shortcuts.</summary>
    public NativeMenu? Menu
    {
        get => _menu;
        set
        {
            if (_menu == value)
                return;
            _menu = value;
            WatchApp(value is not null);
            Refresh();
        }
    }

    /// <summary>The gestures in use.</summary>
    internal IReadOnlyList<KeyGesture> Gestures => _shortcuts.Select(s => s.Gesture).ToList();

    public void Dispose()
    {
        Menu = null;
        _window.PropertyChanged -= OnWindowPropertyChanged;
        _window.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
    }

    private static NativeMenu? AppMenu => Application.Current is { } app ? NativeMenu.GetMenu(app) : null;

    // A key no control handled: the first shortcut for it whose item and menus are enabled and shown clicks the item.
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled)
            return;
        foreach (var (gesture, path) in _shortcuts)
            if (gesture.Matches(e) && path.All(item => item.IsEnabled && item.IsVisible))
            {
                e.Handled = true;
                ((INativeMenuItemExporterEventsImplBridge)path[^1]).RaiseClicked();
                return;
            }
    }

    // The platform starts or stops exporting the menu.
    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == NativeMenu.IsNativeMenuExportedProperty)
            PostRefresh();
    }

    // The Application gets another menu.
    private void OnAppPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == NativeMenu.MenuProperty)
            PostRefresh();
    }

    private void WatchApp(bool watch)
    {
        if (watch == _watchingApp || Application.Current is not { } app)
            return;
        _watchingApp = watch;
        if (watch)
            app.PropertyChanged += OnAppPropertyChanged;
        else
            app.PropertyChanged -= OnAppPropertyChanged;
    }

    private void PostRefresh()
    {
        if (_refreshPosted)
            return;
        _refreshPosted = true;
        Dispatcher.UIThread.Post(() =>
        {
            _refreshPosted = false;
            Refresh();
        });
    }

    /// <summary>Makes the shortcuts again from the menus, and watches them for items, submenus and gestures that change.</summary>
    internal void Refresh()
    {
        _shortcuts.Clear();
        foreach (var watch in _watches)
            watch.Dispose();
        _watches.Clear();
        if (_menu is null)
            return;

        var exported = NativeMenu.GetIsNativeMenuExported(_window);
        foreach (var menu in new[] { _menu, AppMenu })
            if (menu is not null)
                Add(menu, [], exported);
    }

    private void Add(NativeMenu menu, IReadOnlyList<NativeMenuItem> parents, bool exported)
    {
        var items = (INotifyCollectionChanged)menu.Items;
        NotifyCollectionChangedEventHandler changed = (_, _) => PostRefresh();
        items.CollectionChanged += changed;
        _watches.Add(new Disposable(() => items.CollectionChanged -= changed));
        foreach (var item in menu.Items.OfType<NativeMenuItem>())
        {
            EventHandler<AvaloniaPropertyChangedEventArgs> itemChanged = (_, e) =>
            {
                if (e.Property == NativeMenuItem.GestureProperty || e.Property == NativeMenuItem.MenuProperty)
                    PostRefresh();
            };
            item.PropertyChanged += itemChanged;
            _watches.Add(new Disposable(() => item.PropertyChanged -= itemChanged));

            List<NativeMenuItem> path = [.. parents, item];
            if (item.Gesture is { } gesture && !exported)
                _shortcuts.Add((gesture, path));
            if (item.Menu is { } submenu)
                Add(submenu, path, exported);
        }
    }

    private sealed class Disposable(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
