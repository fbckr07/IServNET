namespace IServ.Models;

/// <summary>
/// Options for creating recurring events.
/// </summary>
public class RecurringEventOptions
{
    /// <summary>
    /// The type of recurrence interval.
    /// </summary>
    public IntervalType IntervalType { get; set; } = IntervalType.NO;

    /// <summary>
    /// The interval between recurrences (1-30). Not used for WEEKDAYS or NO.
    /// </summary>
    public int? Interval { get; set; }

    /// <summary>
    /// The day of the month for MONTHLY recurrence with BYMONTHDAY (1-31).
    /// </summary>
    public int? MonthDayInMonth { get; set; }

    /// <summary>
    /// The type of monthly interval when IntervalType is MONTHLY.
    /// </summary>
    public MonthlyIntervalType? MonthlyIntervalType { get; set; }

    /// <summary>
    /// The month interval for BYDAY (1=first, 2=second, 3=third, 4=fourth, -1=last).
    /// </summary>
    public int? MonthInterval { get; set; }

    /// <summary>
    /// The day of week for MONTHLY recurrence with BYDAY.
    /// </summary>
    public WeekDay? MonthDay { get; set; }

    /// <summary>
    /// Days of the week for WEEKLY recurrence.
    /// </summary>
    public List<WeekDay>? RecurrenceDays { get; set; }

    /// <summary>
    /// How the recurrence ends.
    /// </summary>
    public EndType EndType { get; set; } = EndType.NEVER;

    /// <summary>
    /// Number of occurrences when EndType is COUNT.
    /// </summary>
    public int? EndInterval { get; set; }

    /// <summary>
    /// End date when EndType is UNTIL (format: DD.MM.YYYY).
    /// </summary>
    public string? UntilDate { get; set; }
}
