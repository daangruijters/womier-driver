using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WomierDriver.App.Views;

/// <summary>Lets the user pick one or more running processes to attach to a profile.</summary>
public partial class PickProcessWindow : Window
{
    private List<ProcessRow> _all = [];

    public PickProcessWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            Reload();
            SearchBox.Focus();
        };
    }

    public IReadOnlyList<string> SelectedProcesses { get; private set; } = [];

    private void Reload()
    {
        var rows = new Dictionary<string, ProcessRow>(StringComparer.OrdinalIgnoreCase);
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var name = process.ProcessName.ToLowerInvariant() + ".exe";
                string title;
                try
                {
                    title = process.MainWindowTitle;
                }
                catch
                {
                    title = string.Empty;
                }

                if (rows.TryGetValue(name, out var existing))
                {
                    if (existing.Title.Length == 0 && title.Length > 0)
                    {
                        rows[name] = existing with { Title = title };
                    }
                }
                else
                {
                    rows[name] = new ProcessRow(name, title);
                }
            }
            catch
            {
                // Process exited while being inspected.
            }
            finally
            {
                process.Dispose();
            }
        }

        _all = rows.Values.OrderByDescending(r => r.Title.Length > 0).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var query = SearchBox.Text.Trim();
        var showAll = ShowAll.IsChecked == true;
        ProcessList.ItemsSource = _all
            .Where(r => showAll || r.Title.Length > 0)
            .Where(r => query.Length == 0 || r.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || r.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void OnFilterChanged(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            ApplyFilter();
        }
    }

    private void OnRefresh(object sender, RoutedEventArgs e) => Reload();

    private void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ProcessList.SelectedItem is not null)
        {
            OnAccept(sender, e);
        }
    }

    private void OnAccept(object sender, RoutedEventArgs e)
    {
        SelectedProcesses = ProcessList.SelectedItems.OfType<ProcessRow>().Select(r => r.Name).ToList();
        if (SelectedProcesses.Count == 0)
        {
            return;
        }

        DialogResult = true;
    }

    private sealed record ProcessRow(string Name, string Title);
}
