using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using AvaloniaUiStandards;

namespace Catalog;

/// <summary>The live theme controls: each one sets a property of the application's <see cref="StandardsTheme"/>.</summary>
public partial class ThemeToolbar : UserControl
{
    private static readonly (string Name, string Hex)[] Accents =
    [
        ("Default", Hex(StandardsTheme.DefaultAccent)),
        ("Teal", "#00899E"),
        ("Green", "#2E8B3E"),
        ("Orange", "#E0720B"),
        ("Purple", "#7A4FD6"),
        ("Pink", "#E91E63"),
    ];

    private readonly StandardsTheme? _theme = Application.Current?.Styles.OfType<StandardsTheme>().FirstOrDefault();
    private bool _syncing;

    public ThemeToolbar()
    {
        InitializeComponent();
        AccentBox.ItemsSource = Accents.Select(a => a.Name).ToList();
        AccentBox.SelectedIndex = 0;
        AccentHex.Text = Accents[0].Hex;

        ThemeBox.SelectionChanged += (_, _) => Application.Current!.RequestedThemeVariant =
            ThemeBox.SelectedIndex switch { 1 => ThemeVariant.Light, 2 => ThemeVariant.Dark, _ => ThemeVariant.Default };
        DensityBox.SelectionChanged += (_, _) =>
        {
            if (_theme is not null)
                _theme.Density = DensityBox.SelectedIndex == 1 ? DensityStyle.Compact : DensityStyle.Normal;
        };
        MotionBox.SelectionChanged += (_, _) =>
        {
            if (_theme is not null)
                _theme.ReduceMotion = Tri(MotionBox.SelectedIndex);
        };
        TransparencyBox.SelectionChanged += (_, _) =>
        {
            if (_theme is not null)
                _theme.ReduceTransparency = Tri(TransparencyBox.SelectedIndex);
        };
        AccentBox.SelectionChanged += (_, _) =>
        {
            if (!_syncing && AccentBox.SelectedIndex >= 0)
                AccentHex.Text = Accents[AccentBox.SelectedIndex].Hex;
        };
        AccentHex.TextChanged += (_, _) => ApplyHex();
    }

    private static string Hex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    // Follow OS (null), On, Off.
    private static bool? Tri(int index) => index switch { 1 => true, 2 => false, _ => null };

    private void ApplyHex()
    {
        var text = AccentHex.Text?.Trim() ?? "";
        var valid = text.StartsWith('#') && text.Length is 7 && Color.TryParse(text, out _);
        if (!valid && Application.Current!.FindResource("DangerColor") is Color danger)
            AccentHex.Foreground = new SolidColorBrush(danger);
        else
            AccentHex.ClearValue(TemplatedControl.ForegroundProperty);
        if (!valid || _theme is null)
            return;

        _theme.Accent = Color.Parse(text);
        _syncing = true;
        AccentBox.SelectedIndex = Array.FindIndex(Accents, a => a.Hex.Equals(text, StringComparison.OrdinalIgnoreCase));
        _syncing = false;
    }
}
