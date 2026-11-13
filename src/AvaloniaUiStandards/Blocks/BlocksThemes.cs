using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
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
        resources[typeof(MenuItem)] = MenuTheme.Item();
        resources[typeof(Menu)] = MenuTheme.Menu(fluentTheme(typeof(Menu)));
        resources[typeof(ContextMenu)] = MenuTheme.MenuOverlay(typeof(ContextMenu), fluentTheme(typeof(ContextMenu)));
        resources[typeof(MenuFlyoutPresenter)] = MenuTheme.MenuOverlay(typeof(MenuFlyoutPresenter), fluentTheme(typeof(MenuFlyoutPresenter)));
        resources[typeof(FlyoutPresenter)] = MenuTheme.FlyoutPresenter(fluentTheme(typeof(FlyoutPresenter)));
        resources[typeof(ToolTip)] = ToolTipTheme.Create(fluentTheme(typeof(ToolTip)));
        resources[typeof(WindowNotificationManager)] = NotificationTheme.Manager(fluentTheme(typeof(WindowNotificationManager)));
        resources[typeof(NotificationCard)] = NotificationTheme.Card();
        resources[typeof(Separator)] = SeparatorTheme.Create();
        resources[typeof(GridSplitter)] = GridSplitterTheme.Create();
        var fieldButton = FieldParts.ButtonTheme();
        resources[typeof(TextBox)] = TextInputTheme.Create(fieldButton);
        var innerTextBox = TextInputTheme.CreateInner(fieldButton);
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
        resources[typeof(DataGrid)] = DataGridTheme.Grid(fluentTheme(typeof(DataGrid)));
        resources[typeof(DataGridRow)] = DataGridTheme.Row();
        resources[typeof(DataGridCell)] = DataGridTheme.Cell();
        resources[typeof(DataGridColumnHeader)] = DataGridTheme.ColumnHeader();
        resources[typeof(DataGridRowHeader)] = DataGridTheme.RowHeader(fluentTheme(typeof(DataGridRowHeader)));
        resources[DataGridTheme.CellTextBlockThemeKey] = DataGridTheme.CellTextBlock();
        resources[DataGridTheme.CellTextBoxThemeKey] = DataGridTheme.CellTextBox((ControlTheme)resources[typeof(TextBox)]!);
        resources[typeof(RadioButton)] = RadioButtonTheme.Create();
        resources[typeof(ToggleSwitch)] = ToggleSwitchTheme.Create();
        resources[typeof(Slider)] = SliderTheme.Create();
        resources[typeof(ProgressBar)] = ProgressBarTheme.Create();
        resources[typeof(ProgressRing)] = ProgressRingTheme.Create();
        var pickerItem = PickerThemes.Item();
        var pickerStep = PickerThemes.StepButton();
        resources[typeof(DatePicker)] = PickerThemes.DatePicker(fluentTheme(typeof(DatePicker)));
        resources[typeof(TimePicker)] = PickerThemes.TimePicker(fluentTheme(typeof(TimePicker)));
        resources[typeof(DatePickerPresenter)] = PickerThemes.Presenter(typeof(DatePickerPresenter), fluentTheme(typeof(DatePickerPresenter)), pickerItem, pickerStep);
        resources[typeof(TimePickerPresenter)] = PickerThemes.Presenter(typeof(TimePickerPresenter), fluentTheme(typeof(TimePickerPresenter)), pickerItem, pickerStep);
        resources[typeof(Calendar)] = CalendarThemes.Calendar(fluentTheme(typeof(Calendar)));
        resources[typeof(CalendarItem)] = CalendarThemes.Item(fluentTheme(typeof(CalendarItem)));
        resources[typeof(CalendarDayButton)] = CalendarThemes.DayButton();
        resources[typeof(CalendarButton)] = CalendarThemes.MonthButton();
        resources[typeof(CalendarDatePicker)] = CalendarDatePickerTheme.Create(fluentTheme(typeof(CalendarDatePicker)), fieldButton, innerTextBox);
        ColorPickerThemes.AddResources(resources);
        resources[typeof(ColorView)] = ColorPickerThemes.ColorView();
        resources[typeof(ColorPicker)] = ColorPickerThemes.ColorPicker();
        resources[typeof(ColorSpectrum)] = ColorPartsThemes.Spectrum();
        resources[typeof(ColorSlider)] = ColorPartsThemes.Slider();
        resources[typeof(ColorPreviewer)] = ColorPartsThemes.Previewer();
        resources[typeof(TrafficLightButton)] = WindowButtonThemes.TrafficLight();
        resources[typeof(CaptionButton)] = WindowButtonThemes.Caption();
        WindowButtonThemes.AddResources(resources);
    }

    /// <summary>Blocks styles that depend on where a control sits, added to the theme after the control themes.</summary>
    public static IEnumerable<IStyle> Styles() => [SeparatorTheme.InMenuStyle(), .. DataGridTheme.Styles(DataGridTheme.CellCheckBox())];

    /// <summary>
    /// Sets the transitions of the Blocks themes for the current motion: <paramref name="fast"/>
    /// is <c>MotionFast</c>, <paramref name="normal"/> is <c>MotionNormal</c> and <paramref name="slow"/>
    /// is <c>MotionSlow</c>, or all zero under reduce motion (no transitions).
    /// </summary>
    public static void ApplyMotion(IResourceDictionary resources, TimeSpan fast, TimeSpan normal, TimeSpan slow, Easing easing)
    {
        resources[OverlayParts.FadeInFastKey] = OverlayParts.FadeIn(fast, easing);
        resources[OverlayParts.FadeInSlowKey] = OverlayParts.FadeIn(slow, easing);
        resources[OverlayParts.FadeOutSlowKey] = OverlayParts.FadeOut(slow, easing);
        resources[ButtonParts.ChromeTransitionsKey] = ButtonParts.ChromeTransitions(fast, easing);
        resources[ButtonParts.FadeTransitionsKey] = ButtonParts.FadeTransitions(fast, easing);
        resources[ScrollBarTheme.PartTransitionsKey] = ScrollBarTheme.PartTransitions(fast, easing);
        resources[SelectionIndicator.TransitionsKey] = SelectionIndicator.Slide(normal, easing);
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
        var iconSize = density == DensityStyle.Compact ? IconTheme.Compact : IconTheme.Normal;
        resources[IconTheme.SizeKey] = iconSize;
        resources[NotificationTheme.CloseMarginKey] = NotificationTheme.CloseMargin(sizes.MinHeight, iconSize);
        FieldParts.ApplyDensity(resources, density);
        WindowButtonThemes.ApplyDensity(resources, density == DensityStyle.Compact);
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
        DataGridTheme.ApplyDensity(resources, density == DensityStyle.Compact);
        PickerThemes.ApplyDensity(resources, density);
        CalendarThemes.ApplyDensity(resources, density);
        ColorPartsThemes.ApplyDensity(resources, density);
    }
}
