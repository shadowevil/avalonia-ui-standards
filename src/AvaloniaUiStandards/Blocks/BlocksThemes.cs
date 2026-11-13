using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace AvaloniaUiStandards.Blocks;

/// <summary>The control themes of the Blocks tier and their density-dependent values.</summary>
internal static class BlocksThemes
{
    /// <param name="fluentTheme">Fluent's theme for a control type, for themes that build on Fluent's.</param>
    public static void AddTo(IResourceDictionary resources, Func<Type, ControlTheme?> fluentTheme)
    {
        resources[typeof(ListBox)] = ListItemTheme.ListBox(fluentTheme(typeof(ListBox)));
        resources[typeof(ListBoxItem)] = ListItemTheme.Item(typeof(ListBoxItem));
        resources[typeof(ComboBoxItem)] = ListItemTheme.Item(typeof(ComboBoxItem));
        resources[typeof(TreeView)] = TreeViewTheme.TreeView(fluentTheme(typeof(TreeView)));
        resources[typeof(TreeViewItem)] = TreeViewTheme.Item();
        resources[typeof(ScrollViewer)] = ScrollViewerTheme.Create(fluentTheme(typeof(ScrollViewer)));
        ScrollViewerTheme.AddResources(resources);
        resources[typeof(ScrollBar)] = ScrollBarTheme.Create(fluentTheme(typeof(ScrollBar)));
        ScrollBarTheme.AddResources(resources);

        resources[typeof(Button)] = ButtonTheme.Create();
        resources[typeof(RepeatButton)] = RepeatButtonTheme.Create();
        resources[typeof(ToggleButton)] = ToggleButtonTheme.Create();
        resources[typeof(HyperlinkButton)] = HyperlinkButtonTheme.Create();
        resources[typeof(DropDownButton)] = DropDownButtonTheme.Create();
        resources[typeof(SplitButton)] = SplitButtonTheme.Create();
        resources[typeof(Icon)] = IconTheme.Create();
        var fieldButton = FieldParts.ButtonTheme();
        resources[typeof(TextBox)] = TextInputTheme.Create(fieldButton);
        var innerTextBox = TextInputTheme.CreateInner(fieldButton);
        resources[typeof(CalendarDatePicker)] = TextInputTheme.KeepInnerTextBoxBare(typeof(CalendarDatePicker), fluentTheme(typeof(CalendarDatePicker)), innerTextBox);
        resources[typeof(ComboBox)] = ComboBoxTheme.Create(innerTextBox);
        resources[typeof(ButtonSpinner)] = SpinnerThemes.ButtonSpinner(fieldButton);
        resources[typeof(AutoCompleteBox)] = AutoCompleteBoxTheme.Create(fluentTheme(typeof(AutoCompleteBox)));
        resources[typeof(NumericUpDown)] = SpinnerThemes.NumericUpDown(SpinnerThemes.Template(fieldButton), innerTextBox);
        SegmentedControlTheme.AddResources(resources);
        resources[typeof(SegmentedControl)] = SegmentedControlTheme.Control();
        resources[typeof(SegmentedControlItem)] = SegmentedControlTheme.Segment();
        resources[typeof(TabControl)] = TabThemes.TabControl();
        resources[typeof(TabItem)] = TabThemes.Tab(typeof(TabItem));
        resources[typeof(TabStrip)] = TabThemes.TabStrip();
        resources[typeof(TabStripItem)] = TabThemes.Tab(typeof(TabStripItem));
        resources[typeof(Expander)] = ExpanderTheme.Create();
        resources[typeof(CheckBox)] = CheckBoxTheme.Create();
        resources[typeof(RadioButton)] = RadioButtonTheme.Create();
        resources[typeof(ToggleSwitch)] = ToggleSwitchTheme.Create();
        resources[typeof(Slider)] = SliderTheme.Create();
        resources[typeof(ProgressBar)] = ProgressBarTheme.Create();
        resources[typeof(ProgressRing)] = ProgressRingTheme.Create();
        resources[typeof(TrafficLightButton)] = WindowButtonThemes.TrafficLight();
        resources[typeof(CaptionButton)] = WindowButtonThemes.Caption();
        WindowButtonThemes.AddResources(resources);
    }

    /// <summary>
    /// Sets the transitions of the Blocks themes for the current motion: <paramref name="fast"/>
    /// is <c>MotionFast</c> and <paramref name="normal"/> is <c>MotionNormal</c>, or both zero
    /// under reduce motion (no transitions).
    /// </summary>
    public static void ApplyMotion(IResourceDictionary resources, TimeSpan fast, TimeSpan normal, Easing easing)
    {
        resources[ButtonParts.ChromeTransitionsKey] = ButtonParts.ChromeTransitions(fast, easing);
        resources[ButtonParts.FadeTransitionsKey] = ButtonParts.FadeTransitions(fast, easing);
        resources[ScrollBarTheme.PartTransitionsKey] = ScrollBarTheme.PartTransitions(fast, easing);
        resources[SelectionIndicator.TransitionsKey] = SelectionIndicator.Slide(normal, easing);
        resources[SegmentedControlTheme.TextTransitionsKey] = SegmentedControlTheme.TextTransitions(normal, easing);
        resources[Reveal.TransitionsKey] = Reveal.Opening(normal, easing);
        resources[ExpanderTheme.CaretTransitionsKey] = ExpanderTheme.CaretTransitions(normal, easing);
        resources[ToggleParts.PartTransitionsKey] = ToggleParts.PartTransitions(fast, normal, easing);
        resources[ToggleParts.KnobTransitionsKey] = ToggleParts.KnobTransitions(normal, easing);
        resources[TreeViewTheme.CaretTransitionsKey] = TreeViewTheme.CaretTransitions(normal, easing);
    }

    /// <summary>Sets the density-dependent values of the Blocks themes.</summary>
    public static void ApplyDensity(IResourceDictionary resources, DensityStyle density)
    {
        var sizes = density == DensityStyle.Compact ? ButtonParts.Compact : ButtonParts.Normal;
        resources[ButtonParts.MinHeightKey] = sizes.MinHeight;
        resources[ButtonParts.PaddingKey] = sizes.Padding;
        resources[ButtonParts.GlyphMarginKey] = new Thickness(0, 0, sizes.Padding.Right, 0);
        resources[IconTheme.SizeKey] = density == DensityStyle.Compact ? IconTheme.Compact : IconTheme.Normal;
        FieldParts.ApplyDensity(resources, density);
        resources[ComboBoxTheme.PaddingKey] = ComboBoxTheme.Padding(sizes.Padding);
        var segment = SegmentedControlTheme.Item(sizes);
        resources[SegmentedControlTheme.ItemMinHeightKey] = segment.MinHeight;
        resources[SegmentedControlTheme.ItemPaddingKey] = segment.Padding;
        var side = sizes.Padding.Left;
        resources[TabThemes.ContentPaddingKey] = new Thickness(side, Style.Spacing.M);
        resources[ExpanderTheme.ContentPaddingKey] = new Thickness(side);
        ToggleParts.ApplyDensity(resources, density == DensityStyle.Compact ? ToggleParts.Compact : ToggleParts.Normal);
        RangeParts.ApplyDensity(resources, density == DensityStyle.Compact ? RangeParts.Compact : RangeParts.Normal);
        TreeViewTheme.ApplyDensity(resources, density == DensityStyle.Compact);
    }
}
