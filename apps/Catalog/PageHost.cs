using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Catalog;

/// <summary>
/// The scroller every page is shown in. It scrolls vertically only, so a page always wraps to the
/// available width. The window and the render tests both host pages in it.
/// </summary>
internal sealed class PageHost : ScrollViewer
{
    public PageHost()
    {
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
    }

    protected override Type StyleKeyOverride => typeof(ScrollViewer);
}
