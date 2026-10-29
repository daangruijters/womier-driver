using System.Diagnostics;

namespace WomierDriver.Core.Automation;

/// <summary>Normalizes and matches "game" rules, which are process image names such as "bf6.exe".</summary>
public static class GameRule
{
    /// <summary>Trims, strips any directory, lower-cases and appends ".exe" when no extension is present.</summary>
    public static string Normalize(string rule)
    {
        var name = rule.Trim().Trim('"');
        if (name.Length == 0)
        {
            return string.Empty;
        }

        var slash = name.LastIndexOfAny(['\\', '/']);
        if (slash >= 0)
        {
            name = name[(slash + 1)..];
        }

        name = name.ToLowerInvariant();
        if (!name.Contains('.'))
        {
            name += ".exe";
        }

        return name;
    }

    public static bool Matches(string rule, IReadOnlySet<string> runningProcesses) => runningProcesses.Contains(Normalize(rule));
}

/// <summary>Takes a snapshot of the running process image names, lower-case with ".exe" appended.</summary>
public static class ProcessSnapshot
{
    public static HashSet<string> GetRunningProcessNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                names.Add(process.ProcessName.ToLowerInvariant() + ".exe");
            }
            catch
            {
                // A process can exit between enumeration and inspection.
            }
            finally
            {
                process.Dispose();
            }
        }

        return names;
    }
}

/// <summary>Polls the process list on a timer and raises <see cref="Changed"/> when the set of names changes.</summary>
public sealed class ProcessWatcher : IDisposable
{
    private readonly TimeSpan _interval;
    private CancellationTokenSource? _cts;
    private Task? _loop;
    private HashSet<string> _current = new(StringComparer.OrdinalIgnoreCase);

    public ProcessWatcher(TimeSpan interval)
    {
        _interval = interval < TimeSpan.FromMilliseconds(250) ? TimeSpan.FromMilliseconds(250) : interval;
    }

    public event Action<IReadOnlySet<string>>? Changed;

    public IReadOnlySet<string> Current => _current;

    public void Start()
    {
        if (_loop is not null)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _loop = Task.Run(() => LoopAsync(token), token);
    }

    /// <summary>Samples immediately; returns true when the set changed.</summary>
    public bool Refresh()
    {
        try
        {
            var snapshot = ProcessSnapshot.GetRunningProcessNames();
            if (snapshot.SetEquals(_current))
            {
                return false;
            }

            _current = snapshot;
            Changed?.Invoke(snapshot);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task LoopAsync(CancellationToken cancellationToken)
    {
        Refresh();
        using var timer = new PeriodicTimer(_interval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                Refresh();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
