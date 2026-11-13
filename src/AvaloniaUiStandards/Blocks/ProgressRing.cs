using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace AvaloniaUiStandards.Blocks;

/// <summary>
/// A circular progress indicator, the round counterpart of <see cref="ProgressBar"/> for small or
/// inline places (in a button, beside a label). Determinate, an accent arc grows clockwise from the
/// top with <see cref="RangeBase.Value"/> between <see cref="RangeBase.Minimum"/> and
/// <see cref="RangeBase.Maximum"/>; indeterminate (<see cref="IsIndeterminate"/>), a quarter arc
/// turns. It is 20 px at Normal density and 16 px at Compact, or any size set through
/// <c>Width</c> and <c>Height</c>.
/// </summary>
[PseudoClasses(":indeterminate")]
public class ProgressRing : RangeBase
{
    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        ProgressBar.IsIndeterminateProperty.AddOwner<ProgressRing>();

    public ProgressRing() => UpdatePseudoClasses();

    /// <summary>Whether the ring shows activity of unknown length instead of an amount.</summary>
    public bool IsIndeterminate
    {
        get => GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsIndeterminateProperty)
            UpdatePseudoClasses();
    }

    private void UpdatePseudoClasses() => PseudoClasses.Set(":indeterminate", IsIndeterminate);
}
