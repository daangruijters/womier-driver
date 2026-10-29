using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WomierDriver.App.ViewModels;

namespace WomierDriver.App.Views;

/// <summary>
/// Draws the keyboard and handles selection: click, Ctrl/Shift-click to add or toggle, drag a rectangle to
/// select many keys at once, right-click a key to select only that key.
/// </summary>
public partial class KeyboardView : UserControl
{
    public static readonly DependencyProperty KeysProperty =
        DependencyProperty.Register(nameof(Keys), typeof(IEnumerable), typeof(KeyboardView), new PropertyMetadata(null));

    public static readonly RoutedEvent SelectionChangedEvent =
        EventManager.RegisterRoutedEvent(nameof(SelectionChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(KeyboardView));

    private const double DragThreshold = 4;

    private Point _pressPoint;
    private KeyViewModel? _pressedKey;
    private bool _pressed;
    private bool _dragging;
    private HashSet<KeyViewModel>? _selectionBeforeDrag;

    public KeyboardView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    public IEnumerable? Keys
    {
        get => (IEnumerable?)GetValue(KeysProperty);
        set => SetValue(KeysProperty, value);
    }

    private IEnumerable<KeyViewModel> KeyModels => Keys?.OfType<KeyViewModel>() ?? [];

    private void OnSurfaceMouseDown(object sender, MouseButtonEventArgs e)
    {
        _pressPoint = e.GetPosition(Surface);
        _pressedKey = HitKey(e.OriginalSource as DependencyObject);
        _pressed = true;
        _dragging = false;
        Surface.CaptureMouse();
        e.Handled = true;
    }

    private void OnSurfaceMouseMove(object sender, MouseEventArgs e)
    {
        if (!_pressed || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var current = e.GetPosition(Surface);
        if (!_dragging)
        {
            if (Math.Abs(current.X - _pressPoint.X) < DragThreshold && Math.Abs(current.Y - _pressPoint.Y) < DragThreshold)
            {
                return;
            }

            _dragging = true;
            _selectionBeforeDrag = IsAdditive() ? KeyModels.Where(k => k.IsSelected).ToHashSet() : [];
            Marquee.Visibility = Visibility.Visible;
        }

        var rect = new Rect(_pressPoint, current);
        Canvas.SetLeft(Marquee, rect.X);
        Canvas.SetTop(Marquee, rect.Y);
        Marquee.Width = rect.Width;
        Marquee.Height = rect.Height;

        foreach (var key in KeyModels)
        {
            var keyRect = new Rect(key.X, key.Y, key.Width, key.Height);
            key.IsSelected = rect.IntersectsWith(keyRect) || (_selectionBeforeDrag?.Contains(key) ?? false);
        }
    }

    private void OnSurfaceMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_pressed)
        {
            return;
        }

        _pressed = false;
        Surface.ReleaseMouseCapture();

        if (_dragging)
        {
            _dragging = false;
            Marquee.Visibility = Visibility.Collapsed;
            _selectionBeforeDrag = null;
            RaiseEvent(new RoutedEventArgs(SelectionChangedEvent));
            return;
        }

        var key = _pressedKey ?? HitKey(e.OriginalSource as DependencyObject);
        if (key is null)
        {
            if (!IsAdditive())
            {
                foreach (var k in KeyModels)
                {
                    k.IsSelected = false;
                }
            }
        }
        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            key.IsSelected = !key.IsSelected;
        }
        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            key.IsSelected = true;
        }
        else
        {
            foreach (var k in KeyModels)
            {
                k.IsSelected = ReferenceEquals(k, key);
            }
        }

        RaiseEvent(new RoutedEventArgs(SelectionChangedEvent));
    }

    private void OnSurfaceRightClick(object sender, MouseButtonEventArgs e)
    {
        var key = HitKey(e.OriginalSource as DependencyObject);
        if (key is null)
        {
            return;
        }

        if (!key.IsSelected)
        {
            foreach (var k in KeyModels)
            {
                k.IsSelected = ReferenceEquals(k, key);
            }

            RaiseEvent(new RoutedEventArgs(SelectionChangedEvent));
        }
    }

    private static bool IsAdditive() => Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);

    private static KeyViewModel? HitKey(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is FrameworkElement { Tag: KeyViewModel key })
            {
                return key;
            }

            source = source is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(source) : LogicalTreeHelper.GetParent(source);
        }

        return null;
    }
}
