namespace IServ.Models;

/// <summary>
/// Represents the type of monthly interval for recurring events.
/// </summary>
public enum MonthlyIntervalType
{
    /// <summary>
    /// By specific day of month (e.g., 15th).
    /// </summary>
    BYMONTHDAY,

    /// <summary>
    /// By day of week (e.g., second Tuesday).
    /// </summary>
    BYDAY
}
