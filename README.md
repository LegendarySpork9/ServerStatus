# ServerStatus [![Commit Status](https://github.com/LegendarySpork9/ServerStatus/actions/workflows/Commit.yml/badge.svg?event=push)](https://github.com/LegendarySpork9/ServerStatus/actions/workflows/Commit.yml) [![PR Status](https://github.com/LegendarySpork9/ServerStatus/actions/workflows/Pull%20Request.yml/badge.svg)](https://github.com/LegendarySpork9/ServerStatus/actions/workflows/Pull%20Request.yml)

Monitoring suite for game servers. Provides a web dashboard for real-time status and alert management, a per-server reporter for health checks, and an automation service for detecting missed events and sending Discord notifications.

## Components

| Component | Type | Description |
|-----------|------|-------------|
| ServerStatusSite | Blazor Server (.NET 8.0) | Web UI for status viewing, alert management, and live log streaming |
| ServerStatusReporter | Console App (.NET 8.0) | Per-machine service checking PC, server, and connection status |
| ServerStatusAutomation | Console App (.NET 8.0) | Detects stale events, creates alerts, and sends Discord notifications |
| ServerStatusCommon | Class Library (.NET 8.0) | Shared abstractions, services, and models |

## Tech Stack

| Technology | Version |
|------------|---------|
| .NET | 8.0 |
| Language | C# |
| UI Framework | Blazor Server |
| Notifications | Discord Webhooks |

## Status

Active development

## Documentation

Setup, configuration, and usage documentation is available on the [LegendarySpork9 Wiki](https://wiki.legendaryspork9.co.uk).

---

Copyright © 2026 Toby Hunter, All rights reserved.
