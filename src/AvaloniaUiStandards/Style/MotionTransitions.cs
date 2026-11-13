using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;

namespace AvaloniaUiStandards.Style;

/// <summary>
/// Binds template transitions to the motion resources, so their duration follows
/// <c>Motion*</c> (zero under reduce motion) and their easing follows <c>MotionEasing</c>, live.
/// </summary>
internal static class MotionTransitions
{
    /// <summary>
    /// Returns <paramref name="transitions"/> as a <see cref="Transitions"/>, each bound to the
    /// <paramref name="durationKey"/> and <c>MotionEasing</c> resources as seen from <paramref name="host"/>.
    /// </summary>
    public static Transitions Create(StyledElement host, string durationKey, params TransitionBase[] transitions)
    {
        var result = new Transitions();
        foreach (var transition in transitions)
        {
            transition.Bind(TransitionBase.DurationProperty, host.GetResourceObservable(durationKey));
            transition.Bind(TransitionBase.EasingProperty, host.GetResourceObservable("MotionEasing"));
            result.Add(transition);
        }
        return result;
    }
}
