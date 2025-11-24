namespace IServ.Models;

/// <summary>
/// Represents how the event should show on the calendar.
/// </summary>
public enum ShowMeAs
{
    /// <summary>
    /// Shows as busy (blocks time).
    /// </summary>
    OPAQUE,

    /// <summary>
    /// Shows as available (transparent).
    /// </summary>
    TRANSPARENT
}
