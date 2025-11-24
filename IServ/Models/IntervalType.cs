namespace IServ.Models;

/// <summary>
/// Represents the type of interval for recurring events.
/// </summary>
public enum IntervalType
{
    /// <summary>
    /// No recurrence.
    /// </summary>
    NO,

    /// <summary>
    /// Daily recurrence.
    /// </summary>
    DAILY,

    /// <summary>
    /// Weekdays recurrence (Monday to Friday).
    /// </summary>
    WEEKDAYS,

    /// <summary>
    /// Weekly recurrence.
    /// </summary>
    WEEKLY,

    /// <summary>
    /// Monthly recurrence.
    /// </summary>
    MONTHLY,

    /// <summary>
    /// Yearly recurrence.
    /// </summary>
    YEARLY
}
