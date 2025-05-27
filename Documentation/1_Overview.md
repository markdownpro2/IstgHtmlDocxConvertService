# Overview

<details>
<summary>Relevant source files</summary>

The following files were used as context for generating this wiki page:

- [IstgHtmlDocxConvertService.csproj](IstgHtmlDocxConvertService.csproj)
- [LICENSE](LICENSE)
- [README.md](README.md)

</details>



## Purpose and Scope

IstgHtmlDocxConvertService is a .NET 6.0 web service that provides bidirectional document conversion between HTML and Microsoft Word DOCX formats, with real-time collaborative editing capabilities. The service enables users to convert HTML content (particularly from TinyMCE editor) to Word documents, edit those documents in Microsoft Word, and synchronize changes back to HTML in real-time.

This overview covers the high-level architecture, core capabilities, and key components of the system. For detailed API documentation, see [HTTP API Reference](#3). For real-time communication protocols, see [Real-time Communication](#5). For deployment and configuration details, see [Configuration and Deployment](#7).

Sources: [README.md:1-3](), [IstgHtmlDocxConvertService.csproj:1-41]()

## System Architecture

The service follows a layered architecture with multiple communication protocols and external integrations:

### High-Level Component Architecture

```mermaid
graph TB
    subgraph "HTTP_API_Layer"
        ConversionController["ConversionController"]
        EditInWordController["EditInWordController"]
    end
    
    subgraph "WebSocket_Layer" 
        WebSocketHandler["WebSocketHandler"]
    end
    
    subgraph "Core_Services"
        ConversionService["ConversionService"]
        HtmlService["HtmlService"] 
        SessionStorageService["SessionStorageService"]
        TokenValidationService["TokenValidationService"]
        SessionCleanupService["SessionCleanupService"]
    end
    
    subgraph "External_Dependencies"
        AsposeWords["Aspose.Words"]
        SqlDatabase["SQL Database (tbrPersonnel)"]
        FileSystem["File System"]
        IstgOfficeAutomationBrl["IstgOfficeAutomationBrl"]
    end
    
    ConversionController --> ConversionService
    ConversionController --> TokenValidationService
    EditInWordController --> ConversionService  
    EditInWordController --> TokenValidationService
    
    WebSocketHandler --> ConversionService
    WebSocketHandler --> SessionStorageService
    WebSocketHandler --> TokenValidationService
    
    ConversionService --> AsposeWords
    ConversionService --> HtmlService
    ConversionService --> SessionStorageService
    
    TokenValidationService --> SqlDatabase
    SessionStorageService --> FileSystem
    SessionCleanupService --> SessionStorageService
    
    ConversionService --> IstgOfficeAutomationBrl
```

Sources: [IstgHtmlDocxConvertService.csproj:25-26](), [IstgHtmlDocxConvertService.csproj:29-31]()

### Document Processing Workflow

```mermaid
flowchart LR
    subgraph "HTML_to_DOCX_Flow"
        HtmlInput["HTML Content"] --> ConversionService1["ConversionService"]
        ConversionService1 --> HtmlService1["HtmlService"] 
        HtmlService1 --> AsposeWords1["Aspose.Words"]
        AsposeWords1 --> DocxOutput["DOCX File"]
    end
    
    subgraph "Word_Integration_Flow"
        DocxOutput --> EditInWordController1["EditInWordController"]
        EditInWordController1 --> MsWordUrl["ms-word:// URL"]
        MsWordUrl --> WordApp["Microsoft Word"]
    end
    
    subgraph "Real_Time_Sync"
        WordApp --> WebSocketHandler1["WebSocketHandler"]
        WebSocketHandler1 --> ConversionService2["ConversionService"] 
        ConversionService2 --> AsposeWords2["Aspose.Words"]
        AsposeWords2 --> HtmlOutput["Updated HTML"]
        HtmlOutput --> WebClient["Web Client"]
    end
    
    subgraph "Session_Management"
        SessionStorageService1["SessionStorageService"] --> HtmlSession["HtmlSession"]
        HtmlSession --> ConcurrentDictionary["ConcurrentDictionary Storage"]
        SessionCleanupService1["SessionCleanupService"] --> ConcurrentDictionary
    end
    
    ConversionService1 -.-> SessionStorageService1
    ConversionService2 -.-> SessionStorageService1
    WebSocketHandler1 -.-> SessionStorageService1
```

Sources: Based on system architecture diagrams and component relationships

## Core Capabilities

### Document Conversion
- **HTML to DOCX**: Converts HTML content to Microsoft Word documents using `ConversionService` and `Aspose.Words`
- **DOCX to HTML**: Processes uploaded Word documents back to HTML format
- **Format Preservation**: Maintains styling, formatting, and document structure across conversions

### Real-Time Collaboration  
- **WebSocket Communication**: Bidirectional real-time updates via `WebSocketHandler`
- **Session Management**: User sessions tracked through `SessionStorageService` with `HtmlSession` objects
- **Live Synchronization**: Changes in Microsoft Word automatically sync to web clients

### Microsoft Word Integration
- **Launch URLs**: Generates `ms-word://` protocol URLs via `EditInWordController` 
- **OOXML Processing**: Handles Word document updates through OOXML format
- **Background Cleanup**: Automatic file and session cleanup via `SessionCleanupService`

Sources: Based on component analysis and workflow diagrams

## Technology Stack

| Component | Technology | Purpose |
|-----------|------------|---------|
| **Framework** | .NET 6.0 | Web service platform |
| **Document Engine** | Aspose.Words | Core document conversion |
| **Database** | SQL Server | User authentication via `tbrPersonnel` |
| **Real-time** | WebSockets | Live collaboration |
| **Graphics** | SkiaSharp | Image processing support |
| **API Documentation** | Swashbuckle | OpenAPI/Swagger integration |
| **External Integration** | IstgOfficeAutomationBrl | Office automation components |

Sources: [IstgHtmlDocxConvertService.csproj:10-14](), [IstgHtmlDocxConvertService.csproj:16-22](), [IstgHtmlDocxConvertService.csproj:24-32]()

## Key Components Overview

### Controllers
- **`ConversionController`**: HTTP REST endpoints for document conversion operations
- **`EditInWordController`**: Word integration endpoints for launching and managing Word editing sessions

### Services  
- **`ConversionService`**: Core business logic for document processing and format conversion
- **`HtmlService`**: Specialized HTML content processing and manipulation
- **`SessionStorageService`**: Session lifecycle management using `ConcurrentDictionary`
- **`TokenValidationService`**: User authentication against SQL database
- **`SessionCleanupService`**: Background service for resource cleanup

### Communication
- **`WebSocketHandler`**: Real-time message processing and client/Word application coordination
- **`HtmlSession`**: Session state container linking users, HTML content, and WebSocket connections

Sources: Based on architectural analysis and component relationships

## License and Usage

The service is distributed under the MIT License, allowing unrestricted use, modification, and distribution for both commercial and non-commercial purposes.

Sources: [LICENSE:1-22]()