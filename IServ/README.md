# IServ.Net-Api

A comprehensive C# client library for interacting with the IServ school management system API.

## Overview

This library provides a complete C# implementation of the IServ API, translated from the Python [IServAPI](https://github.com/Leo-Aqua/IServAPI) project. It supports all major features including user management, notifications, email, calendar events, and file operations.

## Features

- **Authentication & Session Management** - Automatic login and cookie handling
- **User Management** - Get/set user info, search users, profile pictures
- **Notifications & Badges** - Retrieve and manage notifications
- **Email** - Send/receive emails with attachment support
- **Calendar** - Create/delete events with recurring patterns and alarms
- **File Management** - WebDAV integration and disk space monitoring
- **Conference** - Video conference health monitoring
- **Groups** - Retrieve available groups

## Quick Start

```csharp
using IServ;

// Initialize and login
using var api = new IServApi("username", "password", "school.iserv.de");

// Get user information
var userInfo = await api.GetOwnUserInfoAsync();
Console.WriteLine($"User: {userInfo.PublicInfo.Mail}");

// Create a calendar event
await api.CreateEventAsync(
    subject: "Meeting",
    calendar: "calendar-id",
    start: DateTime.Now.AddDays(1),
    end: DateTime.Now.AddDays(1).AddHours(1)
);

// Send an email
await api.SendEmailAsync(
    receiverEmail: "recipient@school.iserv.de",
    subject: "Hello",
    body: "Test email from IServ.Net-Api"
);
```

## Installation

Add the project reference to your application:

```bash
dotnet add reference IServ/IServ.csproj
```

## Documentation

See [README_USAGE.md](README_USAGE.md) for comprehensive documentation with examples.

## Requirements

- .NET 10.0 or later
- Valid IServ account credentials
- Network access to IServ instance

## Dependencies

- HtmlAgilityPack - HTML parsing
- MailKit - SMTP email functionality
- Newtonsoft.Json - JSON serialization
 - Built-in Console logger (no external logging dependency)
- WebDAVClient - WebDAV file operations

## Project Structure

```
IServ/
├── IServApi.cs              # Main API class
├── Models/                  # Data models
│   ├── UserInfo.cs
│   ├── RecurringEventOptions.cs
│   ├── IntervalType.cs
│   ├── Privacy.cs
│   └── ...
└── Exceptions/
    └── IServException.cs    # Custom exception type
```

## Credits

This C# implementation is based on the Python [IServAPI](https://github.com/Leo-Aqua/IServAPI) by Leo-Aqua.

## License

See LICENSE file for details.
