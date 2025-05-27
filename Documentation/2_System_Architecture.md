# System Architecture

<details>
<summary>Relevant source files</summary>

The following files were used as context for generating this wiki page:

- [Program.cs](Program.cs)
- [Services/ConversionService.cs](Services/ConversionService.cs)
- [Services/SessionStorageService.cs](Services/SessionStorageService.cs)
- [WebSockets/WebSocketHandler.cs](WebSockets/WebSocketHandler.cs)

</details>



This document explains the high-level architecture of the IstgHtmlDocxConvertService, including its layered design, dependency injection configuration, WebSocket communication patterns, and session management infrastructure. The system implements a multi-protocol document conversion service that supports both synchronous HTTP APIs and real-time WebSocket communication for collaborative document editing workflows.

For specific API endpoint documentation, see [HTTP API Reference](#3). For real-time communication details, see [Real-time Communication](#5). For error handling patterns, see [Error Handling](#6).

## Application Startup and Dependency Injection

The application uses ASP.NET Core's built-in dependency injection container with a carefully designed service lifetime strategy. The startup configuration establishes the foundation for the entire system architecture.

### Service Registration Architecture

```mermaid
graph TB
    subgraph "Program.cs Service Registration"
        WebApp["WebApplication.CreateBuilder()"]
        
        subgraph "Singleton Services"
            SystemEventLogger["SystemEventLogger"]
            SessionStorageService["SessionStorageService"]
        end
        
        subgraph "Hosted Services"
            SessionCleanupService["SessionCleanupService"]
        end
        
        subgraph "Scoped Services"
            ConversionService["ConversionService"]
            WebSocketHandler["WebSocketHandler"]
            TokenValidationService["TokenValidationService"]
        end
        
        subgraph "Transient Services"
            HtmlService["HtmlService"]
        end
        
        subgraph "Infrastructure"
            Controllers["AddControllers()"]
            Swagger["AddSwaggerGen()"]
            CORS["AddCors()"]
            WebSockets["UseWebSockets()"]
        end
    end
    
    WebApp --> SystemEventLogger
    WebApp --> SessionStorageService
    WebApp --> SessionCleanupService
    WebApp --> ConversionService
    WebApp --> WebSocketHandler
    WebApp --> TokenValidationService
    WebApp --> HtmlService
    WebApp --> Controllers
    WebApp --> Swagger
    WebApp --> CORS
    WebApp --> WebSockets
```

The service registration follows a specific pattern where stateful services like `SessionStorageService` are registered as singletons to maintain session data across requests, while request-specific services like `ConversionService` are scoped to ensure proper resource management.

Sources: [Program.cs:19-25]()

### Middleware Pipeline Architecture

```mermaid
graph TB
    Request["HTTP Request"] --> HttpsRedirection["UseHttpsRedirection()"]
    HttpsRedirection --> CORS["UseCors('CorsPolicy')"]
    CORS --> WebSocketMiddleware["WebSocket Middleware"]
    
    subgraph "WebSocket Processing"
        WebSocketMiddleware --> WebSocketCheck{"IsWebSocketRequest?"}
        WebSocketCheck -->|Yes| CreateScope["CreateScope()"]
        CreateScope --> GetHandler["GetRequiredService<WebSocketHandler>()"]
        GetHandler --> AcceptWebSocket["AcceptWebSocketAsync()"]
        AcceptWebSocket --> HandleAsync["handler.HandleAsync(webSocket)"]
    end
    
    WebSocketCheck -->|No| Authorization["UseAuthorization()"]
    Authorization --> MapControllers["MapControllers()"]
    
    subgraph "Aspose License"
        LicenseConfig["Configuration['Aspose:LicensePath']"]
        LicenseConfig --> LicenseCheck{"File.Exists(licensePath)?"}
        LicenseCheck -->|Yes| SetLicense["new Aspose.Words.License().SetLicense()"]
        LicenseCheck -->|No| LogError["Console.WriteLine('License path missing')"]
    end
```

The middleware pipeline demonstrates the dual-protocol nature of the system, handling both traditional HTTP requests through controllers and WebSocket connections through custom middleware.

Sources: [Program.cs:55-69](), [Program.cs:76-88]()

## Core Service Layer Architecture

The service layer implements a hierarchical dependency structure with clear separation of concerns. Each service has specific responsibilities and dependencies that create a well-defined architecture.

### Service Dependency Graph

```mermaid
graph TB
    subgraph "Controllers"
        ConversionController["ConversionController"]
        EditInWordController["EditInWordController"]
    end
    
    subgraph "Core Services"
        ConversionService["ConversionService"]
        SessionStorageService["SessionStorageService"]
        TokenValidationService["TokenValidationService"]
        HtmlService["HtmlService"]
        WebSocketHandler["WebSocketHandler"]
    end
    
    subgraph "Infrastructure Services"
        SystemEventLogger["SystemEventLogger"]
        IConfiguration["IConfiguration"]
    end
    
    subgraph "Background Services"
        SessionCleanupService["SessionCleanupService"]
    end
    
    subgraph "External Dependencies"
        AsposeWords["Aspose.Words.Document"]
        FileSystem["File System"]
        Database["SQL Database"]
    end
    
    ConversionController --> ConversionService
    ConversionController --> TokenValidationService
    EditInWordController --> ConversionService
    EditInWordController --> TokenValidationService
    
    WebSocketHandler --> ConversionService
    WebSocketHandler --> SessionStorageService
    WebSocketHandler --> TokenValidationService
    WebSocketHandler --> SystemEventLogger
    
    ConversionService --> HtmlService
    ConversionService --> SessionStorageService
    ConversionService --> TokenValidationService
    ConversionService --> SystemEventLogger
    ConversionService --> IConfiguration
    ConversionService --> AsposeWords
    ConversionService --> FileSystem
    
    SessionStorageService --> SystemEventLogger
    SessionStorageService --> IConfiguration
    SessionStorageService --> FileSystem
    
    SessionCleanupService --> SessionStorageService
    
    TokenValidationService --> Database
```

Sources: [Services/ConversionService.cs:24-36](), [Services/SessionStorageService.cs:18-26](), [WebSockets/WebSocketHandler.cs:20-26]()

### Service Lifetime and Resource Management

| Service | Lifetime | Purpose | Key Dependencies |
|---------|----------|---------|------------------|
| `SystemEventLogger` | Singleton | Windows Event Log integration | None |
| `SessionStorageService` | Singleton | In-memory session storage using `ConcurrentDictionary` | `IConfiguration`, `SystemEventLogger` |
| `SessionCleanupService` | Hosted | Background cleanup of expired sessions | `SessionStorageService` |
| `ConversionService` | Scoped | Document conversion using Aspose.Words | `HtmlService`, `SessionStorageService`, `TokenValidationService` |
| `WebSocketHandler` | Scoped | WebSocket message processing | `ConversionService`, `SessionStorageService`, `TokenValidationService` |
| `TokenValidationService` | Scoped | Database-based token validation | SQL Database |
| `HtmlService` | Transient | HTML processing utilities | None |

Sources: [Program.cs:19-25]()

## Session Management Architecture

The session management system uses a `ConcurrentDictionary<string, HtmlSession>` as the core storage mechanism, providing thread-safe operations for multiple concurrent WebSocket connections.

### Session Storage Implementation

```mermaid
graph TB
    subgraph "SessionStorageService Internal Architecture"
        ConcurrentDict["ConcurrentDictionary<string, HtmlSession>"]
        
        subgraph "HtmlSession Properties"
            UserId["UserId: string"]
            SessionId["SessionId: string"]
            Html["Html: string"]
            CreatedAt["CreatedAt: DateTime"]
            LastUpdated["LastUpdated: DateTime"]
            LastUserInteraction["LastUserInteraction: DateTime"]
            ClientSocket["ClientSocket: WebSocket?"]
            WordSocket["WordSocket: WebSocket?"]
            WordFilePath["WordFilePath: string?"]
        end
        
        subgraph "Core Methods"
            CreateSession["CreateSession(userId, initialHtml)"]
            Get["Get(sessionId)"]
            SaveOrUpdate["SaveOrUpdate(sessionId, html)"]
            RemoveSession["RemoveSession(sessionId)"]
            TryRegisterSocket["TryRegisterSocket(sessionId, socket, origin)"]
            UnregisterSocket["UnregisterSocket(sessionId, socket)"]
        end
        
        subgraph "Configuration"
            SessionTTL["SessionTTLMinutes: 30"]
            MaxSessionLifetime["MaxSessionLifetimeMinutes: 120"]
            MaxSessions["MaxSessions: 2"]
        end
    end
    
    ConcurrentDict --> HtmlSession
    CreateSession --> ConcurrentDict
    Get --> ConcurrentDict
    SaveOrUpdate --> ConcurrentDict
    RemoveSession --> ConcurrentDict
    TryRegisterSocket --> ConcurrentDict
    UnregisterSocket --> ConcurrentDict
```

The session lifecycle management includes automatic cleanup based on both inactivity timeout (`SessionTTLMinutes`) and absolute lifetime limits (`MaxSessionLifetimeMinutes`).

Sources: [Services/SessionStorageService.cs:12-26](), [Services/SessionStorageService.cs:27-45](), [Services/SessionStorageService.cs:181-208]()

### WebSocket Registration and Authentication

```mermaid
graph LR
    subgraph "Socket Registration Flow"
        SocketConnect["WebSocket Connection"] --> InitMessage["Initial SocketMessageRequest"]
        InitMessage --> ValidateSession{"Session Exists?"}
        ValidateSession -->|No| SessionNotFound["Send SessionNotFound Error"]
        ValidateSession -->|Yes| ValidateToken{"Valid Token?"}
        ValidateToken -->|No| InvalidToken["Send InvalidToken Error"]
        ValidateToken -->|Yes| RegisterSocket["TryRegisterSocket(sessionId, socket, origin)"]
        
        subgraph "Origin-based Registration"
            RegisterSocket --> CheckOrigin{"Origin Type?"}
            CheckOrigin -->|"Origins.Client"| RegisterClient["session.ClientSocket = socket"]
            CheckOrigin -->|"Origins.Word"| RegisterWord["session.WordSocket = socket"]
        end
        
        RegisterClient --> SocketReady["Socket Ready for Messages"]
        RegisterWord --> SocketReady
    end
```

Sources: [WebSockets/WebSocketHandler.cs:54-91](), [Services/SessionStorageService.cs:117-146]()

## WebSocket Communication Architecture

The WebSocket communication system implements a message-based protocol with specific action handlers and error management. The system supports bidirectional communication between web clients and Microsoft Word applications.

### WebSocket Message Processing Flow

```mermaid
graph TB
    subgraph "WebSocketHandler.HandleAsync() Flow"
        ReceiveMessage["ReceiveMessageAsync()"] --> ParseMessage["JsonSerializer.Deserialize<SocketMessageRequest>()"]
        ParseMessage --> ValidateSession["_storage.Get(message.SessionId)"]
        ValidateSession --> ValidateToken["_tokenValidationService.IsTokenValid()"]
        ValidateToken --> RegisterSocket["_storage.TryRegisterSocket()"]
        
        RegisterSocket --> MessageLoop["Message Processing Loop"]
        
        subgraph "Message Loop"
            ReceiveNext["Receive Next Message"] --> CheckSession{"Session Exists?"}
            CheckSession -->|No| SendSessionExpired["Send SessionExpiredOrRemoved Error"]
            CheckSession -->|Yes| CheckAuth{"Socket Authenticated?"}
            CheckAuth -->|No| SendAuthError["Send SocketNotAuthenticated Error"]
            CheckAuth -->|Yes| RouteAction["Route to Action Handler"]
            
            subgraph "Action Handlers"
                RouteAction --> HandleUpdate["HandleUpdate(WebSocketActions.UpdateOoxml)"]
                RouteAction --> HandleGet["HandleGet(WebSocketActions.GetHtml)"]
                RouteAction --> HandleEnd["HandleEnd(WebSocketActions.EndSession)"]
            end
        end
    end
    
    HandleUpdate --> ConvertOOXML["_conversionService.ConvertOoxmlToHtml()"]
    ConvertOOXML --> SaveHtml["_storage.SaveOrUpdate(sessionId, html)"]
    SaveHtml --> BroadcastUpdate["Broadcast to GetActiveSockets()"]
    
    HandleGet --> RetrieveHtml["session.Html"]
    RetrieveHtml --> SendResponse["Send SocketMessageResponse"]
    
    HandleEnd --> CleanupSession["_storage.RemoveSession()"]
    CleanupSession --> NotifyClients["Broadcast EndSession to all sockets"]
```

Sources: [WebSockets/WebSocketHandler.cs:54-152](), [WebSockets/WebSocketHandler.cs:154-192](), [WebSockets/WebSocketHandler.cs:194-218](), [WebSockets/WebSocketHandler.cs:220-241]()

### WebSocket Protocol Message Types

| Message Type | Action | Direction | Purpose |
|--------------|--------|-----------|---------|
| `SocketMessageRequest` | `WebSocketActions.UpdateOoxml` | Word → Server | Send OOXML content for conversion |
| `SocketMessageRequest` | `WebSocketActions.GetHtml` | Client → Server | Request current HTML content |
| `SocketMessageRequest` | `WebSocketActions.EndSession` | Any → Server | Terminate session |
| `SocketMessageResponse` | `WebSocketActions.GetHtml` | Server → Client | Broadcast HTML updates |
| `SocketMessageError` | Various | Server → Client | Error notifications with error codes |

Sources: [WebSockets/WebSocketHandler.cs:27-32]()

## Document Conversion Architecture

The document conversion system leverages Aspose.Words as the core engine with custom HTML processing and session integration. The system supports multiple conversion workflows including synchronous HTTP conversion and asynchronous WebSocket-based conversion.

### Conversion Service Implementation

```mermaid
graph TB
    subgraph "ConversionService Core Methods"
        ConvertHtmlToDocx["ConvertHtmlToDocx(htmlContent)"]
        ConvertDocxToHtml["ConvertDocxToHtml(docxFile)"]
        GenerateWordLaunchLink["GenerateWordLaunchLink(token, htmlContent)"]
        ConvertOoxmlToHtml["ConvertOoxmlToHtml(ooxmlContent)"]
    end
    
    subgraph "Aspose.Words Integration"
        Document["new Document(stream, loadOptions)"]
        LoadOptions["LoadOptions { LoadFormat, Encoding }"]
        SaveOptions["HtmlSaveOptions { ExportImagesAsBase64, PrettyFormat }"]
        SaveFormat["SaveFormat.Docx / SaveFormat.Html"]
    end
    
    subgraph "Session Integration"
        CreateSession["_storage.CreateSession(userId, htmlContent)"]
        EmbedProperties["document.CustomDocumentProperties.Add()"]
        SetWordFilePath["_storage.SetWordFilePath(sessionId, filePath)"]
    end
    
    subgraph "File Management"
        TempFilesFolderPath["_configuration['TempFilesFolderPath']"]
        PublicHostingFolderUrl["_configuration['PublicHostingFolderUrl']"]
        WordLaunchUrl["ms-word:ofe|u|{publicUrl}"]
    end
    
    ConvertHtmlToDocx --> Document
    ConvertHtmlToDocx --> LoadOptions
    ConvertHtmlToDocx --> SaveFormat
    
    ConvertDocxToHtml --> Document
    ConvertDocxToHtml --> SaveOptions
    ConvertDocxToHtml --> HtmlService["_htmlService.ExtractBodyInnerHtml()"]
    
    GenerateWordLaunchLink --> CreateSession
    GenerateWordLaunchLink --> Document
    GenerateWordLaunchLink --> EmbedProperties
    GenerateWordLaunchLink --> SetWordFilePath
    GenerateWordLaunchLink --> WordLaunchUrl
    
    ConvertOoxmlToHtml --> Document
    ConvertOoxmlToHtml --> SaveOptions
    ConvertOoxmlToHtml --> HtmlService
```

Sources: [Services/ConversionService.cs:41-74](), [Services/ConversionService.cs:79-132](), [Services/ConversionService.cs:138-181](), [Services/ConversionService.cs:183-222]()

### Aspose.Words Configuration and Licensing

The system requires proper Aspose.Words licensing configuration during application startup. The license path is configured through `appsettings.json` and validated at startup.

```mermaid
graph LR
    AppStartup["Application Startup"] --> ReadConfig["Configuration['Aspose:LicensePath']"]
    ReadConfig --> CheckFile{"File.Exists(licensePath)?"}
    CheckFile -->|Yes| SetLicense["new Aspose.Words.License().SetLicense(licensePath)"]
    CheckFile -->|No| LogError["Console.WriteLine('License path missing')"]
    SetLicense --> EnabledConversion["Full Aspose.Words Features"]
    LogError --> LimitedConversion["Limited/Trial Mode"]
```

Sources: [Program.cs:76-88]()

## Background Services Architecture

The system includes background services for maintenance tasks, specifically session cleanup to prevent resource leaks and manage expired sessions.

### Session Cleanup Service

```mermaid
graph TB
    subgraph "SessionCleanupService"
        HostedService["IHostedService Implementation"]
        ExecuteAsync["ExecuteAsync(CancellationToken)"]
        Timer["Periodic Timer (configurable interval)"]
    end
    
    subgraph "Cleanup Logic"
        CleanupCall["CleanupExpiredSessionsAndReturnSocketsToClose()"]
        CheckExpiration["Check LastUserInteraction vs SessionTTLMinutes"]
        CheckMaxLifetime["Check CreatedAt vs MaxSessionLifetimeMinutes"]
        CollectSockets["Collect WebSockets to Close"]
        RemoveSessions["RemoveSession() for expired sessions"]
        DeleteFiles["Delete WordFilePath files"]
    end
    
    Timer --> CleanupCall
    CleanupCall --> CheckExpiration
    CleanupCall --> CheckMaxLifetime
    CheckExpiration --> CollectSockets
    CheckMaxLifetime --> CollectSockets
    CollectSockets --> RemoveSessions
    RemoveSessions --> DeleteFiles
```

Sources: [Services/SessionStorageService.cs:181-208](), [Program.cs:21]()

## External Dependencies and Configuration

The system integrates with several external dependencies that require specific configuration and initialization patterns.

### Configuration Architecture

| Configuration Section | Purpose | Used By |
|----------------------|---------|---------|
| `AllowedOrigins` | CORS policy configuration | Startup pipeline |
| `Aspose:LicensePath` | Aspose.Words license file path | License initialization |
| `TempFilesFolderPath` | Temporary file storage location | ConversionService |
| `PublicHostingFolderUrl` | Public URL for Word launch links | ConversionService |
| `SessionTTLMinutes` | Session inactivity timeout | SessionStorageService |
| `MaxSessionLifetimeMinutes` | Maximum session duration | SessionStorageService |
| `MaxSessions` | Per-user session limit | SessionStorageService |

Sources: [Program.cs:28](), [Program.cs:77](), [Services/ConversionService.cs:31-32](), [Services/SessionStorageService.cs:22-24]()