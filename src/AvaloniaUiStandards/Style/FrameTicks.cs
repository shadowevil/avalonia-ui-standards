using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// A top level that paces continuous animation (decision 0016): <c>StandardsWindow</c>, through
/// its <c>FrameTick</c>. Lower tiers reach the window's frame ticks through this seam, so a Block
/// can animate continuously without depending on the Modules tier.
/// </summary>
internal interface IFrameTickSource
{
    /// <summary>
    /// Calls <paramref name="tick"/> on each frame tick, with the time since the previous tick (zero
    /// on the first tick after a start or resume), until the result is disposed.
    /// </summary>
    IDisposable SubscribeFrameTicks(Action<TimeSpan> tick);
}

internal static class FrameTicks
{
    /// <summary>The frame tick source of the window <paramref name="control"/> is in (through a popup's parent), or null.</summary>
    public static IFrameTickSource? For(Control control)
    {
        var top = TopLevel.GetTopLevel(control);
        while (top is PopupRoot popup)
            top = popup.ParentTopLevel;
        return top as IFrameTickSource;
    }

    /// <summary>A subscription that runs <paramref name="dispose"/> once.</summary>
    public static IDisposable Subscription(Action dispose) => new Once(dispose);

    private sealed class Once(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose()
        {
            _dispose?.Invoke();
            _dispose = null;
        }
    }
}
