# IServNet Code Examples

This document provides practical code examples for common use cases.

## Table of Contents
- [Setup](#setup)
- [Authentication](#authentication)
- [User Operations](#user-operations)
- [Notifications](#notifications)
- [Email Operations](#email-operations)
- [Calendar Operations](#calendar-operations)
- [Complete Application Example](#complete-application-example)

## Setup

First, ensure you have the necessary using directives:

```csharp
using IServ;
using IServ.Models;
using IServ.Exceptions;
using Newtonsoft.Json.Linq;
```

## Authentication

Authentication is automatic when you create an `IServApi` instance:

```csharp
// Basic initialization
using var api = new IServApi(
    username: "your.username",
    password: "your.password",
    iservUrl: "school.iserv.de"
);

// The constructor automatically performs login and stores session cookies
Console.WriteLine("Successfully logged in!");
```

## User Operations

### Get Your Profile Information

```csharp
using var api = new IServApi("username", "password", "school.iserv.de");

var userInfo = await api.GetOwnUserInfoAsync();

// Display basic information
Console.WriteLine("=== User Profile ===");
Console.WriteLine($"Email: {userInfo.PublicInfo.Mail}");
Console.WriteLine($"Phone: {userInfo.PublicInfo.Phone}");
Console.WriteLine($"Mobile: {userInfo.PublicInfo.MobilePhone}");

// Display groups
Console.WriteLine("\n=== Groups ===");
foreach (var (groupName, groupUrl) in userInfo.Groups)
{
    Console.WriteLine($"- {groupName}");
}

// Display roles
Console.WriteLine("\n=== Roles ===");
foreach (var role in userInfo.Roles)
{
    Console.WriteLine($"- {role}");
}
```

### Update Your Profile

```csharp
var updates = new Dictionary<string, string>
{
    ["phone"] = "+49 123 456789",
    ["mobilePhone"] = "+49 987 654321",
    ["homepage"] = "https://mywebsite.com",
    ["note"] = "Available Mon-Fri 9:00-17:00"
};

var statusCode = await api.SetOwnUserInfoAsync(updates);

if (statusCode == 200)
{
    Console.WriteLine("Profile updated successfully!");
}
```

### Search for Users

```csharp
// Search by name
var results = await api.SearchUsersAsync("müller");

Console.WriteLine($"Found {results.Count} users:");
foreach (var user in results)
{
    Console.WriteLine($"- {user.Name}");
    
    // Get detailed info for each user
    var details = await api.GetUserInfoAsync(user.Name);
    foreach (var (key, value) in details)
    {
        Console.WriteLine($"  {key}: {value}");
    }
}
```

### Download Profile Pictures

```csharp
var outputDir = "./profile_pictures";
Directory.CreateDirectory(outputDir);

var usernames = new[] { "john.doe", "jane.smith", "max.mustermann" };

foreach (var username in usernames)
{
    try
    {
        await api.GetUserProfilePictureAsync(username, outputDir);
        Console.WriteLine($"Downloaded picture for {username}");
    }
    catch (IServException ex)
    {
        Console.WriteLine($"Could not download picture for {username}: {ex.Message}");
    }
}
```

## Notifications

### Check and Read Notifications

```csharp
// Get all notifications
var notifications = await api.GetNotificationsAsync();
Console.WriteLine($"Total notifications: {notifications["total"]}");

// Get badge counts
var badges = await api.GetBadgesAsync();
Console.WriteLine($"Unread messages: {badges["mail"]}");

// Mark all as read
await api.ReadAllNotificationsAsync();
Console.WriteLine("All notifications marked as read");

// Mark specific notification as read
await api.ReadNotificationAsync(12345);
```

## Email Operations

### Read Emails

```csharp
// Get recent emails from inbox
var emails = await api.GetEmailsAsync(
    path: "INBOX",
    length: 10,
    start: 0,
    order: "date",
    dir: "desc"
);

Console.WriteLine("=== Recent Emails ===");
var data = emails["data"] as JArray;
if (data != null)
{
    foreach (var email in data)
    {
        Console.WriteLine($"From: {email["from"]}");
        Console.WriteLine($"Subject: {email["subject"]}");
        Console.WriteLine($"Date: {email["date"]}");
        Console.WriteLine("---");
    }
}
```

### Send Simple Email

```csharp
await api.SendEmailAsync(
    receiverEmail: "colleague@school.iserv.de",
    subject: "Weekly Report",
    body: "Please find attached the weekly report."
);

Console.WriteLine("Email sent successfully!");
```

### Send Email with Attachments

```csharp
var attachments = new List<string>
{
    @"C:\Documents\report.pdf",
    @"C:\Documents\data.xlsx"
};

await api.SendEmailAsync(
    receiverEmail: "manager@school.iserv.de",
    subject: "Monthly Report with Attachments",
    body: "Please review the attached documents.",
    htmlBody: "<h2>Monthly Report</h2><p>Please review the attached documents.</p>",
    attachments: attachments
);

Console.WriteLine("Email with attachments sent!");
```

### List Mail Folders

```csharp
var folders = await api.GetMailFoldersAsync();
Console.WriteLine("=== Mail Folders ===");

var folderList = folders["folders"] as JArray;
if (folderList != null)
{
    foreach (var folder in folderList)
    {
        Console.WriteLine($"- {folder["name"]} ({folder["count"]} messages)");
    }
}
```

## Calendar Operations

### View Upcoming Events

```csharp
var upcomingEvents = await api.GetUpcomingEventsAsync();

Console.WriteLine("=== Upcoming Events ===");
var events = upcomingEvents["events"] as JArray;
if (events != null)
{
    foreach (var evt in events)
    {
        Console.WriteLine($"- {evt["subject"]}");
        Console.WriteLine($"  Time: {evt["start"]} - {evt["end"]}");
        Console.WriteLine($"  Location: {evt["location"]}");
    }
}
```

### Create a Simple Event

```csharp
await api.CreateEventAsync(
    subject: "Team Meeting",
    calendar: "my-calendar",
    start: new DateTime(2024, 12, 20, 14, 0, 0),
    end: new DateTime(2024, 12, 20, 15, 0, 0),
    location: "Conference Room A",
    description: "Weekly team sync meeting"
);

Console.WriteLine("Event created!");
```

### Create Event with Reminders

```csharp
// Add multiple reminders
var alarms = new List<string>
{
    "15M",  // 15 minutes before
    "1H",   // 1 hour before
    "1D"    // 1 day before
};

await api.CreateEventAsync(
    subject: "Important Presentation",
    calendar: "my-calendar",
    start: DateTime.Now.AddDays(7).AddHours(10),
    end: DateTime.Now.AddDays(7).AddHours(11),
    location: "Main Hall",
    alarms: alarms,
    description: "Q4 Results Presentation"
);
```

### Create Weekly Recurring Event

```csharp
// Create event that repeats every Monday, Wednesday, Friday
var recurringOptions = new RecurringEventOptions
{
    IntervalType = IntervalType.WEEKLY,
    Interval = 1,
    RecurrenceDays = new List<WeekDay> 
    { 
        WeekDay.MO, 
        WeekDay.WE, 
        WeekDay.FR 
    },
    EndType = EndType.COUNT,
    EndInterval = 20  // 20 occurrences
};

await api.CreateEventAsync(
    subject: "Daily Standup",
    calendar: "my-calendar",
    start: new DateTime(2024, 12, 1, 9, 0, 0),
    end: new DateTime(2024, 12, 1, 9, 15, 0),
    recurring: recurringOptions
);
```

### Create Monthly Recurring Event

```csharp
// Repeat on the 15th of each month
var monthlyRecurring = new RecurringEventOptions
{
    IntervalType = IntervalType.MONTHLY,
    Interval = 1,
    MonthlyIntervalType = MonthlyIntervalType.BYMONTHDAY,
    MonthDayInMonth = 15,
    EndType = EndType.UNTIL,
    UntilDate = "31.12.2025"
};

await api.CreateEventAsync(
    subject: "Monthly Report Due",
    calendar: "my-calendar",
    start: new DateTime(2024, 12, 15, 17, 0, 0),
    end: new DateTime(2024, 12, 15, 17, 30, 0),
    recurring: monthlyRecurring,
    description: "Submit monthly report by end of day"
);
```

### Create Event with Participants

```csharp
var participants = new List<string>
{
    "john.doe",
    "jane.smith",
    "team.leader"
};

await api.CreateEventAsync(
    subject: "Project Kickoff Meeting",
    calendar: "my-calendar",
    start: DateTime.Now.AddDays(3).AddHours(10),
    end: DateTime.Now.AddDays(3).AddHours(12),
    location: "Board Room",
    participants: participants,
    description: "Initial project planning and role assignments",
    privacy: Privacy.CONFIDENTIAL,
    showMeAs: ShowMeAs.OPAQUE
);
```

### Search for Events

```csharp
var searchResults = await api.SearchEventAsync(
    query: "meeting",
    start: DateTime.Now,
    end: DateTime.Now.AddMonths(1)
);

Console.WriteLine($"Found {searchResults.Count} events");
foreach (var evt in searchResults)
{
    Console.WriteLine($"- {evt["subject"]} on {evt["start"]}");
}
```

### Delete an Event

```csharp
// To delete an event, you need its UID, hash, and calendar ID
// These can be obtained from GetEventsAsync() or SearchEventAsync()

await api.DeleteEventAsync(
    uid: "event-unique-id",
    hash: "event-hash",
    calendar: "calendar-id",
    start: new DateTime(2024, 12, 20, 14, 0, 0),
    series: false  // false for single event, true for entire series
);

Console.WriteLine("Event deleted successfully!");
```

## Complete Application Example

Here's a complete console application that demonstrates multiple features:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IServ;
using IServ.Models;
using IServ.Exceptions;

namespace IServExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Configuration
            const string username = "your.username";
            const string password = "your.password";
            const string iservUrl = "school.iserv.de";

            try
            {
                Console.WriteLine("=== IServ.Net-Api Demo ===\n");

                // Initialize API
                Console.WriteLine("Logging in...");
                using var api = new IServApi(username, password, iservUrl);
                Console.WriteLine("✓ Login successful\n");

                // Get user information
                Console.WriteLine("Fetching user information...");
                var userInfo = await api.GetOwnUserInfoAsync();
                Console.WriteLine($"✓ Logged in as: {userInfo.PublicInfo.Mail}");
                Console.WriteLine($"  Groups: {userInfo.Groups.Count}");
                Console.WriteLine($"  Roles: {string.Join(", ", userInfo.Roles)}\n");

                // Get notifications
                Console.WriteLine("Checking notifications...");
                var notifications = await api.GetNotificationsAsync();
                Console.WriteLine($"✓ Total notifications: {notifications["total"]}\n");

                // Get upcoming events
                Console.WriteLine("Fetching upcoming events...");
                var upcomingEvents = await api.GetUpcomingEventsAsync();
                Console.WriteLine($"✓ Upcoming events retrieved\n");

                // Create a test event
                Console.WriteLine("Creating test calendar event...");
                var eventStart = DateTime.Now.AddDays(1).Date.AddHours(14);
                var eventEnd = eventStart.AddHours(1);

                await api.CreateEventAsync(
                    subject: "Test Event from C# API",
                    calendar: "default",
                    start: eventStart,
                    end: eventEnd,
                    location: "Virtual",
                    description: "This event was created using IServ.Net-Api",
                    alarms: new List<string> { "15M" }
                );
                Console.WriteLine($"✓ Event created for {eventStart:yyyy-MM-dd HH:mm}\n");

                // Send a test email (commented out by default)
                /*
                Console.WriteLine("Sending test email...");
                await api.SendEmailAsync(
                    receiverEmail: "yourself@school.iserv.de",
                    subject: "Test from IServ.Net-Api",
                    body: "This is a test email sent from the C# API client."
                );
                Console.WriteLine("✓ Email sent\n");
                */

                // Get disk space
                Console.WriteLine("Checking disk space...");
                var diskSpace = await api.GetDiskSpaceAsync();
                Console.WriteLine($"✓ Disk space information retrieved\n");

                // Get groups
                Console.WriteLine("Fetching available groups...");
                var groups = await api.GetGroupsAsync();
                Console.WriteLine($"✓ Found {groups.Count} groups\n");

                Console.WriteLine("=== Demo completed successfully! ===");
            }
            catch (IServException ex)
            {
                Console.WriteLine($"\n❌ IServ Error: {ex.Message}");
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }
    }
}
```

## Error Handling Best Practices

Always wrap API calls in try-catch blocks:

```csharp
try
{
    var userInfo = await api.GetOwnUserInfoAsync();
    // Process user info
}
catch (IServException ex)
{
    // Handle IServ-specific errors
    Console.WriteLine($"IServ API Error: {ex.Message}");
    
    // You might want to:
    // - Log the error
    // - Notify the user
    // - Retry the operation
    // - Fall back to cached data
}
catch (HttpRequestException ex)
{
    // Handle network errors
    Console.WriteLine($"Network Error: {ex.Message}");
}
catch (Exception ex)
{
    // Handle unexpected errors
    Console.WriteLine($"Unexpected Error: {ex.Message}");
}
```

## Performance Tips

1. **Reuse the IServApi instance**: Don't create a new instance for each operation
2. **Use async/await properly**: Always await async operations
3. **Dispose properly**: Use `using` statements or explicit disposal
4. **Batch operations**: When possible, batch multiple related operations

```csharp
// Good: Reuse instance
using var api = new IServApi(username, password, iservUrl);

var userInfo = await api.GetOwnUserInfoAsync();
var notifications = await api.GetNotificationsAsync();
var events = await api.GetUpcomingEventsAsync();

// Bad: Creating multiple instances
using (var api1 = new IServApi(username, password, iservUrl))
{
    var userInfo = await api1.GetOwnUserInfoAsync();
}
using (var api2 = new IServApi(username, password, iservUrl))
{
    var notifications = await api2.GetNotificationsAsync();
}
```

## Logging Configuration

The library uses a simple built-in Console-based logger by default. Log messages are written to the console without requiring an external logging package.

To use a different logging backend (for example `Microsoft.Extensions.Logging`), replace the `Logger` implementation in `IServ/IServApi.cs` with an adapter that forwards messages to your chosen framework.

## Troubleshooting

### Authentication Issues

If you get authentication errors:
1. Verify your credentials are correct
2. Check if your IServ URL is accessible
3. Ensure your account is not locked
4. Check if two-factor authentication is enabled (not currently supported)

### Network Issues

If you experience network timeouts:
1. Check your internet connection
2. Verify the IServ server is accessible
3. Consider increasing timeout values if needed

### Calendar Issues

If event creation fails:
1. Verify the calendar ID exists
2. Check date/time formats
3. Ensure recurring options are valid
4. Verify you have permission to create events

For more help, check the main documentation in README_USAGE.md.
