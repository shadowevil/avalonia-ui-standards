using Avalonia.Automation;
using Avalonia.Controls;
using AvaloniaUiStandards.Blocks;

namespace Catalog.Pages;

/// <summary>SegmentedControl: text and icon segments in every state (hover and press on "Week"), and live controls.</summary>
internal sealed class SegmentedControlPage : UserControl
{
    public SegmentedControlPage() => Content = Ui.Page("Segmented",
        Ui.Section("States", StateGrid.Build(
        [
            ("Text", Text),
            ("Icons", Glyphs),
            ("Segment disabled", () =>
            {
                var control = Text();
                control.Items.OfType<SegmentedControlItem>().Last().IsEnabled = false;
                return control;
            }),
        ], control => ((SegmentedControl)control).ContainerFromIndex(1) ?? control)),
        Ui.Section("Use", Ui.Caption(
            "One of a few options for one setting, about five to seven at most; text or icons, not mixed. Icon segments carry a tooltip " +
            "and an automation name. Tab reaches the control once; the arrow keys, Home and End move and select.")));

    private static SegmentedControl Text()
    {
        var control = new SegmentedControl();
        foreach (var label in new[] { "Day", "Week", "Month", "Year" })
            control.Items.Add(new SegmentedControlItem { Content = label });
        control.SelectedIndex = 0;
        return control;
    }

    private static SegmentedControl Glyphs()
    {
        var control = new SegmentedControl();
        foreach (var (name, data) in new[] { ("List", Icons.ListBullets), ("Grid", Icons.SquaresFour), ("Sidebar", Icons.SidebarSimple) })
        {
            var item = new SegmentedControlItem { Content = new Icon { Data = data } };
            ToolTip.SetTip(item, name);
            AutomationProperties.SetName(item, name);
            control.Items.Add(item);
        }
        control.SelectedIndex = 0;
        return control;
    }
}
