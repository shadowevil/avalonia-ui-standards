using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvaloniaUiStandards.Blocks;

namespace AvaloniaUiStandards.Modules;

/// <summary>
/// The in-window menu bar of <see cref="StandardsWindow"/>'s menu row: a Menu bar (0036's look) built
/// from the window's <see cref="NativeMenu"/> and kept in sync with it, with the Application's menu items
/// appended at the end of Help. Titles that do not fit fold, from the end, into a trailing "…" title that
/// holds them as submenus. The items' gestures are window shortcuts kept by <see cref="MenuShortcuts"/>.
/// </summary>
/// <remarks>
/// Avalonia's <see cref="NativeMenuBar"/> is not used: it hides itself whenever the platform exports the
/// menu (macOS, Linux desktops with a global menu), which would leave the Windows layout on those
/// systems without a menu, and it does not act on the gestures.
/// </remarks>
internal sealed class MenuRow : Menu
{
    /// <summary>The resource with the Help menu's header: the title the Application's items join, or the one added for them.</summary>
    public const string HelpHeaderKey = "MenuRowHelpHeader";

    /// <summary>The default Help header (with its access key).</summary>
    public const string HelpHeader = "_Help";

    private readonly MenuItem _more;
    private readonly Dictionary<MenuItem, double> _natural = [];
    // Each title's native item (null for the added Help title) and the Application's items it ends with.
    private readonly Dictionary<MenuItem, (NativeMenuItem? Native, Func<Node, IEnumerable<Control>>? Tail)> _titleSources = [];
    private int _foldedApplied;
    // The native item each built menu item stands for, so a click can put a radio group back as the native items have it.
    private readonly ConditionalWeakTable<MenuItem, NativeMenuItem> _natives = new();
    private Node _root = new();
    private Node _moreNode = new();
    private NativeMenu? _source;
    private NativeMenu? _appMenu;
    private int _shownTitles = int.MaxValue;
    private bool _foldPosted;
    private bool _rebuildPosted;

    public MenuRow()
    {
        ItemsPanel = new FuncTemplate<Panel?>(() => new FoldingPanel(this));
        _more = new MenuItem
        {
            Header = new Icon { Data = Icons.DotsThree, HorizontalAlignment = HorizontalAlignment.Center },
            Padding = default,
            IsVisible = false,
        };
        _more.Bind(Layoutable.WidthProperty, new DynamicResourceExtension(ButtonParts.MinHeightKey));
        ToolTip.SetTip(_more, ToolbarBand.MoreLabel);
        AutomationProperties.SetName(_more, ToolbarBand.MoreLabel);
        WindowDecorationProperties.SetElementRole(_more, WindowDecorationsElementRole.DecorationsElement);
        Items.Add(_more);
    }

    protected override Type StyleKeyOverride => typeof(Menu);

    /// <summary>The window's menu, shown as the titles.</summary>
    public NativeMenu? Source
    {
        get => _source;
        set
        {
            if (_source == value)
                return;
            _source = value;
            Rebuild();
        }
    }

    /// <summary>The Application's menu, whose items join the end of Help.</summary>
    public NativeMenu? AppMenu
    {
        get => _appMenu;
        set
        {
            if (_appMenu == value)
                return;
            _appMenu = value;
            Rebuild();
        }
    }

    /// <summary>The menu titles, in order, folded or not (not the "…" title).</summary>
    internal List<MenuItem> Titles => Items.OfType<MenuItem>().Where(i => i != _more).ToList();

    /// <summary>The "…" title holding the folded titles.</summary>
    internal MenuItem MoreItem => _more;

    /// <summary>Opens the menu bar from the keyboard (F10), selecting its first title, or closes it when open.</summary>
    internal void ToggleFromKeyboard()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Application.Current is { } app)
            app.PropertyChanged += OnAppPropertyChanged;
        // The Help header comes from the resources, which are reachable from here on.
        Rebuild();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Application.Current is { } app)
            app.PropertyChanged -= OnAppPropertyChanged;
    }

    // The Application gets another menu: its items join Help from the new one.
    private void OnAppPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == NativeMenu.MenuProperty && _source is not null)
            AppMenu = e.NewValue as NativeMenu;
    }

    // --- Building -------------------------------------------------------------------------------

    private void PostRebuild()
    {
        if (_rebuildPosted)
            return;
        _rebuildPosted = true;
        Dispatcher.UIThread.Post(() =>
        {
            _rebuildPosted = false;
            Rebuild();
        });
    }

    /// <summary>Builds the titles from the menu: on a new menu, and when its titles change (added, removed, renamed, shown or hidden).</summary>
    private void Rebuild()
    {
        Close();
        _root.Dispose();
        _moreNode.Dispose();
        _root = new Node();
        _moreNode = new Node();
        _natural.Clear();
        _titleSources.Clear();
        _shownTitles = int.MaxValue;
        _foldedApplied = 0;
        Items.Clear();

        if (_source is not null)
        {
            _root.Subs.Add(Observe(_source.Items, PostRebuild));
            if (_appMenu is not null)
            {
                _root.Subs.Add(Observe(_appMenu.Items, PostRebuild));
                // An Application item shown or hidden can add or remove the added Help title.
                foreach (var item in _appMenu.Items.OfType<NativeMenuItem>())
                    _root.Subs.Add(item.GetObservable(NativeMenuItem.IsVisibleProperty).Skip1(PostRebuild));
            }

            var helpHeader = HelpHeaderText();
            var titles = _source.Items.OfType<NativeMenuItem>().ToList();
            var help = titles.LastOrDefault(t => t.IsVisible && SameHeader(t.Header, helpHeader));
            var appItems = _appMenu?.Items.Where(i => i is not NativeMenuItem { IsVisible: false }).ToList() ?? [];
            foreach (var title in titles)
            {
                // A title's own changes that move or rename titles rebuild the row.
                _root.Subs.Add(title.GetObservable(NativeMenuItem.IsVisibleProperty).Skip1(PostRebuild));
                _root.Subs.Add(title.GetObservable(NativeMenuItem.HeaderProperty).Skip1(PostRebuild));
                if (!title.IsVisible)
                    continue;
                var tail = title == help && appItems.Count > 0 ? AppTail(title) : null;
                var built = Title(title, _root, tail);
                _titleSources[built] = (title, tail);
                Items.Add(built);
            }
            if (help is null && appItems.Count > 0)
            {
                var added = AddedHelp(_root);
                _titleSources[added] = (null, AppTail(null));
                Items.Add(added);
            }
        }

        _more.IsVisible = false;
        _more.Items.Clear();
        Items.Add(_more);
    }

    private string HelpHeaderText() =>
        this.TryFindResource(HelpHeaderKey, out var value) && value is string text ? text : HelpHeader;

    // Headers match without their access key marks and case: "_Help" is "Help".
    internal static bool SameHeader(string? a, string? b) =>
        string.Equals(Plain(a), Plain(b), StringComparison.CurrentCultureIgnoreCase);

    private static string Plain(string? header) =>
        (header ?? "").Replace("__", "\0").Replace("_", "").Replace("\0", "_").Trim();

    // The Application's items after the window's Help items: a separator (when Help has items of its own), then the items.
    private Func<Node, IEnumerable<Control>> AppTail(NativeMenuItem? help) => level =>
    {
        var built = new List<Control>();
        if (help?.Menu?.Items.Count > 0)
            built.Add(new Separator());
        foreach (var item in _appMenu?.Items ?? [])
            built.Add(Build(item, level));
        return built;
    };

    // The Help title added to hold the Application's items when the window's menu has none.
    private MenuItem AddedHelp(Node parent)
    {
        var added = new MenuItem();
        added.Bind(HeaderedSelectingItemsControl.HeaderProperty, new DynamicResourceExtension(HelpHeaderKey));
        WindowDecorationProperties.SetElementRole(added, WindowDecorationsElementRole.DecorationsElement);
        foreach (var item in AppTail(null)(parent))
            added.Items.Add(item);
        KeepSeparatorsApart(added, parent);
        return added;
    }

    private MenuItem Title(NativeMenuItem native, Node parent, Func<Node, IEnumerable<Control>>? tail)
    {
        var title = new MenuItem();
        var node = new Node();
        parent.Children.Add(node);
        Link(title, native, node, isTitle: true, tail);
        WindowDecorationProperties.SetElementRole(title, WindowDecorationsElementRole.DecorationsElement);
        return title;
    }

    private Control Build(NativeMenuItemBase item, Node parent)
    {
        if (item is not NativeMenuItem native)
            return new Separator();
        var menuItem = new MenuItem();
        var node = new Node();
        parent.Children.Add(node);
        Link(menuItem, native, node, isTitle: false, tail: null);
        return menuItem;
    }

    /// <summary>Keeps <paramref name="menuItem"/> in step with <paramref name="native"/>: its properties, its submenu, and clicks forwarded to it.</summary>
    private void Link(MenuItem menuItem, NativeMenuItem native, Node node, bool isTitle, Func<Node, IEnumerable<Control>>? tail)
    {
        _natives.AddOrUpdate(menuItem, native);
        node.Subs.Add(menuItem.Bind(HeaderedSelectingItemsControl.HeaderProperty, native.GetObservable(NativeMenuItem.HeaderProperty)));
        node.Subs.Add(menuItem.Bind(IsEnabledProperty, native.GetObservable(NativeMenuItem.IsEnabledProperty)));
        // A title's visibility belongs to the folding; a hidden title is left out of the row instead.
        // A separator's visibility is worked out from its neighbours (KeepSeparatorsApart).
        if (!isTitle)
        {
            if (native is not NativeMenuItemSeparator)
                node.Subs.Add(menuItem.Bind(IsVisibleProperty, native.GetObservable(NativeMenuItem.IsVisibleProperty)));
            node.Subs.Add(menuItem.Bind(MenuItem.ToggleTypeProperty, native.GetObservable(NativeMenuItem.ToggleTypeProperty)));
            node.Subs.Add(menuItem.Bind(MenuItem.IsCheckedProperty, native.GetObservable(NativeMenuItem.IsCheckedProperty)));
            node.Subs.Add(menuItem.Bind(MenuItem.InputGestureProperty, native.GetObservable(NativeMenuItem.GestureProperty)));
            node.Subs.Add(menuItem.Bind(ToolTip.TipProperty, native.GetObservable(NativeMenuItem.ToolTipProperty)));
            node.Subs.Add(menuItem.Bind(MenuItem.IconProperty, native.GetObservable(NativeMenuItem.IconProperty).Map(IconFor)));
        }

        // Clicks bubble up from submenu items; only the item's own click is forwarded.
        EventHandler<Avalonia.Interactivity.RoutedEventArgs> click = (_, e) =>
        {
            if (e.Source == menuItem && !menuItem.HasSubMenu)
                Forward(native, menuItem);
        };
        menuItem.Click += click;
        node.Subs.Add(new Disposable(() => menuItem.Click -= click));

        // The submenu, refilled when the item gets another menu.
        var submenu = new Node();
        node.Children.Add(submenu);
        void FillSubmenu()
        {
            submenu.Dispose();
            menuItem.Items.Clear();
            if (native.Menu is { } menu)
                Fill(menuItem, menu, submenu, tail);
            else if (tail is not null)
            {
                foreach (var item in tail(submenu))
                    menuItem.Items.Add(item);
                KeepSeparatorsApart(menuItem, submenu);
            }
        }
        FillSubmenu();
        node.Subs.Add(native.GetObservable(NativeMenuItem.MenuProperty).Skip1(FillSubmenu));
    }

    /// <summary>Fills <paramref name="target"/> with <paramref name="menu"/>'s items (and the <paramref name="tail"/>), again whenever they change.</summary>
    private void Fill(ItemsControl target, NativeMenu menu, Node owner, Func<Node, IEnumerable<Control>>? tail)
    {
        var level = new Node();
        owner.Children.Add(level);
        void Refill()
        {
            level.Dispose();
            target.Items.Clear();
            foreach (var item in menu.Items)
                target.Items.Add(Build(item, level));
            if (tail is not null)
                foreach (var item in tail(level))
                    target.Items.Add(item);
            KeepSeparatorsApart(target, level);
        }
        Refill();
        owner.Subs.Add(Observe(menu.Items, Refill));
    }

    /// <summary>
    /// Shows a separator only between two shown items, never first, last or next to another shown separator, as native
    /// menus do, and again whenever an item is shown or hidden. The subscriptions belong to <paramref name="owner"/>.
    /// </summary>
    private void KeepSeparatorsApart(ItemsControl target, Node owner)
    {
        NativeMenuItem? SeparatorOf(Control c) =>
            c is MenuItem item && _natives.TryGetValue(item, out var native) && native is NativeMenuItemSeparator ? native : null;
        bool IsSeparator(Control c) => c is Separator || SeparatorOf(c) is not null;
        void Update()
        {
            var items = target.Items.OfType<Control>().ToList();
            var last = items.FindLastIndex(c => !IsSeparator(c) && c.IsVisible);
            var seen = false;
            for (var i = 0; i < items.Count; i++)
            {
                if (IsSeparator(items[i]))
                {
                    var shown = seen && i < last && (SeparatorOf(items[i])?.IsVisible ?? true);
                    items[i].IsVisible = shown;
                    if (shown)
                        seen = false;
                }
                else if (items[i].IsVisible)
                    seen = true;
            }
        }
        foreach (var item in target.Items.OfType<MenuItem>())
            owner.Subs.Add(SeparatorOf(item) is { } separator
                ? separator.GetObservable(NativeMenuItem.IsVisibleProperty).Skip1(Update)
                : item.GetObservable(IsVisibleProperty).Skip1(Update));
        Update();
    }

    /// <summary>
    /// Forwards a click to the native item (its Click and Command). The native items keep the check
    /// state: menu items that checked or unchecked themselves (the clicked one, and its radio
    /// siblings) take the native items' state back, at once and again once the menu has handled the
    /// click, so the app checks and unchecks them, as with a system menu.
    /// </summary>
    private void Forward(NativeMenuItem native, MenuItem menuItem)
    {
        ((INativeMenuItemExporterEventsImplBridge)native).RaiseClicked();
        var group = menuItem.Parent is ItemsControl parent ? parent.Items.OfType<MenuItem>().ToList() : [menuItem];
        void Restore()
        {
            foreach (var item in group)
                if (_natives.TryGetValue(item, out var source) && item.IsChecked != source.IsChecked)
                    item.SetCurrentValue(MenuItem.IsCheckedProperty, source.IsChecked);
        }
        Restore();
        Dispatcher.UIThread.Post(Restore);
    }

    private static object? IconFor(Bitmap? bitmap) => bitmap is null ? null : new Image { Source = bitmap };

    private static IDisposable Observe(IList<NativeMenuItemBase> items, Action changed)
    {
        var collection = (INotifyCollectionChanged)items;
        NotifyCollectionChangedEventHandler handler = (_, _) => changed();
        collection.CollectionChanged += handler;
        return new Disposable(() => collection.CollectionChanged -= handler);
    }

    // --- Folding --------------------------------------------------------------------------------

    /// <summary>
    /// The number of titles that fit in <paramref name="width"/> with their natural widths: all of
    /// them, or as many from the start as fit beside the "…" title of <paramref name="moreWidth"/>.
    /// </summary>
    internal static int TitlesThatFit(IReadOnlyList<double> widths, double moreWidth, double width)
    {
        if (widths.Sum() <= width + 0.5)
            return widths.Count;
        var used = moreWidth;
        var count = 0;
        while (count < widths.Count && used + widths[count] <= width + 0.5)
            used += widths[count++];
        return count;
    }

    private double MoreWidth => double.IsNaN(_more.Width) ? _more.DesiredSize.Width : _more.Width;

    /// <summary>
    /// Applies which titles show, after layout (posted), so layout never loops: folded titles hide,
    /// the "…" title shows when any is folded, and its submenus are rebuilt from the folded titles.
    /// </summary>
    private void PostFold(int shown)
    {
        _shownTitles = shown;
        if (_foldPosted)
            return;
        _foldPosted = true;
        Dispatcher.UIThread.Post(() =>
        {
            _foldPosted = false;
            ApplyFold();
        }, DispatcherPriority.Render);
    }

    private void ApplyFold()
    {
        var titles = Titles;
        var shown = Math.Min(_shownTitles, titles.Count);
        var focusLost = false;
        for (var i = 0; i < titles.Count; i++)
        {
            var visible = i < shown;
            if (titles[i].IsVisible == visible)
                continue;
            focusLost |= !visible && titles[i].IsKeyboardFocusWithin;
            if (!visible)
                titles[i].Close();
            titles[i].IsVisible = visible;
        }

        // The "…" title's submenus: copies of the folded titles, rebuilt when another set folds.
        var folded = titles.Skip(shown).ToList();
        if (folded.Count != _foldedApplied || folded.Count != _more.ItemCount)
        {
            _foldedApplied = folded.Count;
            _more.Close();
            _moreNode.Dispose();
            _moreNode = new Node();
            _more.Items.Clear();
            foreach (var title in folded)
                if (_titleSources.TryGetValue(title, out var source))
                    _more.Items.Add(source.Native is { } native ? Title(native, _moreNode, source.Tail) : AddedHelp(_moreNode));
        }
        _more.IsVisible = folded.Count > 0;
        if (focusLost && _more.IsVisible)
            _more.Focus(NavigationMethod.Directional);
    }

    /// <summary>
    /// Lays the titles out in a row. Every title is measured at its natural width (a folded one keeps
    /// the width it had when shown); titles that no longer fit are laid out empty until the fold is applied.
    /// </summary>
    private sealed class FoldingPanel(MenuRow row) : Panel, INavigableContainer
    {
        // Keyboard moves between the shown titles (and "…"), left and right, as a StackPanel's do.
        IInputElement? INavigableContainer.GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
        {
            var shown = Children.Where(c => c.IsVisible && c.Bounds.Width > 0 && c.IsEffectivelyEnabled).ToList();
            if (shown.Count == 0)
                return null;
            var index = from is Control control ? shown.IndexOf(control) : -1;
            int? next = direction switch
            {
                NavigationDirection.First => 0,
                NavigationDirection.Last => shown.Count - 1,
                NavigationDirection.Next or NavigationDirection.Right => index + 1,
                NavigationDirection.Previous or NavigationDirection.Left => index < 0 ? shown.Count - 1 : index - 1,
                _ => null,
            };
            if (next is not { } i)
                return null;
            if (wrap)
                i = (i + shown.Count) % shown.Count;
            return i >= 0 && i < shown.Count ? shown[i] : null;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var height = 0.0;
            foreach (var child in Children)
            {
                child.Measure(Size.Infinity);
                height = Math.Max(height, child.DesiredSize.Height);
                if (child is MenuItem title && title != row._more && title.IsVisible)
                    row._natural[title] = child.DesiredSize.Width;
            }

            var titles = row.Titles;
            // A title not yet measured (folded before it was shown) shows again to be measured.
            var unmeasured = titles.Any(t => !row._natural.ContainsKey(t));
            var widths = titles.Select(t => row._natural.GetValueOrDefault(t)).ToList();
            var fit = unmeasured ? titles.Count : TitlesThatFit(widths, row.MoreWidth, availableSize.Width);
            var current = titles.Count(t => t.IsVisible);
            if (fit != current || (fit < titles.Count) != row._more.IsVisible || fit != row._shownTitles)
                row.PostFold(fit);

            var width = widths.Take(Math.Min(fit, current)).Sum() + (row._more.IsVisible ? row.MoreWidth : 0);
            return new Size(Math.Min(width, availableSize.Width), height);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var x = 0.0;
            var titles = row.Titles;
            var fit = Math.Min(row._shownTitles, titles.Count);
            foreach (var child in Children)
            {
                var index = child is MenuItem title ? titles.IndexOf(title) : -1;
                if (!child.IsVisible || (index >= 0 && index >= fit))
                {
                    child.Arrange(default);
                    continue;
                }
                child.Arrange(new Rect(x, 0, child.DesiredSize.Width, finalSize.Height));
                x += child.DesiredSize.Width;
            }
            return finalSize;
        }
    }

    // --- Subscriptions --------------------------------------------------------------------------

    /// <summary>The subscriptions of one built item and the items below it, released together when they are rebuilt.</summary>
    private sealed class Node : IDisposable
    {
        public List<IDisposable> Subs { get; } = [];
        public List<Node> Children { get; } = [];

        public void Dispose()
        {
            foreach (var child in Children)
                child.Dispose();
            Children.Clear();
            foreach (var sub in Subs)
                sub.Dispose();
            Subs.Clear();
        }
    }

    private sealed class Disposable(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}

internal static class MenuRowExtensions
{
    /// <summary>Calls <paramref name="changed"/> on every value after the current one.</summary>
    public static IDisposable Skip1<T>(this IObservable<T> source, Action changed) =>
        source.Subscribe(new SkipFirst<T>(changed));

    /// <summary>The values of <paramref name="source"/> mapped by <paramref name="map"/>.</summary>
    public static IObservable<TResult> Map<T, TResult>(this IObservable<T> source, Func<T, TResult> map) =>
        new Mapped<T, TResult>(source, map);

    private sealed class SkipFirst<T>(Action changed) : IObserver<T>
    {
        private bool _first = true;

        public void OnNext(T value)
        {
            if (_first)
                _first = false;
            else
                changed();
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }
    }

    private sealed class Mapped<T, TResult>(IObservable<T> source, Func<T, TResult> map) : IObservable<TResult>
    {
        public IDisposable Subscribe(IObserver<TResult> observer) =>
            source.Subscribe(new Observer(observer, map));

        private sealed class Observer(IObserver<TResult> target, Func<T, TResult> map) : IObserver<T>
        {
            public void OnNext(T value) => target.OnNext(map(value));
            public void OnCompleted() => target.OnCompleted();
            public void OnError(Exception error) => target.OnError(error);
        }
    }
}
