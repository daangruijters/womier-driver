namespace WomierDriver.Core.Hid;

/// <summary>
/// The 2.4 GHz receiver reports that the keyboard is not connected: it sleeps, is out of range or is switched to its
/// cable. Not a device failure: the receiver is fine and the keyboard comes back on the next key press.
/// </summary>
public sealed class KeyboardAsleepException : Exception
{
    public KeyboardAsleepException()
        : base("The keyboard is asleep; press a key to wake it.")
    {
    }

    public KeyboardAsleepException(string message)
        : base(message)
    {
    }

    public KeyboardAsleepException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
