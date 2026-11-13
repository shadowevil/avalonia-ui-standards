using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using AvaloniaUiStandards.Blocks;
using Xunit;

namespace AvaloniaUiStandards.Tests;

/// <summary>Renders the Slider, ProgressBar and ProgressRing sheets (light/dark × density, 2x) to screenshots/.</summary>
public class RangeRenderTests
{
    [AvaloniaFact]
    public void Slider_RendersSheet() => ToggleRenderTests.Render("Slider: states × light/dark × density", "m3-slider.png",
    [
        ("Value 40", () => new Slider { Width = 160, Value = 40 }),
        ("Ticks, value 60", () => new Slider { Width = 160, Value = 60, TickFrequency = 10, TickPlacement = TickPlacement.Outside }),
        ("Reversed, value 30", () => new Slider { Width = 160, Value = 30, IsDirectionReversed = true }),
        ("Vertical, value 70", () => new Slider { Height = 100, Value = 70, Orientation = Orientation.Vertical }),
    ]);

    [AvaloniaFact]
    public void Progress_RendersSheet()
    {
        (string Label, Func<Control> Create)[] rows =
        [
            ("ProgressBar 0", () => new ProgressBar { Width = 160, Value = 0 }),
            ("ProgressBar 40", () => new ProgressBar { Width = 160, Value = 40 }),
            ("ProgressBar 100", () => new ProgressBar { Width = 160, Value = 100 }),
            ("ProgressBar 2", () => new ProgressBar { Width = 160, Value = 2 }),
            ("ProgressBar 40, text", () => new ProgressBar { Width = 160, Value = 40, ShowProgressText = true }),
            ("Indeterminate, still", () => new ProgressBar { Width = 160, IsIndeterminate = true }),
            ("Indeterminate, 0.7 s", () => Sweep(new ProgressBar { Width = 160, IsIndeterminate = true }, 0.7)),
            ("Indeterminate, 1.2 s", () => Sweep(new ProgressBar { Width = 160, IsIndeterminate = true }, 1.2)),
            ("Vertical 60", () => new ProgressBar { Height = 80, Value = 60, Orientation = Orientation.Vertical }),
            ("ProgressRing 25", () => new ProgressRing { Value = 25 }),
            ("ProgressRing 70", () => new ProgressRing { Value = 70 }),
            ("ProgressRing 100", () => new ProgressRing { Value = 100 }),
            ("Ring indeterminate, still", () => new ProgressRing { IsIndeterminate = true }),
            ("Ring indeterminate, 0.3 s", () => Sweep(new ProgressRing { IsIndeterminate = true }, 0.3)),
            ("Ring 32 px, 70", () => new ProgressRing { Value = 70, Width = 32, Height = 32 }),
            ("Ring 48 px, indeterminate", () => new ProgressRing { IsIndeterminate = true, Width = 48, Height = 48 }),
        ];
        CellState[] states = [CellState.Rest, CellState.Disabled];
        using var sheet = new Sheet("ProgressBar and ProgressRing: light/dark × density (indeterminate frames at set times)");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
            sheet.AddSection(themeLabel, ToggleRenderTests.Densities.SelectMany(d => states.Select(s => $"{d.Label}, {s}")).ToList(),
                rows.Select(r => (r.Label, (IReadOnlyList<Control>)ToggleRenderTests.Densities
                    .SelectMany(d => states.Select(s => StateCells.Render(sheet, theme, r.Create(), s, d.BeforeShow))).ToList())));
        sheet.Save("m3-progress.png");
    }

    [AvaloniaFact]
    public void BeforeAfter_RendersFluentNextToLibrary()
    {
        (string Label, Func<Control> Create)[] controls =
        [
            ("Slider", () => new Slider { Width = 160, Value = 40 }),
            ("ProgressBar", () => new ProgressBar { Width = 160, Value = 40 }),
        ];
        using var sheet = new Sheet("Slider and ProgressBar: Fluent default (before) and the library (after)");
        foreach (var (themeLabel, theme) in ButtonRenderTests.Themes)
        {
            var rows = new List<(string, IReadOnlyList<Control>)>();
            foreach (var (label, create) in controls)
            {
                rows.Add(($"Fluent {label}", StateCells.All.Select(s => StateCells.Render(sheet, theme, create(), s, w => w.Styles.Add(new FluentTheme()))).ToList()));
                rows.Add(($"Library {label}", StateCells.All.Select(s => StateCells.Render(sheet, theme, create(), s)).ToList()));
            }
            sheet.AddSection(themeLabel, StateCells.All.Select(s => s.ToString()).ToList(), rows);
        }
        sheet.Save("m3-ranges-before-after.png");
    }

    /// <summary>Sets the control's indeterminate animation to <paramref name="seconds"/> once it is shown.</summary>
    internal static Control Sweep(Control control, double seconds)
    {
        control.AttachedToVisualTree += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var clock = control.GetVisualDescendants().OfType<ProgressSweep>().Select(s => s.Clock)
                .Concat(control.GetVisualDescendants().OfType<RingArc>().Select(a => a.Clock)).First();
            clock.Set(TimeSpan.FromSeconds(seconds));
        });
        return control;
    }
}
