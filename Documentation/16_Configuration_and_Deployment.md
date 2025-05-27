# Configuration and Deployment

<details>
<summary>Relevant source files</summary>

The following files were used as context for generating this wiki page:

- [Logging/SystemEventLogger.cs](Logging/SystemEventLogger.cs)
- [Program.cs](Program.cs)
- [Properties/PublishProfiles/FolderProfile.pubxml](Properties/PublishProfiles/FolderProfile.pubxml)

</details>



This document covers the configuration, setup, and deployment procedures for the IstgHtmlDocxConvertService. It explains application startup configuration, dependency injection setup, logging system integration, and deployment processes. For information about the overall system architecture, see [System Architecture](#2). For development environment setup, see [Development Setup](#8).

## Application Configuration Overview

The service uses ASP.NET Core's standard configuration system with dependency injection for service management. The main application bootstrap occurs in `Program.cs`, which configures services, middleware pipeline, and external dependencies.

### Service Registration and Dependency Injection

```mermaid
graph TB
    subgraph "Application Bootstrap"
        WebApplicationBuilder["WebApplication.CreateBuilder(args)"]
        IConfiguration["IConfiguration"]
        ServiceCollection["builder.Services"]
    end
    
    subgraph "Service Registration"
        Controllers["AddControllers()"]
        Swagger["AddSwaggerGen()"]
        CORS["AddCors()"]
    end
    
    subgraph "Custom Services"
        SystemEventLogger["AddSingleton<SystemEventLogger>()"]
        SessionStorageService["AddSingleton<SessionStorageService>()"]
        SessionCleanupService["AddHostedService<SessionCleanupService>()"]
        ConversionService["AddScoped<ConversionService>()"]
        HtmlService["AddTransient<HtmlService>()"]
        WebSocketHandler["AddScoped<WebSocketHandler>()"]
        TokenValidationService["AddScoped<TokenValidationService>()"]
    end
    
    subgraph "Middleware Pipeline"
        UseSwagger["UseSwagger()"]
        UseSwaggerUI["UseSwaggerUI()"]
        UseHttpsRedirection["UseHttpsRedirection()"]
        UseCors["UseCors('CorsPolicy')"]
        UseWebSockets["UseWebSockets()"]
        WebSocketMiddleware["WebSocket Custom Middleware"]
        MapControllers["MapControllers()"]
    end
    
    WebApplicationBuilder --> IConfiguration
    WebApplicationBuilder --> ServiceCollection
    
    ServiceCollection --> Controllers
    ServiceCollection --> Swagger
    ServiceCollection --> CORS
    ServiceCollection --> SystemEventLogger
    ServiceCollection --> SessionStorageService
    ServiceCollection --> SessionCleanupService
    ServiceCollection --> ConversionService
    ServiceCollection --> HtmlService
    ServiceCollection --> WebSocketHandler
    ServiceCollection --> TokenValidationService
    
    ServiceCollection --> UseSwagger
    UseSwagger --> UseSwaggerUI
    UseSwaggerUI --> UseHttpsRedirection
    UseHttpsRedirection --> UseCors
    UseCors --> UseWebSockets
    UseWebSockets --> WebSocketMiddleware
    WebSocketMiddleware --> MapControllers
```

**Sources:** [Program.cs:5-25](), [Program.cs:42-74]()

### Service Lifecycle Configuration

The application registers services with different lifecycles based on their usage patterns:

| Service | Lifecycle | Purpose |
|---------|-----------|---------|
| `SystemEventLogger` | Singleton | Windows Event Log integration, shared across application |
| `SessionStorageService` | Singleton | Thread-safe session storage using `ConcurrentDictionary` |
| `SessionCleanupService` | Hosted Service | Background service for automatic session cleanup |
| `ConversionService` | Scoped | Document conversion operations, per-request lifecycle |
| `HtmlService` | Transient | HTML processing utilities, lightweight stateless operations |
| `WebSocketHandler` | Scoped | WebSocket connection management, per-connection lifecycle |
| `TokenValidationService` | Scoped | Authentication validation, per-request lifecycle |

**Sources:** [Program.cs:19-25]()

## Configuration Settings Structure

### CORS Configuration

The application reads allowed origins from configuration and sets up CORS policy:

```mermaid
graph LR
    AppSettings["appsettings.json"]
    AllowedOrigins["AllowedOrigins[]"]
    CorsPolicy["CorsPolicy"]
    WithOrigins["WithOrigins()"]
    AllowAnyHeader["AllowAnyHeader()"]
    AllowAnyMethod["AllowAnyMethod()"]
    
    AppSettings --> AllowedOrigins
    AllowedOrigins --> CorsPolicy
    CorsPolicy --> WithOrigins
    CorsPolicy --> AllowAnyHeader
    CorsPolicy --> AllowAnyMethod
```

**Sources:** [Program.cs:27-40]()

### Aspose License Configuration

The service requires Aspose.Words licensing configuration:

```mermaid
graph TB
    Configuration["IConfiguration"]
    LicensePath["Aspose:LicensePath"]
    FileExists["File.Exists()"]
    AsposeWordsLicense["new Aspose.Words.License()"]
    SetLicense["SetLicense(licensePath)"]
    ConsoleWarning["Console.WriteLine()"]
    
    Configuration --> LicensePath
    LicensePath --> FileExists
    FileExists -->|exists| AsposeWordsLicense
    FileExists -->|missing| ConsoleWarning
    AsposeWordsLicense --> SetLicense
```

**Sources:** [Program.cs:76-88]()

## Logging System Configuration

### SystemEventLogger Setup

The `SystemEventLogger` integrates with Windows Event Log system:

```mermaid
graph TB
    IConfiguration["IConfiguration"]
    LoggingSection["Logging:Name"]
    LogName["logName parameter"]
    EventLogSourceExists["EventLog.SourceExists()"]
    CreateEventSource["EventLog.CreateEventSource()"]
    SystemEventLogger["SystemEventLogger instance"]
    
    subgraph "Log Methods"
        Info["Info(message)"]
        Warn["Warn(message)"]
        Error["Error(message)"]
        WriteEntry["WriteEntry(message, type)"]
        EventLogWriteEntry["EventLog.WriteEntry()"]
    end
    
    IConfiguration --> LoggingSection
    LogName --> SystemEventLogger
    LoggingSection --> SystemEventLogger
    EventLogSourceExists -->|false| CreateEventSource
    CreateEventSource --> SystemEventLogger
    
    SystemEventLogger --> Info
    SystemEventLogger --> Warn
    SystemEventLogger --> Error
    Info --> WriteEntry
    Warn --> WriteEntry
    Error --> WriteEntry
    WriteEntry --> EventLogWriteEntry
```

**Sources:** [Logging/SystemEventLogger.cs:10-40]()

### Event Log Entry Types

The logger supports three standard Windows Event Log entry types:

| Method | Event Type | Usage |
|--------|------------|-------|
| `Info(string message)` | `EventLogEntryType.Information` | General information messages |
| `Warn(string message)` | `EventLogEntryType.Warning` | Warning conditions |
| `Error(string message)` | `EventLogEntryType.Error` | Error conditions and exceptions |

**Sources:** [Logging/SystemEventLogger.cs:26-39]()

## WebSocket Middleware Configuration

The application implements custom WebSocket middleware for real-time communication:

```mermaid
graph TB
    HTTPContext["HttpContext"]
    IsWebSocketRequest["context.WebSockets.IsWebSocketRequest"]
    CreateScope["app.Services.CreateScope()"]
    GetRequiredService["GetRequiredService<WebSocketHandler>()"]
    AcceptWebSocketAsync["context.WebSockets.AcceptWebSocketAsync()"]
    HandleAsync["handler.HandleAsync(webSocket)"]
    NextMiddleware["await next()"]
    
    HTTPContext --> IsWebSocketRequest
    IsWebSocketRequest -->|true| CreateScope
    IsWebSocketRequest -->|false| NextMiddleware
    CreateScope --> GetRequiredService
    GetRequiredService --> AcceptWebSocketAsync
    AcceptWebSocketAsync --> HandleAsync
```

**Sources:** [Program.cs:55-69]()

## Deployment Configuration

### Publish Profile Settings

The application includes a folder-based publish profile for deployment:

```mermaid
graph LR
    FolderProfile["FolderProfile.pubxml"]
    PropertyGroup["PropertyGroup"]
    
    subgraph "Deployment Settings"
        DeleteExistingFiles["DeleteExistingFiles: false"]
        ExcludeAppData["ExcludeApp_Data: false"]
        LaunchSiteAfterPublish["LaunchSiteAfterPublish: true"]
        BuildConfiguration["LastUsedBuildConfiguration: Release"]
        Platform["LastUsedPlatform: Any CPU"]
        PublishProvider["PublishProvider: FileSystem"]
        PublishUrl["PublishUrl: D:\\Release-Management\\IstgHtmlDocxConvertService"]
        WebPublishMethod["WebPublishMethod: FileSystem"]
        TargetId["_TargetId: Folder"]
    end
    
    FolderProfile --> PropertyGroup
    PropertyGroup --> DeleteExistingFiles
    PropertyGroup --> ExcludeAppData
    PropertyGroup --> LaunchSiteAfterPublish
    PropertyGroup --> BuildConfiguration
    PropertyGroup --> Platform
    PropertyGroup --> PublishProvider
    PropertyGroup --> PublishUrl
    PropertyGroup --> WebPublishMethod
    PropertyGroup --> TargetId
```

**Sources:** [Properties/PublishProfiles/FolderProfile.pubxml:6-16]()

### Deployment Settings Explained

| Setting | Value | Purpose |
|---------|-------|---------|
| `DeleteExistingFiles` | `false` | Preserves existing files during deployment |
| `ExcludeApp_Data` | `false` | Includes App_Data folder in deployment |
| `LaunchSiteAfterPublish` | `true` | Automatically launches site after publishing |
| `LastUsedBuildConfiguration` | `Release` | Uses Release build configuration |
| `PublishProvider` | `FileSystem` | Publishes to file system (not cloud) |
| `PublishUrl` | `D:\Release-Management\IstgHtmlDocxConvertService` | Target deployment directory |
| `WebPublishMethod` | `FileSystem` | File system-based publishing method |

**Sources:** [Properties/PublishProfiles/FolderProfile.pubxml:7-15]()