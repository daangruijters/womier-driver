using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace WomierDriver.App.Views;

/// <summary>
/// A labelled slider with a numeric box, in millimetres. Slider drags update the value live but commit to the
/// bound property only on release (or on typed input), so a drag does not flood the keyboard with writes.
/// </summary>
public partial class SettingSlider : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(SettingSlider), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HintProperty =
        DependencyProperty.Register(nameof(Hint), typeof(string), typeof(SettingSlider), new PropertyMetadata(null));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(double), typeof(SettingSlider),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(nameof(Minimum), typeof(double), typeof(SettingSlider), new PropertyMetadata(0.0));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(SettingSlider), new PropertyMetadata(1.0));

    public static readonly DependencyProperty StepProperty =
        DependencyProperty.Register(nameof(Step), typeof(double), typeof(SettingSlider), new PropertyMetadata(0.01));

    public static readonly DependencyProperty IsMixedProperty =
        DependencyProperty.Register(nameof(IsMixed), typeof(bool), typeof(SettingSlider), new PropertyMetadata(false, OnMixedChanged));

    private bool _syncing;
    private bool _dragging;

    public SettingSlider()
    {
        InitializeComponent();
        ValueSlider.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler((_, _) => _dragging = true));
        SyncFromValue();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Hint
    {
        get => (string?)GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Step
    {
        get => (double)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public bool IsMixed
    {
        get => (bool)GetValue(IsMixedProperty);
        set => SetValue(IsMixedProperty, value);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((SettingSlider)d).SyncFromValue();

    private static void OnMixedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((SettingSlider)d).SyncFromValue();

    private void SyncFromValue()
    {
        if (_syncing)
        {
            return;
        }

        _syncing = true;
        try
        {
            ValueSlider.Value = Value;
            ValueBox.Text = IsMixed ? string.Empty : Format(Value);
        }
        finally
        {
            _syncing = false;
        }
    }

    private void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_syncing)
        {
            return;
        }

        var snapped = Snap(e.NewValue);
        _syncing = true;
        try
        {
            ValueBox.Text = Format(snapped);
        }
        finally
        {
            _syncing = false;
        }

        // Keyboard arrows / click-to-point commit immediately; drags commit on release.
        if (!_dragging)
        {
            Commit(snapped);
        }
    }

    private void OnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        _dragging = false;
        Commit(Snap(ValueSlider.Value));
    }

    private void OnValueBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OnValueBoxCommit(sender, e);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            SyncFromValue();
            e.Handled = true;
        }
    }

    private void OnValueBoxCommit(object sender, RoutedEventArgs e)
    {
        if (_syncing)
        {
            return;
        }

        var text = ValueBox.Text.Trim().Replace(',', '.');
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            Commit(Snap(parsed));
        }
        else
        {
            SyncFromValue();
        }
    }

    private void Commit(double value)
    {
        if (!IsMixed && Math.Abs(value - Value) < 0.00001)
        {
            SyncFromValue();
            return;
        }

        _syncing = true;
        try
        {
            IsMixed = false;
            Value = value;
            ValueSlider.Value = value;
            ValueBox.Text = Format(value);
        }
        finally
        {
            _syncing = false;
        }
    }

    private double Snap(double value)
    {
        var step = Step <= 0 ? 0.01 : Step;
        var snapped = Math.Round(value / step) * step;
        snapped = Math.Round(snapped, 4);
        return Math.Clamp(snapped, Minimum, Maximum);
    }

    private string Format(double value) => value.ToString(Step < 0.01 ? "0.000" : "0.00", CultureInfo.InvariantCulture);
}
