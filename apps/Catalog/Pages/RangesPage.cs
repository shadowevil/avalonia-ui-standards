using Avalonia.Controls;
using Avalonia.Layout;
using AvaloniaUiStandards.Blocks;

namespace Catalog.Pages;

/// <summary>The ranges: Slider, ProgressBar and ProgressRing.</summary>
internal sealed class RangesPage : UserControl
{
    public RangesPage() => Content = Ui.Page("Ranges",
        Ui.Section("Slider", StateGrid.Build(
        [
            ("Value 40", () => new Slider { Width = 160, Value = 40 }),
            ("With ticks", () => new Slider { Width = 160, Value = 60, TickFrequency = 10, TickPlacement = TickPlacement.Outside }),
            ("Vertical", () => new Slider { Height = 100, Value = 70, Orientation = Orientation.Vertical }),
        ])),
        Ui.Section("ProgressBar", StateGrid.Build(
        [
            ("Value 40", () => new ProgressBar { Width = 160, Value = 40 }),
            ("With text", () => new ProgressBar { Width = 160, Value = 40, ShowProgressText = true }),
            ("Indeterminate", () => new ProgressBar { Width = 160, IsIndeterminate = true }),
        ])),
        Ui.Section("ProgressRing", StateGrid.Build(
        [
            ("Value 70", () => new ProgressRing { Value = 70 }),
            ("Indeterminate", () => new ProgressRing { IsIndeterminate = true }),
            ("32 px, indeterminate", () => new ProgressRing { IsIndeterminate = true, Width = 32, Height = 32 }),
        ])));
}
