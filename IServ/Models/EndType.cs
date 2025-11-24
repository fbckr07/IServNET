namespace IServ.Models;

/// <summary>
/// Represents when a recurring event should end.
/// </summary>
public enum EndType
{
    /// <summary>
    /// Never ends.
    /// </summary>
    NEVER,

    /// <summary>
    /// Ends after a specific count of occurrences.
    /// </summary>
    COUNT,

    /// <summary>
    /// Ends on a specific date.
    /// </summary>
    UNTIL
}
