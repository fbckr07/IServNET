/*
* This package is a C# port of the Python package IServAPI.
* Original author: Leo-Aqua
* License: MIT License (see LICENSE)
*/

using System.Net;
using System.Text;
using System.Web;
using HtmlAgilityPack;
using IServ.Exceptions;
using IServ.Models;
using MailKit.Net.Smtp;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace IServ;

static class Logger
{
    public static void Info(string message) => Console.WriteLine($"INFO: {message}");
    public static void Debug(string message) => Console.WriteLine($"DEBUG: {message}");
    public static void Warn(string message) => Console.WriteLine($"WARN: {message}");
    public static void Error(Exception? ex, string message)
    {
        if (ex != null)
            Console.WriteLine($"ERROR: {message} - {ex}");
        else
            Console.WriteLine($"ERROR: {message}");
    }
}

/// <summary>
/// Main API class for interacting with IServ system.
/// </summary>
public class IServApi : IDisposable
{
    
    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookieContainer;
    private readonly string _username;
    private readonly string _password;
    private readonly string _iservUrl;
    private string? _iservSAT;
    private string? _iservSATId;
    private string? _iservSession;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the IServApi class and logs in.
    /// </summary>
    /// <param name="username">The IServ username.</param>
    /// <param name="password">The IServ password.</param>
    /// <param name="iservUrl">The IServ URL (e.g., "school.iserv.de").</param>
    public IServApi(string username, string password, string iservUrl)
    {
        _username = username;
        _password = password;
        _iservUrl = iservUrl;
        _cookieContainer = new CookieContainer();

        var handler = new HttpClientHandler
        {
            CookieContainer = _cookieContainer,
            UseCookies = true,
            AllowAutoRedirect = true
        };

        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");

        LoginAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Logs in to the IServ system.
    /// </summary>
    private async Task LoginAsync()
    {
        try
        {
            var loginUrl = $"https://{_iservUrl}/iserv/auth/login";

            // First request to get login page
            var response = await _httpClient.GetAsync(loginUrl);
            response.EnsureSuccessStatusCode();

            // Submit login credentials
            var loginData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("_username", _username),
                new KeyValuePair<string, string>("_password", _password)
            });

            response = await _httpClient.PostAsync(loginUrl, loginData);
            var content = await response.Content.ReadAsStringAsync();

            // Check for error messages
            if (content.Contains("Account existiert nicht!"))
            {
                throw new IServException("Account does not exist!");
            }

            if (content.Contains("Anmeldung fehlgeschlagen!"))
            {
                throw new IServException("Login failed! Probably wrong password.");
            }

            // Navigate to home and main page to get cookies
            await _httpClient.GetAsync($"https://{_iservUrl}/iserv/auth/home");
            await _httpClient.GetAsync($"https://{_iservUrl}/iserv/");
            await _httpClient.GetAsync($"https://{_iservUrl}/iserv/");

            // Extract cookies
            ExtractCookies();

            Logger.Info("Login successful");
        }
        catch (HttpRequestException ex)
        {
            throw new IServException("Error establishing connection", ex);
        }
    }

    /// <summary>
    /// Extracts session cookies from the cookie container.
    /// </summary>
    private void ExtractCookies()
    {
        var cookies = _cookieContainer.GetCookies(new Uri($"https://{_iservUrl}"));
        
        foreach (Cookie cookie in cookies)
        {
            if (cookie.Name == "IServSAT")
                _iservSAT = cookie.Value;
            else if (cookie.Name == "IServSATId")
                _iservSATId = cookie.Value;
            else if (cookie.Name == "IServSession")
                _iservSession = cookie.Value;
        }

        Logger.Info("Cookies extracted successfully");
    }

    #region User Management

    /// <summary>
    /// Retrieves the current user's information.
    /// </summary>
    /// <returns>A UserInfo object containing the user's information.</returns>
    public async Task<UserInfo> GetOwnUserInfoAsync()
    {
        try
        {
            var userInfo = new UserInfo();

            // Get main profile page
            var profileResponse = await _httpClient.GetAsync($"https://{_iservUrl}/iserv/profile");
            var profileHtml = await profileResponse.Content.ReadAsStringAsync();

            var doc = new HtmlDocument();
            doc.LoadHtml(profileHtml);

            // Parse groups
            var groupsList = doc.DocumentNode.SelectNodes("//div[@class='panel-body']//ul[1]//a");
            if (groupsList != null)
            {
                foreach (var groupNode in groupsList)
                {
                    var text = groupNode.InnerText.Trim();
                    var href = groupNode.GetAttributeValue("href", "");
                    userInfo.Groups[text] = href;
                }
            }

            // Parse roles
            var rolesList = doc.DocumentNode.SelectNodes("//div[@class='panel-body']//ul[2]//li");
            if (rolesList != null)
            {
                foreach (var roleNode in rolesList)
                {
                    userInfo.Roles.Add(roleNode.InnerText.Trim());
                }
            }

            // Parse rights
            var rightsList = doc.DocumentNode.SelectNodes("//div[@class='panel-body']//ul[3]//li");
            if (rightsList != null)
            {
                foreach (var rightNode in rightsList)
                {
                    userInfo.Rights.Add(rightNode.InnerText.Trim());
                }
            }

            // Get public information from edit pages
            await LoadPublicInfoAsync(userInfo.PublicInfo);

            Logger.Info("Retrieved own user info");
            return userInfo;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error retrieving user information");
            throw new IServException("Error retrieving user information", ex);
        }
    }

    /// <summary>
    /// Loads public profile information.
    /// </summary>
    private async Task LoadPublicInfoAsync(PublicInfo publicInfo)
    {
        var urls = new[]
        {
            $"https://{_iservUrl}/iserv/profile/public/edit#data",
            $"https://{_iservUrl}/iserv/profile/public/edit#address",
            $"https://{_iservUrl}/iserv/profile/public/edit#contact",
            $"https://{_iservUrl}/iserv/profile/public/edit#instant",
            $"https://{_iservUrl}/iserv/profile/public/edit#note"
        };

        var tasks = urls.Select(url => _httpClient.GetStringAsync(url)).ToArray();
        var responses = await Task.WhenAll(tasks);

        // Parse data section
        ParsePublicInfoSection(responses[0], publicInfo, new[]
        {
            ("publiccontact_title", "Title"),
            ("publiccontact_company", "Company"),
            ("publiccontact_birthday", "Birthday"),
            ("publiccontact_nickname", "Nickname"),
            ("publiccontact_class", "Class")
        });

        // Parse address section
        ParsePublicInfoSection(responses[1], publicInfo, new[]
        {
            ("publiccontact_street", "Street"),
            ("publiccontact_zipcode", "Zipcode"),
            ("publiccontact_city", "City"),
            ("publiccontact_country", "Country")
        });

        // Parse contact section
        ParsePublicInfoSection(responses[2], publicInfo, new[]
        {
            ("publiccontact_phone", "Phone"),
            ("publiccontact_mobilePhone", "MobilePhone"),
            ("publiccontact_fax", "Fax"),
            ("publiccontact_mail", "Mail"),
            ("publiccontact_homepage", "Homepage")
        });

        // Parse instant messaging section
        ParsePublicInfoSection(responses[3], publicInfo, new[]
        {
            ("publiccontact_icq", "Icq"),
            ("publiccontact_jabber", "Jabber"),
            ("publiccontact_msn", "Msn"),
            ("publiccontact_skype", "Skype")
        });

        // Parse note section
        var noteDoc = new HtmlDocument();
        noteDoc.LoadHtml(responses[4]);
        var noteNode = noteDoc.DocumentNode.SelectSingleNode("//textarea[@id='publiccontact_note']");
        publicInfo.Note = noteNode?.InnerText ?? string.Empty;

        // Get token from contact section
        var tokenDoc = new HtmlDocument();
        tokenDoc.LoadHtml(responses[2]);
        var tokenNode = tokenDoc.DocumentNode.SelectSingleNode("//input[@id='publiccontact__token']");
        publicInfo.Token = tokenNode?.GetAttributeValue("value", string.Empty) ?? string.Empty;

        Logger.Info("Loaded public info");
    }

    /// <summary>
    /// Parses a section of public info.
    /// </summary>
    private void ParsePublicInfoSection(string html, PublicInfo publicInfo, (string id, string property)[] fields)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        foreach (var (id, property) in fields)
        {
            var node = doc.DocumentNode.SelectSingleNode($"//input[@id='{id}']");
            var value = node?.GetAttributeValue("value", string.Empty) ?? string.Empty;

            var prop = typeof(PublicInfo).GetProperty(property);
            prop?.SetValue(publicInfo, value);
        }
    }

    /// <summary>
    /// Updates the current user's profile information.
    /// </summary>
    /// <param name="updates">Dictionary of field names to update with their new values.</param>
    /// <returns>The HTTP status code of the response.</returns>
    public async Task<int> SetOwnUserInfoAsync(Dictionary<string, string> updates)
    {
        try
        {
            // Get current user info
            var userInfo = await GetOwnUserInfoAsync();
            var publicInfo = userInfo.PublicInfo;

            // Build form data
            var formData = new Dictionary<string, string>
            {
                ["publiccontact[title]"] = publicInfo.Title,
                ["publiccontact[company]"] = publicInfo.Company,
                ["publiccontact[birthday]"] = publicInfo.Birthday,
                ["publiccontact[nickname]"] = publicInfo.Nickname,
                ["publiccontact[class]"] = publicInfo.Class,
                ["publiccontact[street]"] = publicInfo.Street,
                ["publiccontact[zipcode]"] = publicInfo.Zipcode,
                ["publiccontact[city]"] = publicInfo.City,
                ["publiccontact[country]"] = publicInfo.Country,
                ["publiccontact[phone]"] = publicInfo.Phone,
                ["publiccontact[mobilePhone]"] = publicInfo.MobilePhone,
                ["publiccontact[fax]"] = publicInfo.Fax,
                ["publiccontact[mail]"] = publicInfo.Mail,
                ["publiccontact[homepage]"] = publicInfo.Homepage,
                ["publiccontact[icq]"] = publicInfo.Icq,
                ["publiccontact[jabber]"] = publicInfo.Jabber,
                ["publiccontact[msn]"] = publicInfo.Msn,
                ["publiccontact[skype]"] = publicInfo.Skype,
                ["publiccontact[note]"] = publicInfo.Note,
                ["publiccontact[hidden]"] = "0",
                ["publiccontact[actions][submit]"] = "",
                ["publiccontact[_token]"] = HttpUtility.UrlEncode(publicInfo.Token)
            };

            // Apply updates
            foreach (var (key, value) in updates)
            {
                var formKey = $"publiccontact[{key}]";
                if (formData.ContainsKey(formKey))
                {
                    formData[formKey] = value;
                    Logger.Info($"Changed {key} to {value}");
                }
            }

            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync(
                $"https://{_iservUrl}/iserv/profile/public/edit",
                content);

            Logger.Info("Public info changed successfully");
            return (int)response.StatusCode;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error setting user information");
            throw new IServException("Error setting user information", ex);
        }
    }

    /// <summary>
    /// Searches for users by query string.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <returns>A list of matching users.</returns>
    public async Task<List<UserSearchResult>> SearchUsersAsync(string query)
    {
        try
        {
            var encodedQuery = HttpUtility.UrlEncode(query);
            var response = await _httpClient.GetAsync(
                $"https://{_iservUrl}/iserv/addressbook/public?filter%5Bsearch%5D={encodedQuery}");

            var html = await response.Content.ReadAsStringAsync();

            if (html.Contains("Too many results") || html.Contains("Zu viele Treffer"))
            {
                throw new IServException("Too many results, please restrict filter criteria!");
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var results = new List<UserSearchResult>();
            var rows = doc.DocumentNode.SelectNodes("//table//tbody//tr");

            if (rows != null)
            {
                foreach (var row in rows)
                {
                    var aTag = row.SelectSingleNode(".//a");
                    if (aTag != null)
                    {
                        results.Add(new UserSearchResult
                        {
                            Name = aTag.InnerText.Trim(),
                            UserUrl = aTag.GetAttributeValue("href", "")
                        });
                    }
                }
            }

            Logger.Info("Searched users");
            return results;
        }
        catch (Exception ex) when (ex is not IServException)
        {
            Logger.Error(ex, "Error searching users");
            throw new IServException("Error searching users", ex);
        }
    }

    /// <summary>
    /// Searches for users with autocomplete functionality.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="limit">Maximum number of results (default 50).</param>
    /// <returns>JSON array of matching users.</returns>
    public async Task<JArray> SearchUsersAutocompleteAsync(string query, int limit = 50)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(
                $"https://{_iservUrl}/iserv/core/autocomplete/api?type=list,mail&query={query}&limit={limit}");

            Logger.Info("Searched users (autocomplete)");
            return JArray.Parse(response);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error in autocomplete search");
            throw new IServException("Error in autocomplete search", ex);
        }
    }

    /// <summary>
    /// Gets detailed information about a specific user.
    /// </summary>
    /// <param name="username">The username to look up.</param>
    /// <returns>Dictionary containing user information.</returns>
    public async Task<Dictionary<string, string>> GetUserInfoAsync(string username)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(
                $"https://{_iservUrl}/iserv/addressbook/public/show/{username}");

            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            var table = doc.DocumentNode.SelectSingleNode("//table");
            if (table == null)
            {
                throw new IServException("No such user found!");
            }

            var result = new Dictionary<string, string>();
            var rows = table.SelectNodes(".//tr");

            if (rows != null)
            {
                foreach (var row in rows)
                {
                    var cells = row.SelectNodes(".//td");
                    if (cells != null && cells.Count >= 2)
                    {
                        var key = cells[0].InnerText.Trim();
                        var value = cells[1].InnerText.Trim();
                        result[key] = value;
                    }
                }
            }

            Logger.Info($"Got info of user {username}");
            return result;
        }
        catch (Exception ex) when (ex is not IServException)
        {
            Logger.Error(ex, "Error getting user info");
            throw new IServException($"Error getting user info for {username}", ex);
        }
    }

    /// <summary>
    /// Downloads a user's profile picture.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="outputFolder">The folder to save the picture to.</param>
    public async Task GetUserProfilePictureAsync(string username, string outputFolder)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"https://{_iservUrl}/iserv/core/avatar/user/{username}");

            var content = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType;

            var filePath = Path.Combine(outputFolder, username);
            
            if (contentType?.Contains("svg") == true)
            {
                filePath += ".svg";
                await File.WriteAllTextAsync(filePath, Encoding.UTF8.GetString(content));
            }
            else
            {
                filePath += ".webp";
                await File.WriteAllBytesAsync(filePath, content);
            }

            Logger.Info($"Downloaded profile picture for {username}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error downloading profile picture");
            throw new IServException("Error downloading profile picture", ex);
        }
    }

    #endregion

    #region Notifications & Badges

    /// <summary>
    /// Retrieves notifications for the current user.
    /// </summary>
    /// <returns>JSON object containing notifications.</returns>
    public async Task<JObject> GetNotificationsAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(
                $"https://{_iservUrl}/iserv/user/api/notifications");

            Logger.Info("Got notifications");
            return JObject.Parse(response);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting notifications");
            throw new IServException("Error getting notifications", ex);
        }
    }

    /// <summary>
    /// Retrieves badge information.
    /// </summary>
    /// <returns>JSON object containing badges.</returns>
    public async Task<JObject> GetBadgesAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(
                $"https://{_iservUrl}/iserv/app/navigation/badges");

            Logger.Info("Got badges");
            return JObject.Parse(response);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting badges");
            throw new IServException("Error getting badges", ex);
        }
    }

    /// <summary>
    /// Marks all notifications as read.
    /// </summary>
    /// <returns>Response from the server.</returns>
    public async Task<HttpResponseMessage> ReadAllNotificationsAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"https://{_iservUrl}/iserv/notification/api/v1/notifications/readall",
                null);

            Logger.Info("Read all notifications");
            return response;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error marking notifications as read");
            throw new IServException("Error marking notifications as read", ex);
        }
    }

    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    /// <param name="notificationId">The notification ID.</param>
    /// <returns>Response from the server.</returns>
    public async Task<HttpResponseMessage> ReadNotificationAsync(int notificationId)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"https://{_iservUrl}/iserv/notification/api/v1/notifications/{notificationId}/read",
                null);

            Logger.Info($"Read notification {notificationId}");
            return response;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error marking notification as read");
            throw new IServException("Error marking notification as read", ex);
        }
    }

    #endregion

    #region Email

    /// <summary>
    /// Retrieves emails from the specified mailbox folder.
    /// </summary>
    /// <param name="path">The mailbox folder path (default: INBOX).</param>
    /// <param name="length">Number of emails to retrieve (default: 50).</param>
    /// <param name="start">Starting index (default: 0).</param>
    /// <param name="order">Order by column (default: date).</param>
    /// <param name="dir">Direction (asc/desc, default: desc).</param>
    /// <returns>JSON object containing email list.</returns>
    public async Task<JObject> GetEmailsAsync(
        string path = "INBOX",
        int length = 50,
        int start = 0,
        string order = "date",
        string dir = "desc")
    {
        try
        {
            var response = await _httpClient.GetStringAsync(
                $"https://{_iservUrl}/iserv/mail/api/message/list?path={path}&length={length}&start={start}&order%5Bcolumn%5D={order}&order%5Bdir%5D={dir}");

            Logger.Info("Got emails successfully");
            return JObject.Parse(response);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting emails");
            throw new IServException("Error getting emails", ex);
        }
    }

    /// <summary>
    /// Gets metadata for emails.
    /// </summary>
    /// <param name="path">The mailbox folder path.</param>
    /// <param name="length">Number of emails.</param>
    /// <param name="start">Starting index.</param>
    /// <param name="order">Order by column.</param>
    /// <param name="dir">Direction.</param>
    /// <returns>JSON object with email info.</returns>
    public async Task<JObject> GetEmailInfoAsync(
        string path = "INBOX",
        int length = 0,
        int start = 0,
        string order = "date",
        string dir = "desc")
    {
        return await GetEmailsAsync(path, length, start, order, dir);
    }

    /// <summary>
    /// Gets the raw source of an email message.
    /// </summary>
    /// <param name="uid">The email UID.</param>
    /// <param name="path">The mailbox folder path (default: INBOX).</param>
    /// <returns>The email source as a string.</returns>
    public async Task<string> GetEmailSourceAsync(int uid, string path = "INBOX")
    {
        try
        {
            var response = await _httpClient.GetStringAsync(
                $"https://{_iservUrl}/iserv/mail/show/source?path={path}&msg={uid}");

            Logger.Info("Got email source");
            return response;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting email source");
            throw new IServException("Error getting email source", ex);
        }
    }

    /// <summary>
    /// Gets the list of mail folders.
    /// </summary>
    /// <returns>JSON object containing mail folders.</returns>
    public async Task<JObject> GetMailFoldersAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(
                $"https://{_iservUrl}/iserv/mail/api/folder/list");

            Logger.Info("Got email folders");
            return JObject.Parse(response);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting mail folders");
            throw new IServException("Error getting mail folders", ex);
        }
    }

    /// <summary>
    /// Sends an email via SMTP.
    /// </summary>
    /// <param name="receiverEmail">Recipient email address.</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="body">Plain text body.</param>
    /// <param name="htmlBody">HTML body (optional).</param>
    /// <param name="smtpServer">SMTP server (optional, defaults to IServ URL).</param>
    /// <param name="smtpsPort">SMTPS port (default: 465).</param>
    /// <param name="attachments">List of file paths to attach (optional).</param>
    public async Task SendEmailAsync(
        string receiverEmail,
        string subject,
        string body,
        string? htmlBody = null,
        string? smtpServer = null,
        int smtpsPort = 465,
        List<string>? attachments = null)
    {
        try
        {
            smtpServer ??= _iservUrl;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_username, $"{_username}@{_iservUrl}"));
            message.To.Add(MailboxAddress.Parse(receiverEmail));
            message.Subject = subject;

            var builder = new BodyBuilder { TextBody = body };

            if (!string.IsNullOrEmpty(htmlBody))
            {
                builder.HtmlBody = htmlBody;
            }

            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    builder.Attachments.Add(attachment);
                    Logger.Debug($"Added attachment: {attachment}");
                }
            }

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpServer, smtpsPort, true);
            await client.AuthenticateAsync(_username, _password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            Logger.Info($"Email sent successfully via SMTPS (port {smtpsPort})");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to send email");
            throw new IServException("Failed to send email", ex);
        }
    }

    #endregion

    #region Calendar

    /// <summary>
    /// Gets upcoming calendar events.
    /// </summary>
    /// <returns>JSON object containing upcoming events.</returns>
    public async Task<JObject> GetUpcomingEventsAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(
                $"https://{_iservUrl}/iserv/calendar/api/upcoming");

            Logger.Info("Got upcoming events");
            return JObject.Parse(response);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting upcoming events");
            throw new IServException("Error getting upcoming events", ex);
        }
    }

    /// <summary>
    /// Gets calendar event sources.
    /// </summary>
    /// <returns>JSON object containing event sources.</returns>
    public async Task<JObject> GetEventSourcesAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(
                $"https://{_iservUrl}/iserv/calendar/api/eventsources");

            Logger.Info("Got event sources");
            return JObject.Parse(response);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting event sources");
            throw new IServException("Error getting event sources", ex);
        }
    }

    /// <summary>
    /// Gets events within a date range.
    /// </summary>
    /// <param name="start">Start date.</param>
    /// <param name="end">End date.</param>
    /// <returns>JSON array containing events.</returns>
    public async Task<JArray> GetEventsAsync(DateTime start, DateTime end)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(
                $"https://{_iservUrl}/iserv/calendar/feed/calendar-multi?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}");

            Logger.Info($"Got calendar events from {start:yyyy-MM-dd} to {end:yyyy-MM-dd}");
            return JArray.Parse(response);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting events");
            throw new IServException("Error getting events", ex);
        }
    }

    /// <summary>
    /// Searches for events by query and date range.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="start">Start date.</param>
    /// <param name="end">End date.</param>
    /// <returns>JSON array of matching events.</returns>
    public async Task<JArray> SearchEventAsync(string query, DateTime start, DateTime end)
    {
        try
        {
            var startStr = start.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z";
            var endStr = end.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z";

            var response = await _httpClient.GetStringAsync(
                $"https://{_iservUrl}/iserv/calendar/api/lookup_event?summary={query}&start={startStr}&end={endStr}");

            Logger.Info($"Looked up calendar events with query '{query}'");
            return JArray.Parse(response);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error searching events");
            throw new IServException("Error searching events", ex);
        }
    }

    /// <summary>
    /// Gets events from a specific calendar plugin.
    /// </summary>
    /// <param name="plugin">Plugin name.</param>
    /// <param name="start">Start date.</param>
    /// <param name="end">End date.</param>
    /// <returns>JSON array of plugin events.</returns>
    public async Task<JArray> GetCalendarPluginEventsAsync(string plugin, DateTime start, DateTime end)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(
                $"https://{_iservUrl}/iserv/calendar/feed/plugin?plugin={plugin}&start={start:O}&end={end:O}");

            Logger.Info($"Got {plugin} events from {start:O} to {end:O}");
            return JArray.Parse(response);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting plugin events");
            throw new IServException("Error getting plugin events", ex);
        }
    }

    /// <summary>
    /// Deletes a calendar event.
    /// </summary>
    /// <param name="uid">Event UID.</param>
    /// <param name="hash">Event hash.</param>
    /// <param name="calendar">Calendar ID.</param>
    /// <param name="start">Event start date.</param>
    /// <param name="series">Whether to delete entire series (default: false).</param>
    /// <returns>JSON response.</returns>
    public async Task<JObject> DeleteEventAsync(
        string uid,
        string hash,
        string calendar,
        DateTime start,
        bool series = false)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"https://{_iservUrl}/iserv/calendar/delete?" +
                $"uid={uid}&hash={hash}&cal={calendar}&start={start:yyyy-MM-ddTHH:mm:sszzz}" +
                $"&edit_series={(series ? "series" : "single")}",
                null);

            var content = await response.Content.ReadAsStringAsync();

            Logger.Info($"Deleted event {uid}");
            return JObject.Parse(content);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error deleting event");
            throw new IServException("Error deleting event", ex);
        }
    }

    /// <summary>
    /// Creates a new calendar event.
    /// </summary>
    /// <param name="subject">Event title.</param>
    /// <param name="calendar">Calendar ID.</param>
    /// <param name="start">Start date and time.</param>
    /// <param name="end">End date and time.</param>
    /// <param name="category">Event category (optional).</param>
    /// <param name="location">Event location (optional).</param>
    /// <param name="alarms">List of alarms (optional).</param>
    /// <param name="isAllDayLong">Whether event is all day (default: false).</param>
    /// <param name="description">Event description (optional).</param>
    /// <param name="participants">List of participants (optional).</param>
    /// <param name="showMeAs">Availability status (default: OPAQUE).</param>
    /// <param name="privacy">Privacy level (default: PUBLIC).</param>
    /// <param name="recurring">Recurring event options (optional).</param>
    public async Task CreateEventAsync(
        string subject,
        string calendar,
        DateTime start,
        DateTime end,
        string category = "",
        string location = "",
        List<string>? alarms = null,
        bool isAllDayLong = false,
        string description = "",
        List<string>? participants = null,
        ShowMeAs showMeAs = ShowMeAs.OPAQUE,
        Privacy privacy = Privacy.PUBLIC,
        RecurringEventOptions? recurring = null)
    {
        try
        {
            // Get CSRF token
            var tokenResponse = await _httpClient.GetStringAsync(
                $"https://{_iservUrl}/iserv/calendar/create_simple");

            var tokenDoc = new HtmlDocument();
            tokenDoc.LoadHtml(tokenResponse);
            var tokenNode = tokenDoc.DocumentNode.SelectSingleNode("//input[@id='eventForm__token']");
            var token = tokenNode?.GetAttributeValue("value", "") ?? "";

            // Build form data
            var formData = new Dictionary<string, string>
            {
                ["eventForm[uid]"] = "",
                ["eventForm[etag]"] = "",
                ["eventForm[hash]"] = "",
                ["eventForm[calendarOrg]"] = "",
                ["eventForm[startOrg]"] = "",
                ["eventForm[action]"] = "create",
                ["eventForm[seriesAction]"] = "",
                ["eventForm[invited]"] = "",
                ["eventForm[subscription]"] = "",
                ["eventForm[subject]"] = subject,
                ["eventForm[calendar]"] = calendar,
                ["eventForm[category]"] = category,
                ["eventForm[location]"] = location,
                ["eventForm[startDate]"] = start.ToString("dd.MM.yyyy"),
                ["eventForm[startTime]"] = start.ToString("HH:mm"),
                ["eventForm[endDate]"] = end.ToString("dd.MM.yyyy"),
                ["eventForm[endTime]"] = end.ToString("HH:mm"),
                ["eventForm[description]"] = description,
                ["eventForm[showMeAs]"] = showMeAs.ToString(),
                ["eventForm[privacy]"] = privacy.ToString(),
                ["eventForm[recurring][intervalType]"] = "NO",
                ["eventForm[recurring][interval]"] = "1",
                ["eventForm[recurring][recurrenceDays][]"] = "FR",
                ["eventForm[recurring][monthlyIntervalType]"] = "BYMONTHDAY",
                ["eventForm[recurring][monthDayInMonth]"] = "26",
                ["eventForm[recurring][endType]"] = "NEVER",
                ["eventForm[submit]"] = "",
                ["eventForm[_token]"] = token
            };

            // Process recurring options
            if (recurring != null && recurring.IntervalType != IntervalType.NO)
            {
                ProcessRecurringOptions(formData, recurring);
            }

            // Process alarms
            if (alarms != null && alarms.Count > 0)
            {
                ProcessAlarms(formData, alarms, start);
            }

            // Process participants
            if (participants != null && participants.Count > 0)
            {
                var participantValues = new List<string>();
                foreach (var participant in participants)
                {
                    var searchResult = await SearchUsersAutocompleteAsync(participant, 1);
                    if (searchResult.Count > 0)
                    {
                        var value = searchResult[0]["value"]?.ToString();
                        if (value != null)
                        {
                            participantValues.Add(value);
                        }
                    }
                    else
                    {
                        throw new IServException($"User '{participant}' not found!");
                    }
                }

                for (int i = 0; i < participantValues.Count; i++)
                {
                    formData[$"eventForm[participants][{i}]"] = participantValues[i];
                }
            }

            var content = new FormUrlEncodedContent(formData);
            var response = await _httpClient.PostAsync(
                $"https://{_iservUrl}/iserv/calendar/create?" +
                $"subject={HttpUtility.UrlEncode(subject)}&calendar={calendar}" +
                $"&start={start:dd.MM.yyyy}&end={end:dd.MM.yyyy}" +
                $"&startTime={start:HH:mm}&endTime={end:HH:mm}&allDay={isAllDayLong}",
                content);

            var responseText = await response.Content.ReadAsStringAsync();
            var responseDoc = new HtmlDocument();
            responseDoc.LoadHtml(responseText);
            var errorNode = responseDoc.DocumentNode.SelectSingleNode("//div[@data-type='error']");
            
            if (errorNode != null)
            {
                Logger.Warn($"Event creation warning: {errorNode.InnerText}");
            }

            Logger.Info("Event created");
        }
        catch (Exception ex) when (ex is not IServException)
        {
            Logger.Error(ex, "Error creating event");
            throw new IServException("Error creating event", ex);
        }
    }

    /// <summary>
    /// Processes recurring event options into form data.
    /// </summary>
    private void ProcessRecurringOptions(Dictionary<string, string> formData, RecurringEventOptions recurring)
    {
        // Validation
        if (recurring.IntervalType != IntervalType.WEEKDAYS && 
            recurring.IntervalType != IntervalType.NO && 
            recurring.Interval == null)
        {
            throw new IServException("Interval must be present for this interval type!");
        }

        if (recurring.Interval.HasValue && (recurring.Interval.Value < 1 || recurring.Interval.Value > 30))
        {
            throw new IServException("Interval can only be between 1 and 30");
        }

        if (recurring.IntervalType == IntervalType.MONTHLY)
        {
            if (recurring.MonthlyIntervalType == null)
                throw new IServException("MonthlyIntervalType must be present!");

            if (recurring.MonthlyIntervalType == Models.MonthlyIntervalType.BYDAY)
            {
                if (recurring.MonthInterval == null)
                    throw new IServException("MonthInterval must be present!");
                if (recurring.MonthDay == null)
                    throw new IServException("MonthDay must be present!");
            }
            else if (recurring.MonthlyIntervalType == Models.MonthlyIntervalType.BYMONTHDAY)
            {
                if (recurring.MonthDayInMonth == null)
                    throw new IServException("MonthDayInMonth must be present!");
            }
        }

        if (recurring.IntervalType == IntervalType.WEEKLY && recurring.RecurrenceDays == null)
        {
            throw new IServException("RecurrenceDays must be present!");
        }

        if (recurring.EndType == EndType.COUNT && recurring.EndInterval == null)
        {
            throw new IServException("EndInterval must be present!");
        }

        if (recurring.EndType == EndType.UNTIL && string.IsNullOrEmpty(recurring.UntilDate))
        {
            throw new IServException("UntilDate must be present!");
        }

        // Set values
        formData["eventForm[recurring][intervalType]"] = recurring.IntervalType.ToString();
        
        if (recurring.Interval.HasValue)
            formData["eventForm[recurring][interval]"] = recurring.Interval.Value.ToString();

        if (recurring.MonthlyIntervalType.HasValue)
            formData["eventForm[recurring][monthlyIntervalType]"] = recurring.MonthlyIntervalType.Value.ToString();

        if (recurring.MonthDayInMonth.HasValue)
            formData["eventForm[recurring][monthDayInMonth]"] = recurring.MonthDayInMonth.Value.ToString();

        if (recurring.MonthInterval.HasValue)
            formData["eventForm[recurring][monthInterval]"] = recurring.MonthInterval.Value.ToString();

        if (recurring.MonthDay.HasValue)
            formData["eventForm[recurring][monthDay]"] = recurring.MonthDay.Value.ToString();

        if (recurring.RecurrenceDays != null && recurring.RecurrenceDays.Count > 0)
        {
            formData["eventForm[recurring][recurrenceDays][]"] = string.Join(",", recurring.RecurrenceDays);
        }

        formData["eventForm[recurring][endType]"] = recurring.EndType.ToString();

        if (recurring.EndInterval.HasValue)
            formData["eventForm[recurring][endInterval]"] = recurring.EndInterval.Value.ToString();

        if (!string.IsNullOrEmpty(recurring.UntilDate))
            formData["eventForm[recurring][untilDate]"] = recurring.UntilDate;
    }

    /// <summary>
    /// Processes alarm options into form data.
    /// </summary>
    private void ProcessAlarms(Dictionary<string, string> formData, List<string> alarms, DateTime start)
    {
        var validAlarms = new[] { "0M", "5M", "15M", "30M", "1H", "2H", "12H", "1D", "2D", "7D" };

        for (int i = 0; i < alarms.Count; i++)
        {
            var alarm = alarms[i];
            
            if (!validAlarms.Contains(alarm))
            {
                throw new IServException($"Invalid alarm value: {alarm}");
            }

            formData[$"eventForm[alarms][{i}][trigger][type]"] = $"PT{alarm}";
            formData[$"eventForm[alarms][{i}][trigger][interval][days]"] = "0";
            formData[$"eventForm[alarms][{i}][trigger][interval][hours]"] = "0";
            formData[$"eventForm[alarms][{i}][trigger][interval][minutes]"] = "15";
            formData[$"eventForm[alarms][{i}][trigger][before]"] = "1";
            formData[$"eventForm[alarms][{i}][trigger][dateTime]"] = start.ToString("dd.MM.yyyy+00:00");
        }
    }

    #endregion

    #region Miscellaneous

    /// <summary>
    /// Gets video conference health status.
    /// </summary>
    /// <returns>JSON object with health status.</returns>
    public async Task<JObject> GetConferenceHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(
                $"https://{_iservUrl}/iserv/videoconference/api/health");

            Logger.Info("Got conference health");
            return JObject.Parse(response);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting conference health");
            throw new IServException("Error getting conference health", ex);
        }
    }

    /// <summary>
    /// Gets disk space usage information.
    /// </summary>
    /// <returns>Dictionary containing disk usage data.</returns>
    public async Task<Dictionary<string, object>> GetDiskSpaceAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(
                $"https://{_iservUrl}/iserv/du/account");

            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            var scriptNode = doc.DocumentNode.SelectSingleNode("//script[@id='user-diskusage-data']");
            if (scriptNode == null)
            {
                throw new IServException("Could not find disk usage data");
            }

            var jsonText = scriptNode.InnerText.Trim().Trim('(', ')');
            var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonText);

            Logger.Info("Got disk space");
            return result ?? new Dictionary<string, object>();
        }
        catch (Exception ex) when (ex is not IServException)
        {
            Logger.Error(ex, "Error getting disk space");
            throw new IServException("Error getting disk space", ex);
        }
    }

    /// <summary>
    /// Calculates the size of a folder.
    /// </summary>
    /// <param name="path">The folder path.</param>
    /// <returns>JSON object with folder size information.</returns>
    public async Task<JObject> GetFolderSizeAsync(string path)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(
                $"https://{_iservUrl}/iserv/file/calc?path={HttpUtility.UrlEncode(path)}");

            Logger.Info($"Got folder size for {path}");
            return JObject.Parse(response);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting folder size");
            throw new IServException("Error getting folder size", ex);
        }
    }

    /// <summary>
    /// Gets list of available groups.
    /// </summary>
    /// <returns>Dictionary of group names to IDs.</returns>
    public async Task<Dictionary<string, string>> GetGroupsAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(
                $"https://{_iservUrl}/iserv/profile/grouprequest/add");

            var doc = new HtmlDocument();
            doc.LoadHtml(response);

            var groups = new Dictionary<string, string>();
            var selectNode = doc.DocumentNode.SelectSingleNode("//select[@class='select2']");

            if (selectNode != null)
            {
                var options = selectNode.SelectNodes(".//option");
                if (options != null)
                {
                    foreach (var option in options)
                    {
                        var text = option.InnerText.Trim();
                        var value = option.GetAttributeValue("value", "");
                        groups[text] = value;
                    }
                }
            }

            Logger.Info("Got groups");
            return groups;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error getting groups");
            throw new IServException("Error getting groups", ex);
        }
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes resources used by the API client.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose method.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }
            _disposed = true;
        }
    }

    #endregion
}
