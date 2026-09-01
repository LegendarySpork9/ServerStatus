# Server Status - Infrastructure Document

## Overview

Server Status is a self-hosted monitoring suite for tracking the status of locally hosted game servers. It consists of three companion applications — a Blazor Server website for viewing status and managing alerts, a reporter console application that collects machine and server data, and an automation console application that detects missed events and raises alerts. All three communicate with a shared API.

- **Author:** Hunter Industries / Toby Hunter
- **Version:** 1.0.0
- **Repository:** https://github.com/LegendarySpork9/ServerStatus

## Technology Stack

| Component | Technology | Version |
|---|---|---|
| Framework | ASP.NET Core (Blazor Server) / .NET Console | 8.0 |
| Language | C# | Latest |
| UI | Razor Components (Interactive Server) | - |
| CSS Framework | Bootstrap | 5.x |
| Logging | log4net | 3.3.1 |
| HTTP Client | RestSharp | 112.1.0 |
| JSON Serialisation | Newtonsoft.Json | 13.0.3 |
| System Management | System.Management | 9.0.5 |
| Testing | MSTest | 3.6.4 |
| Test SDK | Microsoft.NET.Test.Sdk | 17.12.0 |
| Mocking | Moq | 4.20.72 |
| Assertions | FluentAssertions | 8.3.0 |
| Code Coverage | coverlet.collector | 6.0.2 |

## Solution Structure

```
ServerStatus/
+-- ServerStatusCommon/                 # Shared class library
|   +-- Abstractions/                   # Interface definitions
|   +-- Converters/                     # API and standard value converters
|   +-- Functions/                      # Shared settings loader, timer, and URL builder functions
|   +-- Implementations/               # Interface implementations (wrappers)
|   +-- Models/                         # Data models
|   |   +-- Requests/                   # API request models
|   |   |   +-- Create/                 # Create request models
|   |   |   +-- Update/                 # Update request models
|   |   +-- Responses/                  # API response models
|   |       +-- Related/                # Nested response models
|   +-- Payload/                        # Authentication payload templates
|   +-- Services/                       # Shared services
+-- ServerStatusSite/                   # Blazor Server web application
|   +-- Components/                     # Razor components
|   |   +-- Layout/                     # Main layout and navigation
|   |   +-- Pages/                      # Application pages
|   |       +-- Alerts/                 # Alert management pages
|   +-- Abstractions/                   # Site-specific interface definitions
|   +-- Converters/                     # Style and theme converters
|   +-- Functions/                      # Hashing, IP address, and webhook auth functions
|   +-- Implementations/               # Site-specific interface implementations (wrappers)
|   +-- Models/                         # Site-specific data models
|   |   +-- Requests/                   # Backup Tool API request models
|   |   +-- Responses/                  # Backup Tool API response models
|   |       +-- Related/                # Nested response models
|   +-- Properties/                     # Launch settings and publish profiles
|   +-- Services/                       # Site-specific services
|   +-- Webhooks/                       # Webhook receiver endpoints
|   +-- Content/                        # Static assets (favicon)
|   +-- wwwroot/                        # Static web assets (CSS, images, JS)
+-- ServerStatusReporter/               # Console application (data collector)
|   +-- Abstractions/                   # Reporter-specific interfaces
|   +-- Implementations/               # Reporter-specific wrappers
|   +-- Models/                         # Reporter-specific configuration model
|   +-- Services/                       # Reporter application and PID services
+-- ServerStatusAutomation/             # Console application (alert automation)
|   +-- Services/                       # Automation service
+-- Tests/
|   +-- ServerStatus.UnitTests/         # Unit tests — converters, functions, helpers only
|   |   +-- Common/Converters/          # API converter tests
|   |   +-- Common/Functions/           # Timer and URL builder function tests
|   |   +-- Site/Converters/            # Style converter tests
|   |   +-- Site/Functions/             # Hash, IP address, and webhook auth function tests
|   +-- ServerStatus.PersistenceTests/  # Persistence tests — file I/O, implementations
|   |   +-- Common/Functions/           # Shared settings loader tests (real config files)
|   |   +-- Common/Implementations/    # File system wrapper and clock tests
|   |   +-- Common/Services/            # PID file service tests
|   +-- ServerStatus.IntegrationTests/  # Integration tests — service orchestration
|   |   +-- Common/Services/            # API and Discord service tests
|   |   +-- Site/Services/              # Backup Tool API and log stream service tests
+-- .github/workflows/                  # CI/CD pipeline definitions
```

## Application Architecture

### Project Dependencies

```
ServerStatusSite ──────────► ServerStatusCommon
ServerStatusReporter ──────► ServerStatusCommon
ServerStatusAutomation ─────► ServerStatusCommon
ServerStatus.Tests ─────────► ServerStatusSite
                            ► ServerStatusReporter
                            ► ServerStatusAutomation
```

### ServerStatusCommon (Shared Library)

The common library provides shared abstractions, services, models, and utilities used by all three applications.

#### Dependency Injection

| Abstraction | Implementation | Purpose |
|---|---|---|
| `ILoggerService` | `LoggerServiceWrapper` | Application logging via log4net with contextual identifiers |
| `IFileSystem` | `FileSystemWrapper` | File read and existence check operations |
| `IAPIClient` | `APIClientWrapper` | REST API communication via RestSharp |
| `IHTTPClient` | `HTTPClientWrapper` | Raw HTTP request execution |
| `IClock` | `SystemClockProvider` | UTC time and default date operations |

#### Shared Services

| Service | Responsibility |
|---|---|
| `APIService` | Token-authenticated API operations with automatic reauthorisation and retry logic |
| `DiscordService` | Discord webhook notifications for alert escalation with per-server channel targeting |
| `LoggerService` | Internal log4net adapter with contextual identifier prefixing |
| `RetryService` | Generic async retry mechanism with configurable attempts and delays |

#### Shared Functions

| Function | Responsibility |
|---|---|
| `SharedSettingsLoader` | Loads App.config files and maps appSettings to `SharedSettingsModel` via reflection |
| `TimerFunction` | Calculates timer intervals from current time to a target elapse time |
| `URLBuilderFunction` | Builds API URLs from a base URL, endpoint, entity ID, and query parameters. Used by both `APIClientWrapper` and `BackupToolAPIClientWrapper` |

#### Shared Converters

| Converter | Responsibility |
|---|---|
| `APIConverter` | Maps API endpoints to query parameters and status values to CSS classes |
| `StandardValues` | Constants for log levels, default settings, alert defaults, and missing value placeholders |

### ServerStatusSite (Web Application)

#### Rendering Model

The application uses **Blazor Server** with Interactive Server Render Mode. The UI runs on the server and communicates with the browser over a SignalR (WebSocket) connection.

#### Dependency Injection

Services are registered in `Program.cs`:

| Registration | Lifetime | Purpose |
|---|---|---|
| `SharedSettingsModel` | Singleton | Application configuration |
| `BackupToolSettingsModel` | Singleton | Backup Tool API configuration (per-server credentials) |
| `ILoggerService` | Singleton | Logging |
| `IClock` | Singleton | Time operations |
| `IFileSystem` | Singleton | File system access |
| `IAPIClient` | Singleton | API communication (Hunter Industries API) |
| `IHTTPClient` | Singleton | HTTP requests |
| `RetryService` | Singleton | Retry logic |
| `APIService` | Singleton | Hunter Industries API operations |
| `IBackupToolAPIClient` | Singleton | API communication (Backup Tool API) |
| `BackupToolAPIService` | Singleton | Backup Tool API operations with retry logic |
| `LogStreamService` | Singleton | Webhook-to-component event bus for real-time log streaming |
| `UserModel` | Scoped | Current user session |
| `IHttpContextAccessor` | Scoped | HTTP context access |

#### Site-Specific Functions

| Function | Responsibility |
|---|---|
| `HashFunction` | SHA512 password hashing |
| `IPAddressFunction` | Client IP extraction from CF-Connecting-IP, X-Forwarded-For, or connection |
| `WebhookAuthValidationFunction` | HMAC-SHA256 signature validation for incoming webhook requests |

#### Site-Specific Converters

| Converter | Responsibility |
|---|---|
| `StyleConverter` | Generates CSS styles for dark mode theming across all UI components |

#### Site-Specific Services

| Service | Responsibility |
|---|---|
| `LogStreamService` | Singleton event bus that routes incoming webhook log payloads to subscribed Blazor components by server name |
| `BackupToolApiService` | HTTP client for the Server Backup Tool API — fetches live/archived logs, registers/unregisters webhooks. Uses per-server Basic Auth credentials from configuration |

#### Webhook Endpoints

| Endpoint | Route | Purpose |
|---|---|---|
| `LogWebhookController` | `POST /webhooks/webhook` | Receives log payloads from the Backup Tool API. Validates HMAC-SHA256 signature, then publishes logs to subscribers via `LogStreamService` |

#### Pages

| Page | Route | Layout | Purpose |
|---|---|---|---|
| Home | `/` | MainLayout | Server status dashboard with auto-refresh |
| Login | `/login` | BlankLayout | Username and password authentication |
| Account | `/account` | MainLayout | User preferences (dark mode, Discord name, credentials) |
| Alerts | `/alerts` | MainLayout | Paginated alert list with server name filtering and admin editing |
| Register Alert | `/registeralert` | MainLayout | Report a new server alert |
| Edit Alert | `/editalert` | MainLayout | Update alert status (admin only) |
| Server Logs | `/serverlogs` | MainLayout | Live and archived log viewer with real-time webhook updates |
| Error | `/Error` | - | Error display page |

### ServerStatusReporter (Data Collector)

A console application that runs on each monitored machine. It periodically checks three components per configured game server and reports status to the API.

#### Reporter-Specific Abstractions

| Abstraction | Implementation | Purpose |
|---|---|---|
| `IProcessService` | `ProcessServiceWrapper` | Windows process existence and start time verification |
| `ITCPClient` | `TCPClientWrapper` | TCP socket connectivity testing (5-second timeout) |

#### Reporter Services

| Service | Responsibility |
|---|---|
| `ApplicationService` | Periodic monitoring orchestrator with configurable timer |
| `PidFileService` | Reads PID files to identify tracked server processes |

#### Monitoring Components

| Component | Check Method | Online Condition |
|---|---|---|
| PC | Always reports | Machine is running (implicit) |
| Server | PID file + process verification | Server process is running with expected start time |
| Connection | TCP socket connection | Successful TCP connection to server IP and port |

### ServerStatusAutomation (Alert Automation)

A console application that detects missed or outdated status events and raises alerts automatically.

#### Automation Service

| Service | Responsibility |
|---|---|
| `AutomationService` | Periodic check for stale/offline statuses, alert creation, and Discord notification |

#### Alert Logic

- Checks each server's event timestamps against the refresh period
- Registers "Unknown" status if events are outdated or missing
- Creates alerts for offline or unknown components
- Skips duplicate alerts if an unresolved alert already exists
- Respects scheduled downtime windows to avoid false positives
- Sends Discord notifications to the server's configured webhook channel when new alerts are created

## Monitoring Pipeline

### Data Flow

1. **ServerStatusReporter** runs on each monitored machine, checking PC, Server, and Connection status
2. Reporter sends status events to the **API** at each refresh interval
3. **ServerStatusAutomation** periodically queries the API for all server events
4. Automation detects missing or outdated events and raises alerts
5. Automation sends **Discord notifications** to each server's configured webhook channel
6. **ServerStatusSite** displays server status and alerts to users via the API
7. When alerts are created or updated via the site, **Discord notifications** are sent to the affected server's webhook channel

### Status Values

| Status | Meaning |
|---|---|
| Online | Component is reachable and operational |
| Offline | Component is unreachable or not running |
| Unknown | Status could not be determined or event data is stale |

### Alert Statuses

| Status | Meaning |
|---|---|
| Reported | Alert has been created |
| Investigating | Alert is being looked into |
| Resolved | Alert has been resolved |

## Authentication and Security

### Site Authentication

- Username and password login with server-side username filtering
- Passwords hashed with **SHA512** before comparison
- Session managed via Blazor's `ProtectedSessionStorage` (encrypted browser session)
- IP address logged for all requests (supports Cloudflare CF-Connecting-IP and X-Forwarded-For headers)

### API Authentication

- Bearer token authentication with automatic expiry tracking and reauthorisation
- Credentials stored as **Base64-encoded Basic auth** in configuration
- Authentication payload loaded from a JSON file (`Authorise.json`)
- Retry logic (4 retries, 30-second delay) for authentication failures

### User Roles

- Standard users can view status and report alerts
- Admin users (`IsAdmin` setting) can edit alert statuses

### Web Security

- HTTPS enforced with HSTS in production
- Antiforgery token validation on all forms

## Configuration

### ServerStatusSite (appsettings.json)

```json
{
  "AppSettings": {
    "Domain": "<application domain>",
    "WebhookURL": "<Discord webhook URL>",
    "SendAlerts": false,
    "BaseURL": "<API base URL>",
    "Credentials": "<Base64-encoded Basic auth>",
    "AuthPayloadLocation": "<path to Authorise.json>",
    "RefreshTime": 5
  },
  "BackupToolApi": {
    "ApiUrlTemplate": "<URL template with {0} for server name>",
    "WebhookSecret": "<shared HMAC-SHA256 secret>",
    "SiteBaseUrl": "<public URL of this site>",
    "Servers": {
      "<ServerName>": {
        "ClientId": "<Basic auth client ID>",
        "ClientSecret": "<Basic auth client secret>"
      }
    }
  }
}
```

### ServerStatusReporter (App.config appSettings)

```xml
<appSettings>
  <add key="BaseURL" value="<API base URL>" />
  <add key="Credentials" value="<Base64-encoded Basic auth>" />
  <add key="AuthPayloadLocation" value="<path to Authorise.json>" />
  <add key="RefreshTime" value="<interval in minutes>" />
  <add key="HostName" value="<machine hostname>" />
  <add key="Games" value="<comma-separated game names>" />
  <add key="Components" value="<comma-separated component names>" />
</appSettings>
```

### ServerStatusAutomation (App.config appSettings)

```xml
<appSettings>
  <add key="WebhookURL" value="<Discord webhook URL>" />
  <add key="RecipientId" value="<Discord recipient ID (long)>" />
  <add key="SendAlerts" value="<true|false>" />
  <add key="BaseURL" value="<API base URL>" />
  <add key="Credentials" value="<Base64-encoded Basic auth>" />
  <add key="AuthPayloadLocation" value="<path to Authorise.json>" />
  <add key="RefreshTime" value="<interval in minutes>" />
</appSettings>
```

### Shared Settings Model

All three applications load configuration into a `SharedSettingsModel` with the following properties:

| Property | Purpose |
|---|---|
| `Domain` | Application domain |
| `WebhookURL` | Discord webhook URL (legacy, per-server URLs are now used from the API) |
| `RecipientId` | Discord recipient ID (long) for automated alerts |
| `SendAlerts` | Whether to send Discord alerts |
| `BaseURL` | API base URL |
| `Credentials` | Base64-encoded Basic auth credentials |
| `AuthPayloadLocation` | Path to authentication payload JSON |
| `RefreshTime` | Refresh interval in minutes |

## Data Persistence

### PID Files

- **Location:** `%PROGRAMDATA%\Hunter Industries\Server Backup Tool`
- **Format:** Text file containing process ID and UTC start time (ISO 8601)
- **Used by:** ServerStatusReporter (read) to verify server process status

### Authentication Payload

- **File:** `Authorise.json`
- **Format:** `{"Phrase": "<authentication phrase>"}`
- **Purpose:** Authentication payload sent to the API during authorisation

## Logging

- **Framework:** log4net 3.3.1

### Log Files

| Application | Log File | Appenders |
|---|---|---|
| ServerStatusSite | `Logs\Site.log` | RollingFile (INFO+) |
| ServerStatusReporter | `Logs\SSR.log` | Console (INFO-WARN) + RollingFile (INFO+) |
| ServerStatusAutomation | `Logs\SSA.log` | Console (INFO-WARN) + RollingFile (INFO+) |

### Log File Settings

- **Max File Size:** 10 MB
- **Backup Count:** 10 rolling files
- **Format:** `{ISO8601 Timestamp} {LEVEL} - {Message}`
- **Lock Model:** MinimalLock (concurrent access safe)

### Logger Identifiers

Log entries are prefixed with a contextual identifier:

| Application | Identifier |
|---|---|
| ServerStatusSite | User IP address or `{username} ({IP})` after login |
| ServerStatusReporter | `Reporter` |
| ServerStatusAutomation | `Automation` |

## External Integrations

### Hunter Industries API

- **Protocol:** REST over HTTPS
- **Authentication:** Bearer token (obtained via Basic auth + phrase payload)
- **Endpoints Used:**
  - Authentication and token management
  - Server information (CRUD)
  - Server events (create, query by component)
  - Alerts (create, update, paginated query)
  - Users and user settings (CRUD)

### Server Backup Tool API

- **Integration:** Live and archived log retrieval with real-time webhook streaming
- **Used by:** ServerStatusSite (Server Logs page)
- **Protocol:** REST over HTTPS with Basic Auth (per-server credentials)
- **Endpoints called:** `GET /logs` (paginated live logs), `GET /logs/archived` (archive list), `GET /logs/archived/{file}` (archived logs), `POST /webhooks` (register webhook), `DELETE /webhooks/{id}` (unregister webhook)
- **Webhook receiver:** `POST /webhooks/webhook` on the Site, authenticated via HMAC-SHA256 signature in `X-Webhook-Secret` header
- **Real-time flow:** Backup Tool pushes new logs to the Site's webhook endpoint → `LogStreamService` routes them to active Blazor components → Blazor Server pushes UI updates over its existing SignalR connection
- **Rendering:** `<Virtualize>` component renders only visible log entries (~30-50 DOM elements regardless of list size)
- **Reverse infinite scroll:** Initial fetch loads the most recent logs; scrolling up triggers loading of older logs via JS interop scroll detection

### Discord

- **Integration:** Webhook notifications with per-server channel targeting
- **Used by:** ServerStatusSite (alert creation/updates) and ServerStatusAutomation (automated alerts)
- **Message format:** Role/user mentions via `<@&recipientId>`
- **Webhook URL:** Stored per server in the API, allowing each server's alerts to be sent to a dedicated Discord channel
- **Recipient ID:** For new alerts (automated and user-raised), the recipient ID comes from application settings. For alert updates, the recipient ID comes from the server record in the API
- **Controlled by:** `SendAlerts` configuration flag

## CI/CD

### GitHub Actions Workflows

All workflows run on `windows-latest` using .NET 8.0.x SDK.

| Workflow | Trigger | Steps |
|---|---|---|
| **CI on Commit** (`Commit.yml`) | Push to any branch | Checkout, Restore, Build (Release) |
| **CI on Pull Request** (`Pull Request.yml`) | PR to any branch | Checkout, Restore, Build (Release), Run Tests with Coverage (`dotnet test --collect:"XPlat Code Coverage"`), Generate Coverage Report, Post Coverage Status, Upload Coverage Artifact, Publish all three applications, Upload timestamped artefacts |
| **Check for Linked Issue** (`PR Linked Issue.yml`) | PR opened/edited/reopened/synchronised | Verifies PR has linked GitHub issues via description, comments, or Development section |

### Pull Request Artefacts

The PR workflow publishes and uploads build artefacts for all three applications:

| Artefact | Source Project |
|---|---|
| `ServerStatusSite_{timestamp}` | ServerStatusSite |
| `ServerStatusReporter_{timestamp}` | ServerStatusReporter |
| `ServerStatusAutomation_{timestamp}` | ServerStatusAutomation |

### Build Configuration

- **SDK:** .NET 8.0.x
- **Configuration:** Release
- **Test Runner:** `dotnet test` (MSTest with method-level parallelisation)

### Code Coverage

- **Collector:** XPlat Code Coverage (via `coverlet.collector`)
- **Configuration:** `coverlet.runsettings` in solution root
- **Report Generator:** `dotnet-reportgenerator-globaltool`
- **Report Formats:** Cobertura, JsonSummary
- **Exclusions:** Program entry points (Site, Reporter, Automation), Models, generated code
- **CI Integration:** Coverage percentage posted to PR status and uploaded as artifact

## Hosting Requirements

### ServerStatusSite

- .NET 8.0 Runtime
- Windows or Linux (no OS-specific dependencies)
- IIS with ASP.NET Core Hosting Bundle (if hosting in IIS)
- HTTPS (port 443) for client access
- Outbound HTTPS to the API base URL

### ServerStatusReporter

- .NET 8.0 Runtime
- Windows (required for process verification via System.Diagnostics and System.Management)
- Outbound HTTPS to the API base URL
- TCP access to monitored server IP addresses and ports
- Read access to `%PROGRAMDATA%\Hunter Industries\Server Backup Tool` (PID files)

### ServerStatusAutomation

- .NET 8.0 Runtime
- Windows or Linux (no OS-specific dependencies)
- Outbound HTTPS to the API base URL
- Outbound HTTPS to Discord webhook URL

### Development Ports

| Profile | URL |
|---|---|
| HTTP | `http://localhost:5131` |
| HTTPS | `https://localhost:7275` |
