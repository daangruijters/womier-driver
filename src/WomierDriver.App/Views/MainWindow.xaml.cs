using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using WomierDriver.App.Services;
using WomierDriver.App.ViewModels;

namespace WomierDriver.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly AppServices _services;
    private RawKeyboardInput? _rawInput;

    public MainWindow(MainViewModel viewModel, AppServices services)
    {
        _viewModel = viewModel;
        _services = services;
        DataContext = viewModel;
        InitializeComponent();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        Loaded += (_, _) => UpdateLegend();
        _viewModel.LiveViewChanged += on =>
        {
            if (on)
            {
                Keyboard.ClearFocus();
            }

            UpdateRawInputRegistration();
        };
        IsVisibleChanged += (_, _) =>
        {
            if (!IsVisible)
            {
                _viewModel.OnWindowHidden();
            }

            UpdateRawInputRegistration();
        };
        StateChanged += (_, _) => UpdateRawInputRegistration();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        if (PresentationSource.FromVisual(this) is HwndSource source)
        {
            _rawInput = new RawKeyboardInput(source);
            _rawInput.KeyChanged += _viewModel.OnPhysicalKey;
            UpdateRawInputRegistration();
        }
    }

    /// <summary>Raw Input is registered only while the live view is on and the window is actually on screen.</summary>
    private void UpdateRawInputRegistration()
    {
        if (_rawInput is null)
        {
            return;
        }

        var wanted = _viewModel.IsLiveViewEnabled && IsVisible && WindowState != WindowState.Minimized;
        try
        {
            if (wanted)
            {
                _rawInput.Register();
            }
            else
            {
                _rawInput.Unregister();
            }
        }
        catch (Exception ex)
        {
            _services.Log.Error("Raw Input registration failed", ex);
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (ShouldSwallowKey(e))
        {
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyDown(e);
    }

    protected override void OnPreviewKeyUp(KeyEventArgs e)
    {
        if (ShouldSwallowKey(e))
        {
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyUp(e);
    }

    /// <summary>While the live view is on, key presses are for the keyboard picture, not for the focused control.</summary>
    private bool ShouldSwallowKey(KeyEventArgs e)
        => _viewModel.IsLiveViewEnabled
           && Keyboard.FocusedElement is not TextBoxBase
           && !Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)
           && e.Key != Key.System;

    /// <summary>Paints the "sensitive … deep" legend with the same colour scale as the keys.</summary>
    private void UpdateLegend()
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 0),
        };
        for (var i = 0; i <= 4; i++)
        {
            var t = i / 4.0;
            if (KeyPalette.Fill(t, false) is SolidColorBrush solid)
            {
                brush.GradientStops.Add(new GradientStop(solid.Color, t));
            }
        }

        LegendBar.Background = brush;
    }

    /// <summary>Set by the application right before a real exit; otherwise closing hides to the tray.</summary>
    public bool AllowClose { get; set; }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            Hide();
            _services.FlushPendingSaves();
            return;
        }

        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _rawInput?.Dispose();
        base.OnClosing(e);
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color or UserPreferenceCategory.VisualStyle)
        {
            Dispatcher.BeginInvoke(() =>
            {
                KeyPalette.Invalidate();
                foreach (var key in _viewModel.Keys)
                {
                    key.RefreshVisuals();
                }

                UpdateLegend();
            });
        }
    }

    private void OnKeySelectionChanged(object sender, RoutedEventArgs e) => _viewModel.SelectionChanged();

    private void OnMoreClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.DataContext = DataContext;
            menu.IsOpen = true;
        }
    }

    private void OnTitleKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Escape && sender is TextBox box)
        {
            var binding = box.GetBindingExpression(TextBox.TextProperty);
            if (e.Key == Key.Escape)
            {
                binding?.UpdateTarget();
            }
            else
            {
                binding?.UpdateSource();
            }

            Keyboard.ClearFocus();
            e.Handled = true;
        }
    }

    private void OnGameTextKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _viewModel.AddGameCommand.CanExecute(null))
        {
            _viewModel.AddGameCommand.Execute(null);
            e.Handled = true;
        }
    }
}
