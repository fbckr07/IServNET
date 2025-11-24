namespace IServ.Models;

/// <summary>
/// Represents a user search result.
/// </summary>
public class UserSearchResult
{
    /// <summary>
    /// The username.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL to the user's profile.
    /// </summary>
    public string UserUrl { get; set; } = string.Empty;
}
