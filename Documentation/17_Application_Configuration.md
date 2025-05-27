# Application Configuration

<details>
<summary>Relevant source files</summary>

The following files were used as context for generating this wiki page:

- [IstgHtmlDocxConvertService.csproj](IstgHtmlDocxConvertService.csproj)
- [Program.cs](Program.cs)
- [Properties/launchSettings.json](Properties/launchSettings.json)

</details>



This page covers the application startup process, dependency injection configuration, and configuration settings for the IstgHtmlDocxConvertService. It focuses on how the application initializes services, configures middleware, and loads configuration values during startup.

For information about logging configuration, see [Logging System](#7.2). For deployment and publishing configuration, see [Deployment and Publishing](#7.3).

## Application Startup Process

The application follows the standard ASP.NET Core startup pattern using the `WebApplication.CreateBuilder` approach. The startup process is defined in the main entry point and handles service registration, configuration loading, and middleware pipeline setup.

### Startup Flow

```mermaid
flowchart TD
    A["WebApplication.CreateBuilder(args)"] --> B["Service Registration"]
    B --> C["Configuration Loading"]
    C --> D["WebApplication.Build()"]
    D --> E["Middleware Pipeline Setup"]
    E --> F["Aspose License Configuration"]
    F --> G["app.Run()"]
    
    B --> B1["AddControllers()"]
    B --> B2["AddSwaggerGen()"]
    B --> B3["Service Dependencies"]
    B --> B4["AddCors()"]
    
    B3 --> B31["SystemEventLogger (Singleton)"]
    B3 --> B32["SessionStorageService (Singleton)"]
    B3 --> B33["SessionCleanupService (HostedService)"]
    B3 --> B34["ConversionService (Scoped)"]
    B3 --> B35["HtmlService (Transient)"]
    B3 --> B36["WebSocketHandler (Scoped)"]
    B3 --> B37["TokenValidationService (Scoped)"]
    
    E --> E1["UseSwagger()"]
    E --> E2["UseSwaggerUI()"]
    E --> E3["UseHttpsRedirection()"]
    E --> E4["UseCors()"]
    E --> E5["UseWebSockets()"]
    E --> E6["Custom WebSocket Middleware"]
    E --> E7["UseAuthorization()"]
    E --> E8["MapControllers()"]
```

Sources: [Program.cs:5-90]()

## Dependency Injection Container

The application uses the built-in ASP.NET Core dependency injection container to register services with appropriate lifetimes based on their usage patterns and thread safety requirements.

### Service Registration Configuration

| Service | Lifetime | Purpose |
|---------|----------|---------|
| `SystemEventLogger` | Singleton | Windows Event Log integration |
| `SessionStorageService` | Singleton | Thread-safe session storage |
| `SessionCleanupService` | HostedService | Background session cleanup |
| `ConversionService` | Scoped | Document conversion operations |
| `HtmlService` | Transient | HTML processing utilities |
| `WebSocketHandler` | Scoped | WebSocket connection management |
| `TokenValidationService` | Scoped | Authentication token validation |

### Service Dependencies Diagram

```mermaid
graph TB
    subgraph "Singleton Services"
        SystemEventLogger["SystemEventLogger"]
        SessionStorageService["SessionStorageService"]
    end
    
    subgraph "Background Services"
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
    
    subgraph "Framework Services"
        Controllers["Controllers"]
        SwaggerGen["SwaggerGen"]
        EndpointsApiExplorer["EndpointsApiExplorer"]
    end
    
    SessionCleanupService --> SessionStorageService
    ConversionService --> HtmlService
    WebSocketHandler --> SessionStorageService
    WebSocketHandler --> ConversionService
    WebSocketHandler --> TokenValidationService
    Controllers --> ConversionService
    Controllers --> TokenValidationService
```

Sources: [Program.cs:19-25](), [Program.cs:11-17]()

## Configuration Sources

The application loads configuration from multiple sources following the standard ASP.NET Core configuration hierarchy. Configuration values are accessed through the `IConfiguration` interface.

### Configuration Structure

```mermaid
graph LR
    subgraph "Configuration Sources"
        A["appsettings.json"]
        B["appsettings.{Environment}.json"]
        C["Environment Variables"]
        D["Command Line Arguments"]
    end
    
    subgraph "Configuration Sections"
        E["AllowedOrigins[]"]
        F["Aspose:LicensePath"]
        G["ConnectionStrings"]
        H["Logging"]
    end
    
    A --> E
    A --> F
    B --> G
    C --> H
    D --> G
    
    subgraph "Usage in Code"
        I["CORS Policy"]
        J["Aspose.Words.License"]
        K["TokenValidationService"]
        L["SystemEventLogger"]
    end
    
    E --> I
    F --> J
    G --> K
    H --> L
```

### Key Configuration Sections

| Section | Type | Usage |
|---------|------|-------|
| `AllowedOrigins` | `string[]` | CORS policy configuration |
| `Aspose:LicensePath` | `string` | Aspose.Words license file path |
| `ConnectionStrings` | `object` | Database connection configuration |
| `Logging` | `object` | Logging level and provider settings |

Sources: [Program.cs:28](), [Program.cs:77]()

## Middleware Pipeline Configuration

The middleware pipeline is configured in a specific order to ensure proper request processing flow. Each middleware component serves a distinct purpose in the request/response cycle.

### Middleware Pipeline Order

```mermaid
flowchart TD
    A["Incoming Request"] --> B["UseSwagger()"]
    B --> C["UseSwaggerUI()"]
    C --> D["UseHttpsRedirection()"]
    D --> E["UseCors(CorsPolicy)"]
    E --> F["UseWebSockets()"]
    F --> G["Custom WebSocket Middleware"]
    G --> H["UseAuthorization()"]
    H --> I["MapControllers()"]
    I --> J["Response"]
    
    G --> G1{"WebSocket Request?"}
    G1 -->|Yes| G2["WebSocketHandler.HandleAsync()"]
    G1 -->|No| H
    G2 --> G3["WebSocket Response"]
```

### WebSocket Middleware Implementation

The custom WebSocket middleware handles WebSocket connection requests by creating a scoped service provider and delegating to the `WebSocketHandler` service.

Sources: [Program.cs:46-74](), [Program.cs:55-69]()

## CORS Configuration

Cross-Origin Resource Sharing (CORS) is configured to allow specific origins to access the API endpoints. The allowed origins are loaded from configuration to support different environments.

### CORS Policy Setup

```mermaid
graph TB
    A["Configuration Section: AllowedOrigins"] --> B["Get<string[]>()"]
    B --> C["CORS Policy Builder"]
    C --> D["WithOrigins(allowedOrigins)"]
    C --> E["AllowAnyHeader()"]
    C --> F["AllowAnyMethod()"]
    
    D --> G["CorsPolicy"]
    E --> G
    F --> G
    
    G --> H["app.UseCors(CorsPolicy)"]
```

The CORS policy is named `"CorsPolicy"` and allows:
- Origins specified in the `AllowedOrigins` configuration array
- Any HTTP headers
- Any HTTP methods

Sources: [Program.cs:28](), [Program.cs:31-40](), [Program.cs:51]()

## Aspose License Configuration

The Aspose.Words library requires a valid license file for production use. The license path is configured through the application settings and loaded during startup.

### License Loading Process

```mermaid
flowchart TD
    A["Read Aspose:LicensePath from Configuration"] --> B{"File.Exists(licensePath)?"}
    B -->|Yes| C["new Aspose.Words.License()"]
    B -->|No| D["Console.WriteLine: License path missing"]
    C --> E["SetLicense(licensePath)"]
    E --> F["Aspose.Words Initialized"]
    D --> G["Aspose.Words in Trial Mode"]
```

The license configuration:
- Reads the `Aspose:LicensePath` setting from configuration
- Validates the file exists before attempting to load
- Initializes the Aspose.Words license if the file is found
- Logs an error message if the license file is missing

Sources: [Program.cs:77-88]()

## Development Environment Configuration

The application supports multiple launch profiles for different development scenarios, configured through the launch settings.

### Launch Profiles

| Profile | Command | URL | Purpose |
|---------|---------|-----|---------|
| `HtmlDocxConvertService` | Project | https://localhost:7125 | Direct project execution |
| `IIS Express` | IISExpress | http://localhost:39065 | IIS Express hosting |

Both profiles:
- Set `ASPNETCORE_ENVIRONMENT` to `"Development"`
- Launch the browser to the Swagger UI
- Enable .NET run messages for debugging

Sources: [Properties/launchSettings.json:12-29]()