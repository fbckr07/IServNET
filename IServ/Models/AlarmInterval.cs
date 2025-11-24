namespace IServ.Models;

/// <summary>
/// Represents an interval for custom alarms.
/// </summary>
public class AlarmInterval
{
    /// <summary>
    /// Number of days.
    /// </summary>
    public int Days { get; set; }

    /// <summary>
    /// Number of hours.
    /// </summary>
    public int Hours { get; set; }

    /// <summary>
    /// Number of minutes.
    /// </summary>
    public int Minutes { get; set; }
}
