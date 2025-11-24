namespace IServ.Models;

/// <summary>
/// Represents the privacy level of an event.
/// </summary>
public enum Privacy
{
    /// <summary>
    /// Public event visible to everyone.
    /// </summary>
    PUBLIC,

    /// <summary>
    /// Confidential event with limited visibility.
    /// </summary>
    CONFIDENTIAL,

    /// <summary>
    /// Private event only visible to owner.
    /// </summary>
    PRIVATE
}
