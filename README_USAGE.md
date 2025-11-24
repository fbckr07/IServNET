# IServ.Net-Api Usage Guide

A C# client library for interacting with IServ school management system.

## Installation

Add the package reference to your project:

```bash
dotnet add reference IServ/IServ.csproj
```

## Quick Start

```csharp
using IServ;
using IServ.Models;

// Initialize the API client (automatically logs in)
using var api = new IServApi("username", "password", "school.iserv.de");

// Get your user information
var userInfo = await api.GetOwnUserInfoAsync();
Console.WriteLine($"User: {userInfo.PublicInfo.Mail}");
```

## Features

### Authentication

Authentication is handled automatically when creating an `IServApi` instance:

```csharp
var api = new IServApi("username", "password", "school.iserv.de");
```

The client maintains session cookies and handles authentication automatically.

### User Management

#### Get Own User Info

```csharp
var userInfo = await api.GetOwnUserInfoAsync();

// Access groups
foreach (var group in userInfo.Groups)
{
    Console.WriteLine($"Group: {group.Key} -> {group.Value}");
}

// Access roles
foreach (var role in userInfo.Roles)
{
    Console.WriteLine($"Role: {role}");
}

// Access public profile
Console.WriteLine($"Email: {userInfo.PublicInfo.Mail}");
Console.WriteLine($"Phone: {userInfo.PublicInfo.Phone}");
```

#### Update User Profile

```csharp
var updates = new Dictionary<string, string>
{
    ["phone"] = "+49 123 456789",
    ["mail"] = "newemail@example.com",
    ["homepage"] = "https://example.com"
};

var statusCode = await api.SetOwnUserInfoAsync(updates);
Console.WriteLine($"Update status: {statusCode}");
```

#### Search Users

```csharp
// Basic search
var results = await api.SearchUsersAsync("John");
foreach (var user in results)
{
    Console.WriteLine($"User: {user.Name} - {user.UserUrl}");
}

// Autocomplete search
var autocompleteResults = await api.SearchUsersAutocompleteAsync("John", limit: 10);
foreach (var result in autocompleteResults)
{
    Console.WriteLine(result);
}
```

#### Get User Information

```csharp
var userDetails = await api.GetUserInfoAsync("john.doe");
foreach (var (key, value) in userDetails)
{
    Console.WriteLine($"{key}: {value}");
}
```

#### Download Profile Picture

```csharp
await api.GetUserProfilePictureAsync("john.doe", "/path/to/output/folder");
```

### Notifications & Badges

```csharp
// Get notifications
var notifications = await api.GetNotificationsAsync();
Console.WriteLine(notifications);

// Get badges
var badges = await api.GetBadgesAsync();
Console.WriteLine(badges);

// Mark all notifications as read
await api.ReadAllNotificationsAsync();

// Mark specific notification as read
await api.ReadNotificationAsync(12345);
```

### Email Management

#### Get Emails

```csharp
// Get emails from INBOX
var emails = await api.GetEmailsAsync();
Console.WriteLine(emails);

// Get emails from specific folder with custom parameters
var sentEmails = await api.GetEmailsAsync(
    path: "Sent",
    length: 20,
    start: 0,
    order: "date",
    dir: "desc"
);
```

#### Get Email Details

```csharp
// Get email metadata
var emailInfo = await api.GetEmailInfoAsync("INBOX");

// Get email source
var emailSource = await api.GetEmailSourceAsync(uid: 12345, path: "INBOX");
Console.WriteLine(emailSource);
```

#### List Mail Folders

```csharp
var folders = await api.GetMailFoldersAsync();
Console.WriteLine(folders);
```

#### Send Email

```csharp
// Simple email
await api.SendEmailAsync(
    receiverEmail: "recipient@example.com",
    subject: "Test Email",
    body: "This is a test email from IServ.Net-Api"
);

// Email with HTML body
await api.SendEmailAsync(
    receiverEmail: "recipient@example.com",
    subject: "HTML Email",
    body: "Plain text version",
    htmlBody: "<h1>HTML Version</h1><p>This is HTML content</p>"
);

// Email with attachments
var attachments = new List<string>
{
    "/path/to/file1.pdf",
    "/path/to/file2.docx"
};

await api.SendEmailAsync(
    receiverEmail: "recipient@example.com",
    subject: "Email with Attachments",
    body: "Please find attached files",
    attachments: attachments
);
```

### Calendar Management

#### Get Calendar Events

```csharp
// Get upcoming events
var upcomingEvents = await api.GetUpcomingEventsAsync();
Console.WriteLine(upcomingEvents);

// Get event sources (calendars)
var eventSources = await api.GetEventSourcesAsync();
Console.WriteLine(eventSources);

// Get events in date range
var start = new DateTime(2024, 1, 1);
var end = new DateTime(2024, 12, 31);
var events = await api.GetEventsAsync(start, end);
foreach (var evt in events)
{
    Console.WriteLine(evt);
}
```

#### Search Events

```csharp
var searchResults = await api.SearchEventAsync(
    query: "Meeting",
    start: DateTime.Now,
    end: DateTime.Now.AddMonths(1)
);
```

#### Create Simple Event

```csharp
await api.CreateEventAsync(
    subject: "Team Meeting",
    calendar: "calendar-id",
    start: new DateTime(2024, 12, 15, 14, 0, 0),
    end: new DateTime(2024, 12, 15, 15, 0, 0),
    location: "Conference Room A",
    description: "Quarterly team meeting"
);
```

#### Create Event with Alarms

```csharp
var alarms = new List<string> { "15M", "1H", "1D" }; // 15 minutes, 1 hour, 1 day before

await api.CreateEventAsync(
    subject: "Important Meeting",
    calendar: "calendar-id",
    start: new DateTime(2024, 12, 20, 10, 0, 0),
    end: new DateTime(2024, 12, 20, 11, 0, 0),
    alarms: alarms
);
```

#### Create Recurring Event

```csharp
// Weekly recurring event
var recurringOptions = new RecurringEventOptions
{
    IntervalType = IntervalType.WEEKLY,
    Interval = 1,
    RecurrenceDays = new List<WeekDay> { WeekDay.MO, WeekDay.WE, WeekDay.FR },
    EndType = EndType.COUNT,
    EndInterval = 10 // 10 occurrences
};

await api.CreateEventAsync(
    subject: "Weekly Standup",
    calendar: "calendar-id",
    start: new DateTime(2024, 12, 1, 9, 0, 0),
    end: new DateTime(2024, 12, 1, 9, 30, 0),
    recurring: recurringOptions
);

// Monthly recurring event (first Monday)
var monthlyRecurring = new RecurringEventOptions
{
    IntervalType = IntervalType.MONTHLY,
    Interval = 1,
    MonthlyIntervalType = MonthlyIntervalType.BYDAY,
    MonthInterval = 1, // First
    MonthDay = WeekDay.MO, // Monday
    EndType = EndType.UNTIL,
    UntilDate = "31.12.2024"
};

await api.CreateEventAsync(
    subject: "Monthly Review",
    calendar: "calendar-id",
    start: new DateTime(2024, 12, 1, 14, 0, 0),
    end: new DateTime(2024, 12, 1, 15, 0, 0),
    recurring: monthlyRecurring
);
```

#### Create Event with Participants

```csharp
var participants = new List<string> { "john.doe", "jane.smith" };

await api.CreateEventAsync(
    subject: "Project Discussion",
    calendar: "calendar-id",
    start: new DateTime(2024, 12, 18, 13, 0, 0),
    end: new DateTime(2024, 12, 18, 14, 0, 0),
    participants: participants,
    privacy: Privacy.CONFIDENTIAL,
    showMeAs: ShowMeAs.OPAQUE
);
```

#### Delete Event

```csharp
// Delete single event
await api.DeleteEventAsync(
    uid: "event-uid",
    hash: "event-hash",
    calendar: "calendar-id",
    start: new DateTime(2024, 12, 15, 14, 0, 0),
    series: false
);

// Delete entire recurring series
await api.DeleteEventAsync(
    uid: "event-uid",
    hash: "event-hash",
    calendar: "calendar-id",
    start: new DateTime(2024, 12, 15, 14, 0, 0),
    series: true
);
```

#### Get Plugin Events

```csharp
var pluginEvents = await api.GetCalendarPluginEventsAsync(
    plugin: "plugin-name",
    start: DateTime.Now,
    end: DateTime.Now.AddMonths(1)
);
```

### File Management

```csharp
// Get disk space usage
var diskSpace = await api.GetDiskSpaceAsync();
foreach (var (key, value) in diskSpace)
{
    Console.WriteLine($"{key}: {value}");
}

// Get folder size
var folderSize = await api.GetFolderSizeAsync("/path/to/folder");
Console.WriteLine(folderSize);
```

### Miscellaneous

```csharp
// Get video conference health
var conferenceHealth = await api.GetConferenceHealthAsync();
Console.WriteLine(conferenceHealth);

// Get available groups
var groups = await api.GetGroupsAsync();
foreach (var (groupName, groupId) in groups)
{
    Console.WriteLine($"{groupName}: {groupId}");
}
```

## Error Handling

All methods throw `IServException` on errors:

```csharp
using IServ.Exceptions;

try
{
    var userInfo = await api.GetOwnUserInfoAsync();
}
catch (IServException ex)
{
    Console.WriteLine($"IServ API Error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"General Error: {ex.Message}");
}
```

## Logging

The library provides a small built-in Console-based logger by default and does not require an external logging package.

If you prefer to integrate a different logging framework (for example `Microsoft.Extensions.Logging`), replace the internal `Logger` implementation in `IServ/IServApi.cs` with an adapter for your logger of choice or add a thin wrapper that forwards messages to your logging framework.

If you want, I can help converting the library to use `Microsoft.Extensions.Logging.Abstractions` to allow dependency-injection-based logging.

## Disposal

The `IServApi` class implements `IDisposable`. Always dispose of instances properly:

```csharp
// Using statement (recommended)
using (var api = new IServApi("username", "password", "school.iserv.de"))
{
    // Use the API
}

// Or explicit disposal
var api = new IServApi("username", "password", "school.iserv.de");
try
{
    // Use the API
}
finally
{
    api.Dispose();
}
```

## Complete Example

```csharp
using IServ;
using IServ.Models;
using IServ.Exceptions;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // Initialize API
            using var api = new IServApi(
                username: "your.username",
                password: "your.password",
                iservUrl: "school.iserv.de"
            );

            // Get user information
            var userInfo = await api.GetOwnUserInfoAsync();
            Console.WriteLine($"Logged in as: {userInfo.PublicInfo.Mail}");

            // Search for a user
            var users = await api.SearchUsersAsync("John");
            Console.WriteLine($"Found {users.Count} users matching 'John'");

            // Get notifications
            var notifications = await api.GetNotificationsAsync();
            Console.WriteLine($"Notifications: {notifications}");

            // Create a calendar event
            await api.CreateEventAsync(
                subject: "Project Kickoff",
                calendar: "my-calendar",
                start: DateTime.Now.AddDays(7),
                end: DateTime.Now.AddDays(7).AddHours(2),
                location: "Office",
                description: "Initial project meeting"
            );
            Console.WriteLine("Event created successfully!");

            // Send an email
            await api.SendEmailAsync(
                receiverEmail: "colleague@school.iserv.de",
                subject: "Hello from C#",
                body: "This email was sent using IServ.Net-Api!"
            );
            Console.WriteLine("Email sent successfully!");

        }
        catch (IServException ex)
        {
            Console.WriteLine($"IServ Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
```

## Requirements

- .NET 10.0 or later
- Valid IServ account credentials
- Network access to IServ instance

## Dependencies

- HtmlAgilityPack (HTML parsing)
- MailKit (SMTP email)
- Newtonsoft.Json (JSON handling)
 - Built-in Console logger (no external logging dependency)
- WebDAVClient (File operations)

## License

See LICENSE file for details.
