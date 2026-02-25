# Test Companion

A cross-platform desktop application for managing exploratory testing sessions. Built with [Uno Platform](https://platform.uno/) and .NET 10, Test Companion helps testers plan, execute, and document session-based testing with structured reporting.

[![Build](https://github.com/andreitdr/TestCompanion/actions/workflows/build.yml/badge.svg)](https://github.com/andreitdr/TestCompanion/actions/workflows/build.yml)

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Supported Platforms](#supported-platforms)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Building](#building)
- [Publishing](#publishing)
- [Project Structure](#project-structure)
- [Coverage Areas](#coverage-areas)
- [Export Formats](#export-formats)
- [Configuration](#configuration)
- [License](#license)

---

## Overview

Test Companion is a session-based test management tool designed for exploratory testing. It provides a structured way to capture test sessions, track bugs and issues, attach files, and generate detailed reports -- all from a single desktop application that runs on Windows, macOS, and Linux.

## Features

- **Session Management** -- Create and manage testing sessions with titles, tester names, start times, and duration tracking.
- **Coverage Area Selection** -- Navigate a hierarchical tree of test coverage areas loaded from a configurable INI file.
- **Task Breakdown Tracking** -- Record time allocation across session setup, test design/execution, and bug investigation.
- **Charter vs. Opportunity Split** -- Track the balance between planned charter work and opportunistic testing.
- **Bug Tracking** -- Log bugs with title, description, actual result, expected result, and related files.
- **Issue Tracking** -- Capture issues and blockers encountered during testing.
- **File Attachments** -- Attach supporting files (screenshots, logs, etc.) to sessions.
- **Test Notes** -- Free-form notes captured during the session.
- **Auto-Save** -- Session state is automatically persisted to local cache, preventing data loss.
- **Report Generation** -- Export session reports in multiple formats.
- **Report Import** -- Re-import previously exported JSON reports for review or continuation.
- **Configurable Settings** -- Customize export paths and default export formats.

## Supported Platforms

| Platform | Architectures        |
|----------|----------------------|
| Windows  | x64, x86, ARM64      |
| macOS    | x64 (Intel), ARM64 (Apple Silicon) |
| Linux    | x64, ARM64, ARM32    |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Uno Platform SDK (resolved automatically via `global.json`)

## Getting Started

1. Clone the repository:

   ```
   git clone <repository-url>
   cd TestCompanion
   ```

2. Restore dependencies:

   ```
   dotnet restore
   ```

3. Run the application:

   ```
   dotnet run --project TestCompanion -f net10.0-desktop
   ```

## Building

Build the project in Debug configuration:

```
dotnet build -c Debug -f net10.0-desktop
```

Build in Release configuration:

```
dotnet build -c Release -f net10.0-desktop
```

## Publishing

Self-contained, single-file executables can be produced for all supported platforms. Use the included build scripts to publish for every target at once:

**macOS / Linux:**

```
./builder.sh
```

**Windows:**

```
builder.bat
```

Or publish for a specific runtime manually:

```
dotnet publish -c Release -f net10.0-desktop -r osx-arm64 --self-contained -p:PublishSingleFile=true
```

Published binaries are placed under `bin/Release/net10.0-desktop/<rid>/`.

## Project Structure

```
TestCompanion/
  Models/              Domain models
    AppSettings.cs       Application settings (export path, format)
    AreaNode.cs          Hierarchical coverage area node
    BugEntry.cs          Bug report data
    IssueEntry.cs        Issue/blocker data
    SessionModel.cs      Core testing session model
  Services/            Application services
    AutoSaveService.cs   Local session cache (auto-save/restore)
    CoverageIniParser.cs Parser for coverage area INI files
    ReportGeneratorService.cs  Multi-format report generation
    ReportImportService.cs     JSON report import
    SettingsService.cs   Persistent user settings
  ViewModels/          MVVM view models
    BugEntryViewModel.cs
    IssueEntryViewModel.cs
    SessionViewModel.cs
  Components/Pages/    UI page components
  Platforms/Desktop/   Desktop entry point (Program.cs)
  Assets/
    coverage.ini         Default coverage area definitions
    Icons/               Application icons (SVG)
    Splash/              Splash screen assets
  Strings/en/          Localized string resources
  Properties/          Launch settings and publish profiles
```

## Coverage Areas

Test coverage areas are defined in `Assets/coverage.ini` using a pipe-delimited hierarchy:

```ini
# Format: Category | Subcategory | SubSubcategory | ...
Web | Authentication | Login
Web | Authentication | Logout
Web | Dashboard | Widgets
API | REST | GET
API | REST | POST
```

Lines starting with `#` or `;` are treated as comments. Edit this file to match the areas relevant to your project under test.

## Export Formats

Session reports can be exported in four formats:

| Format     | Extension | Description                          |
|------------|-----------|--------------------------------------|
| Plain Text | `.txt`    | Human-readable plain text report     |
| Markdown   | `.md`     | Markdown-formatted report            |
| HTML       | `.html`   | Styled HTML report                   |
| JSON       | `.json`   | Machine-readable, re-importable data |

Reports are saved to `~/Documents/TestingSessionReports/` by default. The export directory and default format can be changed in the application settings.

## Configuration

Application data is stored in the local application data directory:

| Data            | Location                                          |
|-----------------|---------------------------------------------------|
| Settings        | `{LocalAppData}/TestCompanion/settings.json`      |
| Session Cache   | `{LocalAppData}/TestCompanion/Cache/session_cache.json` |


