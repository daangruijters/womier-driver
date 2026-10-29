namespace WomierDriver.Core.Automation;

/// <summary>One live travel observation: which key moved and how deep it is pressed now.</summary>
public readonly record struct LiveTravelSample(int Slot, ushort Raw, double Mm);

/// <summary>Produces live travel samples for a connected keyboard. Events are raised on background threads.</summary>
public interface ILiveTravelSource : IDisposable
{
    event Action<LiveTravelSample>? Sample;

    /// <summary>Raised when the source stops; the exception is <c>null</c> when it was disposed on purpose.</summary>
    event Action<Exception?>? Stopped;

    void Start();
}

/// <summary>State of the live view as shown by the UI.</summary>
public enum LiveViewState
{
    /// <summary>Not requested.</summary>
    Off,

    /// <summary>Requested, but no keyboard or no stream right now; re-armed automatically when the keyboard connects.</summary>
    Waiting,

    /// <summary>The keyboard is sending travel reports.</summary>
    Streaming,
}
