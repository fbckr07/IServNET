namespace IServ.Models;

/// <summary>
/// Represents user information from IServ.
/// </summary>
public class UserInfo
{
    /// <summary>
    /// Dictionary of groups the user belongs to (name -> URL).
    /// </summary>
    public Dictionary<string, string> Groups { get; set; } = new();

    /// <summary>
    /// List of roles assigned to the user.
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// List of rights assigned to the user.
    /// </summary>
    public List<string> Rights { get; set; } = new();

    /// <summary>
    /// Public profile information.
    /// </summary>
    public PublicInfo PublicInfo { get; set; } = new();
}

/// <summary>
/// Represents public profile information.
/// </summary>
public class PublicInfo
{
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Birthday { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Zipcode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Icq { get; set; } = string.Empty;
    public string Jabber { get; set; } = string.Empty;
    public string Msn { get; set; } = string.Empty;
    public string Skype { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string MobilePhone { get; set; } = string.Empty;
    public string Fax { get; set; } = string.Empty;
    public string Mail { get; set; } = string.Empty;
    public string Homepage { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}
