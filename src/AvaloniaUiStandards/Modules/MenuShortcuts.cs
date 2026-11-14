using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// The window shortcuts of a <see cref="StandardsWindow"/>'s menu: one per menu item with a gesture (the window's
/// menu and the Application's), whenever the platform does not export the menu to a system menu bar, which then
/// handles them itself. They work in either layout mode, with or without the menu row.
/// </summary>
internal sealed class MenuShortcuts : IDisposable
{
    private readonly TopLevel _window;
    private readonly List<KeyBinding> _bindings = [];
    private readonly List<IDisposable> _watches = [];
    private NativeMenu? _menu;
    private bool _refreshPosted;
    private bool _watchingApp;

    public MenuShortcuts(TopLevel window)
    {
        _window = window;
        _window.PropertyChanged += OnWindowPropertyChanged;
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

    /// <summary>The shortcuts in place.</summary>
    internal IReadOnlyList<KeyBinding> Bindings => _bindings;

    public void Dispose()
    {
        Menu = null;
        _window.PropertyChanged -= OnWindowPropertyChanged;
    }

    private static NativeMenu? AppMenu => Application.Current is { } app ? NativeMenu.GetMenu(app) : null;

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
        foreach (var binding in _bindings)
            _window.KeyBindings.Remove(binding);
        _bindings.Clear();
        foreach (var watch in _watches)
            watch.Dispose();
        _watches.Clear();
        if (_menu is null)
            return;

        var exported = NativeMenu.GetIsNativeMenuExported(_window);
        foreach (var menu in new[] { _menu, AppMenu })
            if (menu is not null)
                Add(menu, [], exported);
        foreach (var binding in _bindings)
            _window.KeyBindings.Add(binding);
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
                _bindings.Add(new KeyBinding { Gesture = gesture, Command = new ShortcutCommand(path) });
            if (item.Menu is { } submenu)
                Add(submenu, path, exported);
        }
    }

    /// <summary>Clicks the item at the end of <paramref name="path"/>, while it and its menus are enabled and shown.</summary>
    private sealed class ShortcutCommand(IReadOnlyList<NativeMenuItem> path) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => path.All(item => item.IsEnabled && item.IsVisible);

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
                ((INativeMenuItemExporterEventsImplBridge)path[^1]).RaiseClicked();
        }
    }

    private sealed class Disposable(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
